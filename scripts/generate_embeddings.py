import psycopg2
from sentence_transformers import SentenceTransformer

CONNECTION_STRING = "host=localhost port=5432 dbname=guessword_db user=postgres password=123"

model = SentenceTransformer("sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2")

def to_pgvector(vec):
    return "[" + ",".join(str(x) for x in vec) + "]"

conn = psycopg2.connect(CONNECTION_STRING)
cur = conn.cursor()

cur.execute('SELECT "Id", "Text" FROM "Words" WHERE "Embedding" IS NULL')
rows = cur.fetchall()

for word_id, text in rows:
    emb = model.encode(text, normalize_embeddings=True)
    cur.execute(
        'UPDATE "Words" SET "Embedding" = %s WHERE "Id" = %s',
        (to_pgvector(emb), word_id)
    )

conn.commit()
cur.close()
conn.close()

print("Done")