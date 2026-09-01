"""
RAG Generator: builds prompt with retrieved context, calls LLM to generate answer.
Supports OpenAI and Groq (gsk_ prefix) API keys. Falls back to context-only if LLM unavailable.
"""
import os
from pathlib import Path
from typing import List, Dict

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

    # Detect Groq key (gsk_ prefix) — use Groq's OpenAI-compatible endpoint
    if api_key.startswith("gsk_"):
        return OpenAI(api_key=api_key, base_url="https://api.groq.com/openai/v1")

    return OpenAI(api_key=api_key)


def _get_chat_model(api_key: str) -> str:
    """Pick the right model based on provider."""
    if api_key.startswith("gsk_"):
        return "qwen/qwen3.8-27b"
    return "gpt-4o-mini"


SYSTEM_PROMPT = """Tu es l'assistant IA du CMFPO (Centre Militaire de Formation Professionnel de l'Omrane).

Tu réponds aux questions des stagiaires, formateurs, chefs de département, responsables de formation et administrateurs du centre.

Règles:
- Réponds toujours en français
- Base tes réponses uniquement sur les informations fournies dans le contexte
- Si le contexte ne contient pas l'information demandée, dis-le clairement
- Sois concis et précis
- Pour les notes et évaluations, donne les chiffres exacts
- Pour les emplois du temps, indique les horaires et lieux
- En cas de données manquantes, suggère de contacter le service concerné
- N'invente jamais d'informations"""


def _build_context(retrieved_chunks: List[Dict]) -> str:
    if not retrieved_chunks:
        return "Aucune information trouvée dans la base de données."

    parts = []
    for i, chunk in enumerate(retrieved_chunks, 1):
        source = chunk.get("table", "unknown")
        text = chunk.get("text", "")
        parts.append(f"[{i}] ({source}) {text}")

    return "\n\n".join(parts)


def generate_answer(query: str, retrieved_chunks: List[Dict], role: str = "Stagiaire", user_name: str = "") -> str:
    context = _build_context(retrieved_chunks)

    api_key = _load_api_key()
    client = _get_client()
    if client is None:
        if not retrieved_chunks:
            return ("Service IA non configuré. Ajoutez votre clé API dans system_aide_decision/.env\n"
                    "Clés acceptées: OpenAI (sk-...) ou Groq (gsk_...)")
        return (f"Le service IA est temporairement indisponible. "
                f"Voici les informations trouvées :\n\n{context}")

    user_ident = f"{role} ({user_name})" if user_name else role
    user_prompt = f"""Contexte du CMFPO:
{context}

Question du {user_ident}: {query}

Réponds en te basant sur le contexte ci-dessus."""

    try:
        model = _get_chat_model(api_key)
        response = client.chat.completions.create(
            model=model,
            messages=[
                {"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": user_prompt}
            ],
            max_tokens=800,
            temperature=0.3
        )
        return response.choices[0].message.content or "Aucune réponse générée."
    except Exception as e:
        if retrieved_chunks:
            return (f"Erreur du service IA ({type(e).__name__}). "
                    f"Voici les données disponibles :\n\n{context}")
        return f"Erreur du service IA: {type(e).__name__}: {e}"
