"""
Seed the RAG knowledge base: index the SQLite database into chunks + embeddings.
Usage: python3 -m rag.seed <db_path>
"""
import json
import sys
from pathlib import Path

from .indexer import index_database


def main():
    if len(sys.argv) < 2:
        print(json.dumps({"error": "Usage: python3 -m rag.seed <db_path>"}))
        sys.exit(1)

    db_path = sys.argv[1]
    if not Path(db_path).exists():
        print(json.dumps({"error": f"Database not found: {db_path}"}))
        sys.exit(1)

    result = index_database(db_path)
    print(json.dumps(result, ensure_ascii=False))


if __name__ == "__main__":
    main()
