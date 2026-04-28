from __future__ import annotations

import argparse
import os

import psycopg2


DEFAULT_CONNECTION_STRING = "host=localhost port=5432 dbname=guessword_db user=postgres password=123"
TABLES_TO_TRUNCATE = [
    "GameAttempts",
    "GamePlayers",
    "Games",
    "Rooms",
    "Words",
]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Reset dictionary and active game data before reimporting the final word lists."
    )
    parser.add_argument(
        "--connection-string",
        default=os.getenv("GUESSWORD_DB_CONNECTION", DEFAULT_CONNECTION_STRING),
        help="PostgreSQL connection string.",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()

    with psycopg2.connect(args.connection_string) as connection:
        with connection.cursor() as cursor:
            joined_tables = ", ".join(f'"{table}"' for table in TABLES_TO_TRUNCATE)
            cursor.execute(f'TRUNCATE TABLE {joined_tables} RESTART IDENTITY CASCADE')

    print("Game dictionary data has been reset.")


if __name__ == "__main__":
    main()
