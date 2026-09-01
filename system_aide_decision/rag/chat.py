"""
RAG Chat: main entry point — receives user query + role context, retrieves, generates answer.
Called from C# via: python3 -m rag.chat <db_path> <query> [role] [promotion] [departement]
"""
import json
import sys
from pathlib import Path


def chat(db_path: str, query: str, role: str = "Stagiaire",
         promotion: str = "", departement: str = "", user_name: str = "") -> dict:
    from .retriever import HybridRetriever
    from .generator import generate_answer

    retriever = HybridRetriever(db_path)
    chunks = retriever.retrieve(query, role=role, promotion=promotion,
                                departement=departement, user_name=user_name, top_k=5)

    answer = generate_answer(query, chunks, role=role, user_name=user_name)

    sources = []
    for c in chunks:
        meta = c.get("metadata", {})
        sources.append({
            "type": meta.get("type", c.get("table", "unknown")),
            "text": c.get("text", "")[:200],
            "score": c.get("rrf_score", 0)
        })

    return {"answer": answer, "sources": sources}


def main():
    if len(sys.argv) < 3:
        print(json.dumps({"error": "Usage: python3 -m rag.chat <db_path> <query> [role] [promotion] [departement]"}))
        sys.exit(1)

    db_path = sys.argv[1]
    query = sys.argv[2]
    role = sys.argv[3] if len(sys.argv) > 3 else "Stagiaire"
    promotion = sys.argv[4] if len(sys.argv) > 4 else ""
    departement = sys.argv[5] if len(sys.argv) > 5 else ""
    user_name = sys.argv[6] if len(sys.argv) > 6 else ""

    if not Path(db_path).exists():
        print(json.dumps({"error": f"Database not found: {db_path}"}))
        sys.exit(1)

    try:
        result = chat(db_path, query, role=role, promotion=promotion, departement=departement, user_name=user_name)
        print(json.dumps(result, ensure_ascii=False))
    except Exception as e:
        print(json.dumps({
            "answer": f"Erreur interne: {type(e).__name__}: {e}",
            "sources": []
        }, ensure_ascii=False))


if __name__ == "__main__":
    main()
