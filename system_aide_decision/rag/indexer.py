"""
Indexer: chunks DB data into text documents, generates embeddings, stores in SQLite.
Uses FTS5 for keyword search + vec0 for vector similarity.
"""
import json
import os
import sqlite3
from pathlib import Path
from typing import List, Dict, Tuple

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
    # Groq keys (gsk_) don't have embeddings endpoint
    if api_key.startswith("gsk_"):
        return None
    return OpenAI(api_key=api_key)


def _embed_texts(client, texts: List[str], model: str = "text-embedding-3-small") -> List[List[float]]:
    if client is None:
        return [[] for _ in texts]
    batch_size = 100
    all_embeddings = []
    for i in range(0, len(texts), batch_size):
        batch = texts[i:i + batch_size]
        resp = client.embeddings.create(input=batch, model=model)
        all_embeddings.extend([d.embedding for d in resp.data])
    return all_embeddings


def _chunk_text(text: str, max_len: int = 500, overlap: int = 50) -> List[str]:
    words = text.split()
    if len(words) <= max_len:
        return [text] if text.strip() else []
    chunks = []
    i = 0
    while i < len(words):
        chunk = " ".join(words[i:i + max_len])
        if chunk.strip():
            chunks.append(chunk)
        i += max_len - overlap
    return chunks


def _build_document_texts(db: sqlite3.Connection) -> List[Dict]:
    docs = []

    def _safe_query(sql):
        try:
            return db.execute(sql).fetchall()
        except Exception:
            return []

    def _safe_query_params(sql, params):
        try:
            return db.execute(sql, params).fetchall()
        except Exception:
            return []

    # Formations
    for row in _safe_query("SELECT id_formation, titre, description, objectifs, type_formation, statut FROM formations"):
        fid, titre, desc, obj, typ, statut = row
        text = f"Formation: {titre}. Type: {typ or 'N/A'}. Statut: {statut}."
        if desc:
            text += f" Description: {desc}"
        if obj:
            text += f" Objectifs: {obj}"
        docs.append({"table": "formations", "id": fid, "text": text, "meta": {"type": "formation", "titre": titre}})

    # Modules
    for row in _safe_query("SELECT m.id_module, m.titre, m.credit_horaire, m.coefficient, m.est_commum, f.titre FROM modules m JOIN formations f ON m.id_formation = f.id_formation"):
        mid, titre, ch, coeff, commum, ftitre = row
        text = f"Module: {titre} (Formation: {ftitre}). Credit horaire: {ch}h. Coefficient: {coeff}."
        if commum:
            text += " Module commun."
        docs.append({"table": "modules", "id": mid, "text": text, "meta": {"type": "module", "titre": titre, "formation": ftitre}})

    # Sessions
    for row in _safe_query("SELECT s.id_session, f.titre, s.date_debut, s.date_fin, s.lieu, s.capacite, s.statut FROM sessions s JOIN formations f ON s.id_formation = f.id_formation"):
        sid, ftitre, dd, df, lieu, cap, statut = row
        text = f"Session: {ftitre} du {dd} au {df}. Lieu: {lieu or 'N/A'}. Capacite: {cap or 'N/A'}. Statut: {statut}."
        docs.append({"table": "sessions", "id": sid, "text": text, "meta": {"type": "session", "formation": ftitre}})

    # Notes with stagiaire + module info
    for row in _safe_query("""
        SELECT n.id_note, u.prenom || ' ' || u.nom, u.promotion, m.titre, n.note, n.date_saisie
        FROM notes n
        JOIN utilisateurs u ON n.id_stagiaire = u.id_utilisateur
        JOIN modules m ON n.id_module = m.id_module
    """):
        nid, nom, promo, mod, note, date = row
        text = f"Note: {nom} (Promotion {promo}) a obtenu {note}/20 en {mod} le {date}."
        docs.append({"table": "notes", "id": nid, "text": text, "meta": {"type": "note", "stagiaire": nom, "promotion": promo, "module": mod}})

    # Evaluations
    for row in _safe_query("""
        SELECT e.id_evaluation, u.prenom || ' ' || u.nom, u.promotion, q.titre, e.score_total, e.pourcentage, e.statut
        FROM evaluations e
        JOIN utilisateurs u ON e.id_utilisateur = u.id_utilisateur
        JOIN questionnaires q ON e.id_questionnaire = q.id_questionnaire
    """):
        eid, nom, promo, qtitre, score, pct, statut = row
        text = f"Evaluation: {nom} (Promotion {promo}) - {qtitre}. Score: {score or 'N/A'}. Pourcentage: {pct or 'N/A'}%. Statut: {statut}."
        docs.append({"table": "evaluations", "id": eid, "text": text, "meta": {"type": "evaluation", "stagiaire": nom, "promotion": promo}})

    # Absences
    for row in _safe_query("""
        SELECT a.id, u.prenom || ' ' || u.nom, u.promotion, a.cours, a.date, a.type, a.duree, a.justifiee, a.motif
        FROM absences_retards a
        JOIN utilisateurs u ON a.utilisateur_id = u.id_utilisateur
    """):
        aid, nom, promo, cours, date, typ, duree, justifiee, motif = row
        statut_j = "justifiee" if justifiee else "non justifiee"
        text = f"Absence: {nom} (Promotion {promo}) - {cours} le {date}. Type: {typ}. Duree: {duree or 'N/A'}. {statut_j}."
        if motif:
            text += f" Motif: {motif}."
        docs.append({"table": "absences_retards", "id": aid, "text": text, "meta": {"type": "absence", "stagiaire": nom, "promotion": promo}})

    # Emplois du temps
    for row in _safe_query("""
        SELECT e.id_emploi, f.titre, e.type_emploi, e.annee, e.promotion, e.description
        FROM emplois_du_temps e
        JOIN formations f ON e.id_formation = f.id_formation
    """):
        eid, ftitre, typ, annee, promo, desc = row
        text = f"Emploi du temps: {ftitre} ({typ}) - Annee {annee}. Promotion: {promo or 'Toutes'}."
        if desc:
            text += f" {desc}"
        docs.append({"table": "emplois_du_temps", "id": eid, "text": text, "meta": {"type": "emploi", "formation": ftitre}})

    # Suggestions
    for row in _safe_query("SELECT id, titre, description, categorie, priorite FROM suggestions_aide"):
        sid, titre, desc, cat, prio = row
        text = f"Suggestion ({cat}): {titre}. {desc}"
        docs.append({"table": "suggestions_aide", "id": sid, "text": text, "meta": {"type": "suggestion", "categorie": cat}})

    # Questionnaires
    for row in _safe_query("""
        SELECT q.id_questionnaire, q.titre, q.description, q.type_evaluation, f.titre, q.statut
        FROM questionnaires q
        JOIN sessions s ON q.id_session = s.id_session
        JOIN formations f ON s.id_formation = f.id_formation
    """):
        qid, titre, desc, typ, ftitre, statut = row
        text = f"Questionnaire: {titre} ({typ or 'N/A'}) - Formation: {ftitre}. Statut: {statut}."
        if desc:
            text += f" {desc}"
        docs.append({"table": "questionnaires", "id": qid, "text": text, "meta": {"type": "questionnaire", "titre": titre}})

    return docs


def init_rag_tables(conn: sqlite3.Connection):
    conn.executescript("""
        CREATE TABLE IF NOT EXISTS kb_chunks (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            source_table TEXT NOT NULL,
            source_id INTEGER NOT NULL,
            chunk_text TEXT NOT NULL,
            metadata TEXT DEFAULT '{}',
            created_at TEXT DEFAULT (datetime('now'))
        );

        CREATE VIRTUAL TABLE IF NOT EXISTS kb_chunks_fts USING fts5(
            chunk_text,
            content='kb_chunks',
            content_rowid='id',
            tokenize='unicode61 remove_diacritics 2'
        );

        CREATE TRIGGER IF NOT EXISTS kb_chunks_ai AFTER INSERT ON kb_chunks BEGIN
            INSERT INTO kb_chunks_fts(rowid, chunk_text) VALUES (new.id, new.chunk_text);
        END;

        CREATE TRIGGER IF NOT EXISTS kb_chunks_ad AFTER DELETE ON kb_chunks BEGIN
            INSERT INTO kb_chunks_fts(kb_chunks_fts, rowid, chunk_text)
            VALUES('delete', old.id, old.chunk_text);
        END;

        CREATE TRIGGER IF NOT EXISTS kb_chunks_au AFTER UPDATE ON kb_chunks BEGIN
            INSERT INTO kb_chunks_fts(kb_chunks_fts, rowid, chunk_text)
            VALUES('delete', old.id, old.chunk_text);
            INSERT INTO kb_chunks_fts(rowid, chunk_text) VALUES (new.id, new.chunk_text);
        END;
    """)

    # Try to create vec0 virtual table for vector search
    try:
        conn.execute("""
            CREATE VIRTUAL TABLE IF NOT EXISTS kb_chunks_vec USING vec0(
                embedding float[1536]
            );
        """)
    except Exception:
        pass


def index_database(db_path: str) -> Dict:
    conn = sqlite3.connect(db_path)
    try:
        if sqlite_vec:
            conn.enable_load_extension(True)
            sqlite_vec.load(conn)
            conn.enable_load_extension(False)
    except Exception:
        pass

    init_rag_tables(conn)

    # Clear old chunks
    conn.execute("DELETE FROM kb_chunks")
    try:
        conn.execute("DELETE FROM kb_chunks_vec")
    except Exception:
        pass
    conn.commit()

    # Build documents
    docs = _build_document_texts(conn)
    if not docs:
        return {"status": "empty", "chunks": 0}

    # Chunk documents
    all_chunks = []
    for doc in docs:
        chunks = _chunk_text(doc["text"])
        for chunk in chunks:
            all_chunks.append({
                "source_table": doc["table"],
                "source_id": doc["id"],
                "chunk_text": chunk,
                "metadata": json.dumps(doc.get("meta", {}))
            })

    # Insert chunks
    for c in all_chunks:
        conn.execute(
            "INSERT INTO kb_chunks (source_table, source_id, chunk_text, metadata) VALUES (?, ?, ?, ?)",
            (c["source_table"], c["source_id"], c["chunk_text"], c["metadata"])
        )
    conn.commit()

    # Generate embeddings
    client = _get_client()
    texts_to_embed = [c["chunk_text"] for c in all_chunks]
    embeddings = _embed_texts(client, texts_to_embed)

    # Store embeddings
    has_vec = False
    try:
        conn.execute("SELECT count(*) FROM kb_chunks_vec LIMIT 1")
        has_vec = True
    except Exception:
        has_vec = False

    if has_vec and embeddings and len(embeddings[0]) > 0:
        for i, emb in enumerate(embeddings):
            if emb:
                try:
                    conn.execute(
                        "INSERT INTO kb_chunks_vec (rowid, embedding) VALUES (?, ?)",
                        (i + 1, json.dumps(emb))
                    )
                except Exception:
                    pass
        conn.commit()

    count = conn.execute("SELECT count(*) FROM kb_chunks").fetchone()[0]
    conn.close()

    return {"status": "ok", "chunks": count, "documents": len(docs), "has_vectors": has_vec and bool(embeddings and embeddings[0])}
