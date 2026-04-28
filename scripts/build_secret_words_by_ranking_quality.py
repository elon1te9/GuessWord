from __future__ import annotations

import argparse
import csv
import math
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path

import numpy as np


DEFAULT_SECRET_TARGET = 800
MIN_SECRET_TARGET = 400
DEFAULT_CANDIDATE_POOL = 4500
DEFAULT_PREVIEW_NEIGHBORS = 100
DEFAULT_EVAL_NEIGHBORS = 50
DEFAULT_REPORT_SIZE = 80
DEFAULT_MAX_TOP20_BAD = 3
DEFAULT_MAX_TOP40_BAD = 8
DEFAULT_MIN_TOP20_DIRECT = 7
DEFAULT_MAX_ABSENT_FROM_TSV = 18

SUSPECT_SUFFIXES = (
    "ость", "изм", "ция", "ирование", "ирование", "ность", "енность",
    "тельность", "ество", "ание", "ение", "изм", "истика",
)


@dataclass(frozen=True)
class CandidateSeed:
    word: str
    seed_score: float
    total_weight: float
    links_count: int


@dataclass(frozen=True)
class CandidateDiagnostics:
    top20_bad: int
    top40_bad: int
    top20_direct: int
    absent_from_tsv: int


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build secret-words.txt by selecting words with the cleanest ranking neighbors."
    )
    parser.add_argument("--vec", type=Path, default=Path("GuessWord.Api/Resources/Data/sociation2vec800.vec"))
    parser.add_argument("--tsv", type=Path, default=Path("GuessWord.Api/Resources/Data/sociation.org.tsv"))
    parser.add_argument("--all-words", type=Path, default=Path("GuessWord.Api/Resources/all-words.txt"))
    parser.add_argument("--output-secret", type=Path, default=Path("GuessWord.Api/Resources/secret-words.txt"))
    parser.add_argument("--quality-report", type=Path, default=Path("GuessWord.Api/Resources/secret-words-quality-report.tsv"))
    parser.add_argument("--prune-report", type=Path, default=Path("GuessWord.Api/Resources/all-words-prune-candidates.tsv"))
    parser.add_argument("--blacklist", type=Path, default=Path("scripts/dictionary-blacklist.txt"))
    parser.add_argument("--secret-blacklist", type=Path, default=Path("scripts/dictionary-secret-blacklist.txt"))
    parser.add_argument("--secret-target", type=int, default=DEFAULT_SECRET_TARGET)
    parser.add_argument("--candidate-pool", type=int, default=DEFAULT_CANDIDATE_POOL)
    parser.add_argument("--preview-neighbors", type=int, default=DEFAULT_PREVIEW_NEIGHBORS)
    parser.add_argument("--eval-neighbors", type=int, default=DEFAULT_EVAL_NEIGHBORS)
    parser.add_argument("--report-size", type=int, default=DEFAULT_REPORT_SIZE)
    parser.add_argument("--min-secret-target", type=int, default=MIN_SECRET_TARGET)
    parser.add_argument("--max-top20-bad", type=int, default=DEFAULT_MAX_TOP20_BAD)
    parser.add_argument("--max-top40-bad", type=int, default=DEFAULT_MAX_TOP40_BAD)
    parser.add_argument("--min-top20-direct", type=int, default=DEFAULT_MIN_TOP20_DIRECT)
    parser.add_argument("--max-absent-from-tsv", type=int, default=DEFAULT_MAX_ABSENT_FROM_TSV)
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


def load_all_words(path: Path) -> list[str]:
    return [
        normalize_word(line)
        for line in path.read_text(encoding="utf-8").splitlines()
        if line.strip()
    ]


def load_tsv_graph(path: Path) -> tuple[dict[str, float], dict[str, int], dict[str, dict[str, float]], set[str]]:
    total_weight: dict[str, float] = defaultdict(float)
    links_count: dict[str, int] = defaultdict(int)
    adjacency: dict[str, dict[str, float]] = defaultdict(dict)
    words: set[str] = set()

    with path.open("r", encoding="utf-8-sig") as file:
        reader = csv.reader(file, delimiter="\t")
        for row in reader:
            if len(row) != 3:
                continue

            left = normalize_word(row[0])
            right = normalize_word(row[1])

            try:
                weight = float(row[2])
            except ValueError:
                continue

            words.add(left)
            words.add(right)
            total_weight[left] += weight
            total_weight[right] += weight
            links_count[left] += 1
            links_count[right] += 1

            adjacency[left][right] = max(adjacency[left].get(right, 0.0), weight)
            adjacency[right][left] = max(adjacency[right].get(left, 0.0), weight)

    return dict(total_weight), dict(links_count), {k: dict(v) for k, v in adjacency.items()}, words


def load_vectors(vec_path: Path, allowed_words: set[str]) -> tuple[list[str], np.ndarray]:
    words: list[str] = []
    vectors: list[np.ndarray] = []

    with vec_path.open("r", encoding="utf-8") as file:
        header = file.readline().strip().split()
        if len(header) != 2:
            raise SystemExit(f"Invalid vec header in {vec_path}: {' '.join(header)}")

        for line in file:
            if not line.strip():
                continue

            parts = line.strip().split()
            word = normalize_word(parts[0])
            if word not in allowed_words:
                continue

            vector = np.asarray(parts[1:], dtype=np.float32)
            words.append(word)
            vectors.append(vector)

    if not words:
        raise SystemExit("No vectors were loaded for the current all-words set.")

    matrix = np.vstack(vectors)
    norms = np.linalg.norm(matrix, axis=1, keepdims=True)
    norms[norms == 0] = 1.0
    normalized = matrix / norms

    return words, normalized


def build_seed_candidates(
    words: list[str],
    total_weight: dict[str, float],
    links_count: dict[str, int],
    secret_blacklist: set[str],
    candidate_pool: int,
) -> list[CandidateSeed]:
    candidates: list[CandidateSeed] = []

    for word in words:
        if word in secret_blacklist:
            continue

        weight = total_weight.get(word, 0.0)
        links = links_count.get(word, 0)
        if links < 2 or weight <= 0:
            continue

        seed_score = math.log1p(weight) * 3.5 + math.log1p(links) * 2.0

        if 4 <= len(word) <= 9:
            seed_score += 0.35
        elif len(word) >= 13:
            seed_score -= 0.3

        candidates.append(CandidateSeed(word, seed_score, weight, links))

    candidates.sort(key=lambda item: (-item.seed_score, item.word))
    return candidates[:candidate_pool]


def looks_suspect(
    word: str,
    tsv_words: set[str],
    links_count: dict[str, int],
    blacklist: set[str],
) -> bool:
    if word in blacklist:
        return True

    if word not in tsv_words:
        return True

    if links_count.get(word, 0) <= 1:
        return True

    if len(word) >= 12 and word.endswith(SUSPECT_SUFFIXES):
        return True

    return False


def evaluate_candidate(
    *,
    index: int,
    words: list[str],
    matrix: np.ndarray,
    adjacency: dict[str, dict[str, float]],
    total_weight: dict[str, float],
    links_count: dict[str, int],
    tsv_words: set[str],
    blacklist: set[str],
    preview_neighbors: int,
    eval_neighbors: int,
) -> tuple[float, list[str], Counter[str], CandidateDiagnostics]:
    word = words[index]
    similarities = matrix @ matrix[index]
    ordered = np.argsort(similarities)[::-1]
    neighbor_indexes = [idx for idx in ordered if idx != index][:preview_neighbors]
    preview_words = [words[idx] for idx in neighbor_indexes]
    top_words = preview_words[:eval_neighbors]

    score = 0.0
    bad_counter: Counter[str] = Counter()
    direct_neighbors = adjacency.get(word, {})
    strong_direct_support = 0
    weak_neighbors = 0
    absent_from_tsv = 0

    for rank, neighbor in enumerate(top_words, start=1):
        rank_weight = 1.0 / rank
        similarity = float(similarities[neighbor_indexes[rank - 1]])
        score += similarity * 2.3 * rank_weight

        direct_weight = direct_neighbors.get(neighbor, 0.0)
        if direct_weight > 0:
            strong_direct_support += 1
            score += math.log1p(direct_weight) * 1.15 * rank_weight
        else:
            score -= 0.38 * rank_weight

        score += math.log1p(links_count.get(neighbor, 0)) * 0.12 * rank_weight
        score += math.log1p(total_weight.get(neighbor, 0.0)) * 0.05 * rank_weight

        suspect = looks_suspect(neighbor, tsv_words, links_count, blacklist)
        if suspect:
            weak_neighbors += 1
            bad_counter[neighbor] += 1
            score -= 1.8 * rank_weight

        if neighbor not in tsv_words:
            absent_from_tsv += 1
            score -= 1.0 * rank_weight

        if len(neighbor) >= 12:
            score -= 0.2 * rank_weight

    top20 = top_words[:20]
    top40 = top_words[:40]
    top20_bad = sum(1 for neighbor in top20 if looks_suspect(neighbor, tsv_words, links_count, blacklist))
    top40_bad = sum(1 for neighbor in top40 if looks_suspect(neighbor, tsv_words, links_count, blacklist))
    top20_direct = sum(1 for neighbor in top20 if neighbor in direct_neighbors)

    score += strong_direct_support * 0.05
    score -= weak_neighbors * 0.04

    if top20_direct < 6:
        score -= (6 - top20_direct) * 0.8

    if top20_bad > 4:
        score -= (top20_bad - 4) * 1.2

    if top40_bad > 10:
        score -= (top40_bad - 10) * 0.6

    if absent_from_tsv > 12:
        score -= (absent_from_tsv - 12) * 0.25

    diagnostics = CandidateDiagnostics(
        top20_bad=top20_bad,
        top40_bad=top40_bad,
        top20_direct=top20_direct,
        absent_from_tsv=absent_from_tsv,
    )

    return score, preview_words, bad_counter, diagnostics


def main() -> None:
    args = parse_args()

    all_words = load_all_words(args.all_words)
    all_word_set = set(all_words)
    blacklist = load_word_set(args.blacklist)
    secret_blacklist = load_word_set(args.secret_blacklist)

    total_weight, links_count, adjacency, tsv_words = load_tsv_graph(args.tsv)
    words, matrix = load_vectors(args.vec, all_word_set)
    word_to_index = {word: index for index, word in enumerate(words)}

    seed_candidates = build_seed_candidates(
        words,
        total_weight,
        links_count,
        secret_blacklist,
        args.candidate_pool,
    )

    scored_candidates: list[tuple[str, float, str, CandidateDiagnostics]] = []
    global_bad_neighbors: Counter[str] = Counter()

    for candidate in seed_candidates:
        index = word_to_index[candidate.word]
        quality_score, preview_words, bad_counter, diagnostics = evaluate_candidate(
            index=index,
            words=words,
            matrix=matrix,
            adjacency=adjacency,
            total_weight=total_weight,
            links_count=links_count,
            tsv_words=tsv_words,
            blacklist=blacklist | secret_blacklist,
            preview_neighbors=args.preview_neighbors,
            eval_neighbors=args.eval_neighbors,
        )

        final_score = candidate.seed_score + quality_score
        scored_candidates.append((candidate.word, final_score, ", ".join(preview_words[:20]), diagnostics))
        global_bad_neighbors.update(bad_counter)

    scored_candidates.sort(key=lambda item: (-item[1], item[0]))

    accepted: list[tuple[str, float, str, CandidateDiagnostics]] = []
    for word, score, preview, diagnostics in scored_candidates:
        if len(accepted) >= args.secret_target:
            break
        if diagnostics.top20_bad > args.max_top20_bad:
            continue
        if diagnostics.top40_bad > args.max_top40_bad:
            continue
        if diagnostics.top20_direct < args.min_top20_direct:
            continue
        if diagnostics.absent_from_tsv > args.max_absent_from_tsv:
            continue

        accepted.append((word, score, preview, diagnostics))

    if len(accepted) < args.min_secret_target:
        raise SystemExit(
            f"Only {len(accepted)} secret words passed ranking quality, but at least {args.min_secret_target} are required."
        )

    secret_words = sorted(word for word, _, _, _ in accepted)
    args.output_secret.write_text("\n".join(secret_words) + "\n", encoding="utf-8")

    with args.quality_report.open("w", encoding="utf-8", newline="") as file:
        writer = csv.writer(file, delimiter="\t")
        writer.writerow(["word", "score", "top20_bad", "top40_bad", "top20_direct", "absent_from_tsv", "top20_preview"])
        for word, score, preview, diagnostics in accepted[: args.report_size]:
            writer.writerow([
                word,
                f"{score:.6f}",
                diagnostics.top20_bad,
                diagnostics.top40_bad,
                diagnostics.top20_direct,
                diagnostics.absent_from_tsv,
                preview,
            ])

    with args.prune_report.open("w", encoding="utf-8", newline="") as file:
        writer = csv.writer(file, delimiter="\t")
        writer.writerow(["word", "bad_hits"])
        for word, count in global_bad_neighbors.most_common(200):
            if count >= 3:
                writer.writerow([word, count])

    print(f"all_words={len(all_words)}")
    print(f"candidate_pool={len(seed_candidates)}")
    print(f"secret_words={len(secret_words)} -> {args.output_secret}")
    print(f"quality_report={args.quality_report}")
    print(f"prune_report={args.prune_report}")
    print("top_secret_sample=", [word for word, _, _, _ in accepted[:20]])


if __name__ == "__main__":
    main()
