#!/usr/bin/env python3
"""
Decision Support Engine for SEFAD/Formatrack.
Scans the SQLite database, runs all analyzers, writes suggestions to suggestions_aide table.

Usage: python3 engine.py <path_to_database.db>
"""
import sys
from pathlib import Path

from db import Database
from analyzers import get_all_analyzers


def main():
    if len(sys.argv) < 2:
        print("Usage: python3 engine.py <db_path>", file=sys.stderr)
        sys.exit(1)

    db_path = sys.argv[1]
    if not Path(db_path).exists():
        print(f"Database not found: {db_path}", file=sys.stderr)
        sys.exit(1)

    db = Database(db_path)
    try:
        db.connect()
        db.ensure_suggestions_table()
        db.clear_old_suggestions()

        all_suggestions = []
        analyzers = get_all_analyzers()

        for name, analyzer_fn in analyzers:
            try:
                results = analyzer_fn(db)
                if results:
                    all_suggestions.extend(results)
            except Exception as e:
                print(f"Analyzer '{name}' failed: {e}", file=sys.stderr)

        if all_suggestions:
            db.insert_suggestions_batch(all_suggestions)

        print(f"Generated {len(all_suggestions)} suggestion(s).")

    finally:
        db.close()


if __name__ == "__main__":
    main()
