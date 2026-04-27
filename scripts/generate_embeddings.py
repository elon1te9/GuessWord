from __future__ import annotations

import argparse
import os
import time
from typing import Any

import psycopg2
import requests


DEFAULT_CONNECTION_STRING = "host=localhost port=5432 dbname=guessword_db user=postgres password=123"
DEFAULT_BASE_URL = "https://polza.ai/api/v1"
DEFAULT_MODEL = "openai/text-embedding-3-large"
DEFAULT_DIMENSIONS = 1024
DEFAULT_BATCH_SIZE = 128
DEFAULT_TIMEOUT_SECONDS = 120
MAX_RETRIES = 3


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate embeddings for words in the Words table via an OpenAI-compatible API."
    )
    parser.add_argument(
        "--connection-string",
        default=os.getenv("GUESSWORD_DB_CONNECTION", DEFAULT_CONNECTION_STRING),
        help="PostgreSQL connection string.",
    )
    parser.add_argument(
        "--base-url",
        default=os.getenv("EMBEDDINGS_BASE_URL", DEFAULT_BASE_URL),
        help="Base URL of the embeddings API.",
    )
    parser.add_argument(
        "--api-key",
        default=os.getenv("POLZA_AI_API_KEY") or os.getenv("OPENAI_API_KEY"),
        help="API key for the embeddings provider. Prefer POLZA_AI_API_KEY/OPENAI_API_KEY env vars.",
    )
    parser.add_argument(
        "--model",
        default=os.getenv("EMBEDDINGS_MODEL", DEFAULT_MODEL),
        help="Embedding model id.",
    )
    parser.add_argument(
        "--dimensions",
        type=int,
        default=int(os.getenv("EMBEDDINGS_DIMENSIONS", str(DEFAULT_DIMENSIONS))),
        help="Embedding dimensions. Keep this aligned with the vector size in the database schema.",
    )
    parser.add_argument(
        "--batch-size",
        type=int,
        default=int(os.getenv("EMBEDDINGS_BATCH_SIZE", str(DEFAULT_BATCH_SIZE))),
        help="Number of words per API request.",
    )
    parser.add_argument(
        "--timeout-seconds",
        type=int,
        default=int(os.getenv("EMBEDDINGS_TIMEOUT_SECONDS", str(DEFAULT_TIMEOUT_SECONDS))),
        help="HTTP timeout per request in seconds.",
    )
    return parser.parse_args()


def to_pgvector(values: list[float]) -> str:
    return "[" + ",".join(str(value) for value in values) + "]"


def fetch_pending_words(connection_string: str) -> list[tuple[int, str]]:
    with psycopg2.connect(connection_string) as connection:
        with connection.cursor() as cursor:
            cursor.execute('SELECT "Id", "Text" FROM "Words" WHERE "Embedding" IS NULL ORDER BY "Id"')
            return cursor.fetchall()


def store_embeddings(connection_string: str, rows: list[tuple[int, list[float]]]) -> None:
    with psycopg2.connect(connection_string) as connection:
        with connection.cursor() as cursor:
            cursor.executemany(
                'UPDATE "Words" SET "Embedding" = %s WHERE "Id" = %s',
                [(to_pgvector(embedding), word_id) for word_id, embedding in rows],
            )
        connection.commit()


def request_embeddings(
    *,
    base_url: str,
    api_key: str,
    model: str,
    dimensions: int,
    inputs: list[str],
    timeout_seconds: int,
) -> list[list[float]]:
    url = base_url.rstrip("/") + "/embeddings"
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
    }
    payload: dict[str, Any] = {
        "model": model,
        "input": inputs,
        "encoding_format": "float",
        "dimensions": dimensions,
    }

    last_error: Exception | None = None

    for attempt in range(1, MAX_RETRIES + 1):
        try:
            response = requests.post(url, headers=headers, json=payload, timeout=timeout_seconds)
            response.raise_for_status()
            body = response.json()
            data = body.get("data")

            if not isinstance(data, list):
                raise RuntimeError(f"Unexpected embeddings response: {body}")

            embeddings = [item["embedding"] for item in sorted(data, key=lambda item: item["index"])]

            if len(embeddings) != len(inputs):
                raise RuntimeError("Embedding response size does not match request size.")

            if any(len(embedding) != dimensions for embedding in embeddings):
                raise RuntimeError(
                    f"Embedding dimensions mismatch. Expected {dimensions}, got {[len(embedding) for embedding in embeddings[:3]]}."
                )

            return embeddings
        except Exception as error:  # noqa: BLE001
            last_error = error
            if attempt == MAX_RETRIES:
                break
            time.sleep(attempt)

    raise RuntimeError(f"Failed to fetch embeddings after {MAX_RETRIES} attempts: {last_error}") from last_error


def chunked[T](values: list[T], size: int) -> list[list[T]]:
    return [values[index:index + size] for index in range(0, len(values), size)]


def main() -> None:
    args = parse_args()

    if not args.api_key:
        raise SystemExit("API key is missing. Set POLZA_AI_API_KEY or OPENAI_API_KEY.")

    if args.dimensions <= 0:
        raise SystemExit("Dimensions must be greater than zero.")

    if args.batch_size <= 0:
        raise SystemExit("Batch size must be greater than zero.")

    pending_words = fetch_pending_words(args.connection_string)
    total = len(pending_words)

    if total == 0:
        print("No words without embeddings were found.")
        return

    print(f"Found {total} words without embeddings.")
    print(f"Provider base URL: {args.base_url}")
    print(f"Model: {args.model}")
    print(f"Dimensions: {args.dimensions}")
    print(f"Batch size: {args.batch_size}")

    processed = 0

    for batch in chunked(pending_words, args.batch_size):
        word_ids = [word_id for word_id, _ in batch]
        inputs = [text for _, text in batch]

        embeddings = request_embeddings(
            base_url=args.base_url,
            api_key=args.api_key,
            model=args.model,
            dimensions=args.dimensions,
            inputs=inputs,
            timeout_seconds=args.timeout_seconds,
        )

        store_embeddings(
            args.connection_string,
            list(zip(word_ids, embeddings, strict=True)),
        )

        processed += len(batch)
        print(f"Processed {processed}/{total}")

    print("Done")


if __name__ == "__main__":
    main()
