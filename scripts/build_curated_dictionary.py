from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path

import pymorphy3
from wordfreq import zipf_frequency


MIN_WORDS = 20_000
DEFAULT_TARGET_WORDS = 22_000
MIN_ZIPF_FREQUENCY = 1.8
ALLOWED_CHARS = set("абвгдеёжзийклмнопрстуфхцчшщъыьэюя")
PROPER_NOUN_GRAMMEMES = {"Name", "Surn", "Patr", "Geox", "Orgn", "Trad"}
PRIORITY_POS = {"NOUN", "VERB", "INFN", "ADJF", "ADJS", "COMP", "ADVB"}


@dataclass(frozen=True)
class CandidateScore:
    word: str
    score: float
    zipf: float


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build a curated all-words.txt from the current dictionary and secret list."
    )
    parser.add_argument(
        "--all-words",
        type=Path,
        default=Path("GuessWord.Api/Resources/all-words.txt"),
        help="Path to the source all-words.txt file.",
    )
    parser.add_argument(
        "--secret-words",
        type=Path,
        default=Path("GuessWord.Api/Resources/secret-words.txt"),
        help="Path to the source secret-words.txt file.",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("GuessWord.Api/Resources/all-words.txt"),
        help="Path for the curated dictionary output.",
    )
    parser.add_argument(
        "--target",
        type=int,
        default=DEFAULT_TARGET_WORDS,
        help="Target number of words in the curated dictionary.",
    )
    return parser.parse_args()


def load_words(path: Path) -> list[str]:
    return [
        line.strip().lower()
        for line in path.read_text(encoding="utf-8").splitlines()
        if line.strip()
    ]


def is_russian_word(word: str) -> bool:
    return all(char in ALLOWED_CHARS for char in word)


def looks_like_common_word(word: str, morph: pymorphy3.MorphAnalyzer) -> bool:
    if len(word) < 3 or len(word) > 14:
        return False

    if not is_russian_word(word):
        return False

    parse = morph.parse(word)[0]

    if any(grammeme in parse.tag for grammeme in PROPER_NOUN_GRAMMEMES):
        return False

    if parse.tag.POS is None:
        return False

    if parse.normal_form != word:
        return False

    if parse.tag.POS not in PRIORITY_POS and len(word) > 10:
        return False

    return True


def build_score(word: str, morph: pymorphy3.MorphAnalyzer) -> CandidateScore:
    zipf = zipf_frequency(word, "ru")
    parse = morph.parse(word)[0]

    score = zipf

    if parse.normal_form == word:
        score += 0.08

    if parse.tag.POS in {"NOUN", "VERB", "INFN", "ADJF"}:
        score += 0.05

    if len(word) <= 8:
        score += 0.06
    elif len(word) >= 12:
        score -= 0.12

    return CandidateScore(word=word, score=score, zipf=zipf)


def main() -> None:
    args = parse_args()

    all_words = sorted(set(load_words(args.all_words)))
    secret_words = sorted(set(load_words(args.secret_words)))

    missing_secret_words = [word for word in secret_words if word not in all_words]
    if missing_secret_words:
        raise SystemExit(
            "Secret words are missing from all-words.txt: "
            + ", ".join(missing_secret_words[:20])
        )

    morph = pymorphy3.MorphAnalyzer()

    curated: set[str] = set(secret_words)
    candidates: list[CandidateScore] = []

    for word in all_words:
        if word in curated:
            continue

        if not looks_like_common_word(word, morph):
            continue

        candidate = build_score(word, morph)
        if candidate.zipf < MIN_ZIPF_FREQUENCY:
            continue

        candidates.append(candidate)

    candidates.sort(key=lambda item: (-item.score, item.word))

    for candidate in candidates:
        curated.add(candidate.word)
        if len(curated) >= args.target:
            break

    if len(curated) < MIN_WORDS:
        raise SystemExit(
            f"Curated dictionary is too small: {len(curated)} words; expected at least {MIN_WORDS}."
        )

    output_words = sorted(curated)
    args.output.write_text("\n".join(output_words) + "\n", encoding="utf-8")

    print(f"Curated dictionary written to {args.output}")
    print(f"Total words: {len(output_words)}")
    print(f"Secret words preserved: {len(secret_words)}")


if __name__ == "__main__":
    main()
