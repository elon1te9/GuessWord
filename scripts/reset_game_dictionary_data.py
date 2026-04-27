from __future__ import annotations

import psycopg2


CONNECTION_STRING = "host=localhost port=5432 dbname=guessword_db user=postgres password=123"
TABLES_TO_TRUNCATE = [
    "GameAttempts",
    "GamePlayers",
    "Games",
    "Rooms",
    "Words",
]


def main() -> None:
    with psycopg2.connect(CONNECTION_STRING) as connection:
        with connection.cursor() as cursor:
            joined_tables = ", ".join(f'"{table}"' for table in TABLES_TO_TRUNCATE)
            cursor.execute(f'TRUNCATE TABLE {joined_tables} RESTART IDENTITY CASCADE')

    print("Game dictionary data has been reset.")


if __name__ == "__main__":
    main()
