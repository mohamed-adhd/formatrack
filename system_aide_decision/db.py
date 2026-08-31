"""SQLite database helper for the decision support engine."""
import sqlite3
from pathlib import Path


class Database:
    def __init__(self, db_path: str):
        self.db_path = db_path
        self.conn: sqlite3.Connection | None = None

    def connect(self):
        self.conn = sqlite3.connect(self.db_path)
        self.conn.row_factory = sqlite3.Row

    def close(self):
        if self.conn:
            self.conn.close()
            self.conn = None

    def ensure_suggestions_table(self):
        """Create the suggestions_aide table if it doesn't exist."""
        cur = self.conn.cursor()
        cur.execute("""
            CREATE TABLE IF NOT EXISTS suggestions_aide (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                titre TEXT NOT NULL,
                description TEXT NOT NULL,
                priorite INTEGER NOT NULL DEFAULT 3,
                categorie TEXT NOT NULL,
                action_page TEXT NOT NULL,
                action_params TEXT DEFAULT '',
                est_lu INTEGER NOT NULL DEFAULT 0,
                date_generation TEXT NOT NULL DEFAULT (datetime('now'))
            )
        """)
        self.conn.commit()

    def clear_old_suggestions(self):
        """Remove all existing suggestions before regenerating."""
        cur = self.conn.cursor()
        cur.execute("DELETE FROM suggestions_aide")
        self.conn.commit()

    def insert_suggestion(self, titre: str, description: str, priorite: int,
                          categorie: str, action_page: str, action_params: str = ""):
        """Insert a single suggestion."""
        cur = self.conn.cursor()
        cur.execute("""
            INSERT INTO suggestions_aide (titre, description, priorite, categorie, action_page, action_params)
            VALUES (?, ?, ?, ?, ?, ?)
        """, (titre, description, priorite, categorie, action_page, action_params))
        self.conn.commit()

    def insert_suggestions_batch(self, suggestions: list[dict]):
        """Insert multiple suggestions at once."""
        cur = self.conn.cursor()
        cur.executemany("""
            INSERT INTO suggestions_aide (titre, description, priorite, categorie, action_page, action_params)
            VALUES (:titre, :description, :priorite, :categorie, :action_page, :action_params)
        """, suggestions)
        self.conn.commit()

    def query(self, sql: str, params: tuple = ()) -> list[dict]:
        """Run a SELECT query and return list of dicts."""
        cur = self.conn.cursor()
        cur.execute(sql, params)
        rows = cur.fetchall()
        return [dict(row) for row in rows]

    def query_one(self, sql: str, params: tuple = ()) -> dict | None:
        """Run a SELECT query and return single row or None."""
        cur = self.conn.cursor()
        cur.execute(sql, params)
        row = cur.fetchone()
        return dict(row) if row else None
