from __future__ import annotations

import argparse
import csv
import math
import re
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path

import pymorphy3


MIN_ALL_WORDS = 20_000
DEFAULT_SECRET_TARGET = 1_000
PROPER_NOUN_GRAMMEMES = {"Name", "Surn", "Patr", "Geox", "Orgn", "Trad"}
CYRILLIC_RE = re.compile(r"^[а-яё]+$")


@dataclass(frozen=True)
class SecretScore:
    word: str
    score: float
    total_weight: float
    links_count: int


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build all-words.txt and a preliminary secret-words.txt from sociation vec + tsv sources."
    )
    parser.add_argument(
        "--vec",
        type=Path,
        default=Path("GuessWord.Api/Resources/Data/sociation2vec800.vec"),
        help="Path to sociation2vec800.vec.",
    )
    parser.add_argument(
        "--tsv",
        type=Path,
        default=Path("GuessWord.Api/Resources/Data/sociation.org.tsv"),
        help="Path to sociation.org.tsv.",
    )
    parser.add_argument(
        "--output-all",
        type=Path,
        default=Path("GuessWord.Api/Resources/all-words.txt"),
        help="Path to the output all-words.txt.",
    )
    parser.add_argument(
        "--output-secret",
        type=Path,
        default=Path("GuessWord.Api/Resources/secret-words.txt"),
        help="Path to the output secret-words.txt.",
    )
    parser.add_argument(
        "--secret-target",
        type=int,
        default=DEFAULT_SECRET_TARGET,
        help="Target number of secret words.",
    )
    parser.add_argument(
        "--blacklist",
        type=Path,
        default=Path("scripts/dictionary-blacklist.txt"),
        help="Path to the exact-match blacklist for all dictionaries.",
    )
    parser.add_argument(
        "--secret-blacklist",
        type=Path,
        default=Path("scripts/dictionary-secret-blacklist.txt"),
        help="Path to the stricter exact-match blacklist for secret words.",
    )
    return parser.parse_args()


def normalize_word(word: str) -> str:
    return word.strip().lower().replace("ё", "е")


def load_word_set(path: Path) -> set[str]:
    if not path.exists():
        return set()

    return {
        normalize_word(line)
        for line in path.read_text(encoding="utf-8").splitlines()
        if line.strip() and not line.strip().startswith("#")
    }


def has_valid_shape(word: str) -> bool:
    if len(word) < 3 or len(word) > 16:
        return False

    if not CYRILLIC_RE.fullmatch(word):
        return False

    return True


def get_noun_nomn_sing_parse(word: str, morph: pymorphy3.MorphAnalyzer):
    for parse in morph.parse(word):
        if parse.tag.POS != "NOUN":
            continue

        if parse.normal_form != word:
            continue

        if "nomn" not in parse.tag or "sing" not in parse.tag:
            continue

        if any(grammeme in parse.tag for grammeme in PROPER_NOUN_GRAMMEMES):
            continue

        if "Abbr" in parse.tag:
            continue

        return parse

    return None


def is_good_all_word(
    word: str,
    morph: pymorphy3.MorphAnalyzer,
    blacklist: set[str],
) -> bool:
    if not has_valid_shape(word):
        return False

    if word in blacklist:
        return False

    return get_noun_nomn_sing_parse(word, morph) is not None


def is_good_secret_word(
    word: str,
    morph: pymorphy3.MorphAnalyzer,
    blacklist: set[str],
    secret_blacklist: set[str],
) -> bool:
    if not has_valid_shape(word):
        return False

    if word in blacklist or word in secret_blacklist:
        return False

    return get_noun_nomn_sing_parse(word, morph) is not None


def load_vec_words(path: Path) -> list[str]:
    words: list[str] = []
    seen: set[str] = set()

    with path.open("r", encoding="utf-8") as file:
        header = file.readline().strip().split()
        if len(header) != 2:
            raise SystemExit(f"Invalid vec header in {path}: {' '.join(header)}")

        for line in file:
            if not line.strip():
                continue

            word = normalize_word(line.split(" ", 1)[0])
            if word in seen:
                continue

            seen.add(word)
            words.append(word)

    return words


def load_tsv_scores(path: Path) -> tuple[dict[str, float], dict[str, int]]:
    total_weight: dict[str, float] = defaultdict(float)
    links_count: dict[str, int] = defaultdict(int)

    with path.open("r", encoding="utf-8-sig") as file:
        reader = csv.reader(file, delimiter="\t")
        for row in reader:
            if len(row) != 3:
                continue

            source = normalize_word(row[0])
            target = normalize_word(row[1])

            try:
                weight = float(row[2])
            except ValueError:
                continue

            total_weight[source] += weight
            total_weight[target] += weight
            links_count[source] += 1
            links_count[target] += 1

    return dict(total_weight), dict(links_count)


def build_secret_score(word: str, total_weight: float, links_count: int) -> SecretScore:
    score = math.log1p(total_weight) * 3.0 + math.log1p(links_count) * 1.6

    if 4 <= len(word) <= 10:
        score += 0.35
    elif len(word) >= 13:
        score -= 0.25

    return SecretScore(
        word=word,
        score=score,
        total_weight=total_weight,
        links_count=links_count,
    )


def main() -> None:
    args = parse_args()
    morph = pymorphy3.MorphAnalyzer()
    blacklist = load_word_set(args.blacklist)
    secret_blacklist = load_word_set(args.secret_blacklist)

    vec_words = load_vec_words(args.vec)
    total_weight_by_word, links_count_by_word = load_tsv_scores(args.tsv)

    all_words = sorted({
        word
        for word in vec_words
        if is_good_all_word(word, morph, blacklist)
    })

    if len(all_words) < MIN_ALL_WORDS:
        raise SystemExit(
            f"Resulting all-words is too small: {len(all_words)} words, expected at least {MIN_ALL_WORDS}."
        )

    all_words_set = set(all_words)
    secret_candidates: list[SecretScore] = []

    for word in all_words:
        if word not in total_weight_by_word:
            continue

        if not is_good_secret_word(word, morph, blacklist, secret_blacklist):
            continue

        secret_candidates.append(
            build_secret_score(
                word,
                total_weight_by_word[word],
                links_count_by_word.get(word, 0),
            )
        )

    secret_candidates.sort(key=lambda item: (-item.score, item.word))
    secret_words = sorted({
        candidate.word
        for candidate in secret_candidates[: args.secret_target]
        if candidate.word in all_words_set
    })

    args.output_all.write_text("\n".join(all_words) + "\n", encoding="utf-8")
    args.output_secret.write_text("\n".join(secret_words) + "\n", encoding="utf-8")

    print(f"all_words={len(all_words)} -> {args.output_all}")
    print(f"secret_words={len(secret_words)} -> {args.output_secret}")
    print("top_secret_sample=", [candidate.word for candidate in secret_candidates[:20]])


if __name__ == "__main__":
    main()
