"""
Hybrid Retriever: FTS5 BM25 + vector cosine similarity with Reciprocal Rank Fusion.
Filters by user role/promotion/departement for access control.
"""
import json
import math
import os
import sqlite3
from pathlib import Path
from typing import List, Dict, Optional

try:
    import sqlite_vec
except ImportError:
    sqlite_vec = None

try:
    from openai import OpenAI
except ImportError:
    OpenAI = None


def _load_api_key() -> str:
    env_path = Path(__file__).parent.parent / ".env"
    if env_path.exists():
        for line in env_path.read_text().splitlines():
            line = line.strip()
            if line.startswith("API_KEY=") and not line.startswith("#"):
                return line.split("=", 1)[1].strip()
    return os.environ.get("API_KEY", "")


def _get_client():
    api_key = _load_api_key()
    if not api_key or api_key == "your-api-key-here":
        return None
    if OpenAI is None:
        return None
    # Groq keys (gsk_) don't have embeddings — return None so we use FTS-only
    if api_key.startswith("gsk_"):
        return None
    return OpenAI(api_key=api_key)


def _embed_query(client, text: str) -> List[float]:
    if client is None:
        return []
    try:
        resp = client.embeddings.create(input=[text], model="text-embedding-3-small")
        return resp.data[0].embedding
    except Exception as e:
        import sys
        print(f"Embedding error: {e}", file=sys.stderr)
        return []


def _cosine_similarity(a: List[float], b: List[float]) -> float:
    dot = sum(x * y for x, y in zip(a, b))
    norm_a = math.sqrt(sum(x * x for x in a))
    norm_b = math.sqrt(sum(y * y for y in b))
    return dot / (norm_a * norm_b) if norm_a and norm_b else 0.0


def _build_role_filter(role: str, promotion: str, departement: str, user_name: str = "") -> str:
    """Build SQL WHERE clause fragments for role-based access control.
    Restricts what data each role can see in the RAG knowledge base."""
    conditions = []

    # suggestions_aide is admin/decision-maker only — hide from everyone else
    if role not in ("Administrateur", "Decideur"):
        conditions.append("source_table != 'suggestions_aide'")

    if role == "Stagiaire":
        # Stagiaire: own notes/evals/absences only (by name + promotion)
        # Hide questionnaires (exam content) from stagiaires
        if promotion and user_name:
            conditions.append(
                f"(source_table NOT IN ('notes', 'evaluations', 'absences_retards', 'questionnaires') "
                f"OR (source_table IN ('notes', 'evaluations', 'absences_retards') "
                f"AND metadata LIKE '%\"promotion\": \"{promotion}\"%' "
                f"AND metadata LIKE '%\"stagiaire\": \"{user_name}\"%'))"
            )
        elif promotion:
            conditions.append(
                f"(source_table NOT IN ('notes', 'evaluations', 'absences_retards', 'questionnaires') "
                f"OR metadata LIKE '%\"promotion\": \"{promotion}\"%')"
            )
    elif role == "Formateur":
        # Formateur sees formations, modules, sessions, notes, evals, emplois
        # but not suggestions or internal questionnaires
        conditions.append("source_table NOT IN ('questionnaires')")
    elif role == "ChefDepartement":
        # ChefDepartement: filter modules by department, hide suggestions
        if departement:
            conditions.append(
                f"(source_table != 'modules' "
                f"OR metadata LIKE '%\"formation\": \"{departement}\"%')"
            )
        conditions.append("source_table NOT IN ('questionnaires')")
    elif role == "ResponsableFormation":
        # ResponsableFormation: filter personal data by promotion, hide suggestions
        if promotion:
            conditions.append(
                f"(source_table NOT IN ('notes', 'evaluations', 'absences_retards', 'questionnaires') "
                f"OR metadata LIKE '%\"promotion\": \"{promotion}\"%')"
            )
    # Admin/Decideur: no additional filter

    return " AND ".join(conditions) if conditions else "1=1"


class HybridRetriever:
    def __init__(self, db_path: str):
        self.db_path = db_path

    def _connect(self) -> sqlite3.Connection:
        conn = sqlite3.connect(self.db_path)
        try:
            if sqlite_vec:
                conn.enable_load_extension(True)
                sqlite_vec.load(conn)
                conn.enable_load_extension(False)
        except Exception:
            pass
        return conn

    def _fts_search(self, conn: sqlite3.Connection, query: str, role_filter: str, top_k: int = 20) -> List[Dict]:
        # Strip punctuation, stem for better recall
        import re
        words = re.findall(r'[a-zA-ZÀ-ÿ]+', query)
        # Filter short/stop words
        stop_words = {'le', 'la', 'les', 'de', 'du', 'des', 'un', 'une', 'et', 'est', 'sont',
                       'que', 'qui', 'quoi', 'quel', 'quelle', 'quels', 'quelles', 'ce', 'cette',
                       'en', 'au', 'aux', 'sur', 'pas', 'ne', 'se', 'son', 'sa', 'ses'}
        clean_tokens = []
        for w in words:
            wl = w.lower()
            if wl in stop_words or len(wl) < 3:
                continue
            # Basic French stemming: strip trailing s/x/aux
            if wl.endswith('aux'):
                stem = wl[:-3] + 'al'
            elif wl.endswith('s') or wl.endswith('x'):
                stem = wl[:-1]
            else:
                stem = wl
            if len(stem) > 2:
                clean_tokens.append(f'{stem}*')
        if not clean_tokens:
            return []
        match_expr = " OR ".join(clean_tokens)

        sql = f"""
            SELECT c.id, c.chunk_text, c.metadata, c.source_table, bm25(kb_chunks_fts) as score
            FROM kb_chunks_fts fts
            JOIN kb_chunks c ON c.id = fts.rowid
            WHERE kb_chunks_fts MATCH ? AND ({role_filter})
            ORDER BY score ASC
            LIMIT ?
        """
        try:
            rows = conn.execute(sql, (match_expr, top_k)).fetchall()
            return [
                {"id": r[0], "text": r[1], "metadata": json.loads(r[2]) if r[2] else {},
                 "table": r[3], "score": abs(float(r[4]))}
                for r in rows
            ]
        except Exception:
            return []

    def _vector_search(self, conn: sqlite3.Connection, query_vec: List[float], role_filter: str, top_k: int = 20) -> List[Dict]:
        if not query_vec:
            return []

        try:
            sql = f"""
                SELECT c.id, c.chunk_text, c.metadata, c.source_table, v.distance
                FROM kb_chunks_vec v
                JOIN kb_chunks c ON c.id = v.rowid
                WHERE v.embedding MATCH ? AND ({role_filter})
                ORDER BY v.distance
                LIMIT ?
            """
            query_json = json.dumps(query_vec)
            rows = conn.execute(sql, (query_json, top_k)).fetchall()
            return [
                {"id": r[0], "text": r[1], "metadata": json.loads(r[2]) if r[2] else {},
                 "table": r[3], "score": 1.0 - float(r[4])}
                for r in rows
            ]
        except Exception:
            return []

    def retrieve(self, query: str, role: str = "Admin", promotion: str = "",
                 departement: str = "", user_name: str = "", top_k: int = 5) -> List[Dict]:
        conn = self._connect()
        try:
            role_filter = _build_role_filter(role, promotion, departement, user_name)

            # FTS search
            fts_results = self._fts_search(conn, query, role_filter, top_k=20)

            # Vector search
            try:
                client = _get_client()
                query_vec = _embed_query(client, query) if client else []
                vec_results = self._vector_search(conn, query_vec, role_filter, top_k=20) if query_vec else []
            except Exception:
                query_vec = []
                vec_results = []

            # RRF fusion
            rrf_scores = {}
            k = 60

            for rank, r in enumerate(fts_results, start=1):
                rrf_scores[r["id"]] = rrf_scores.get(r["id"], 0.0) + (1.0 / (k + rank))

            for rank, r in enumerate(vec_results, start=1):
                rrf_scores[r["id"]] = rrf_scores.get(r["id"], 0.0) + (1.0 / (k + rank))

            # Merge results
            all_results = {r["id"]: r for r in fts_results}
            all_results.update({r["id"]: r for r in vec_results})

            sorted_ids = sorted(rrf_scores.keys(), key=lambda x: rrf_scores[x], reverse=True)[:top_k]

            results = []
            for rid in sorted_ids:
                r = all_results[rid]
                r["rrf_score"] = round(rrf_scores[rid], 4)
                results.append(r)

            return results
        finally:
            conn.close()
