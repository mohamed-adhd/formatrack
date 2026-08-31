"""Sessions analyzer — detects missing evaluations, ending soon, low enrollment."""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))


def analyze(db) -> list[dict]:
    suggestions = []

    # Rule 1: Active session with 0 published questionnaires (Attention)
    rows = db.query("""
        SELECT s.id_session, f.titre AS formation_titre, s.date_fin
        FROM sessions s
        INNER JOIN formations f ON s.id_formation = f.id_formation
        WHERE s.statut = 'EnCours'
        AND NOT EXISTS (
            SELECT 1 FROM questionnaires q
            WHERE q.id_session = s.id_session AND q.statut = 'Publie'
        )
    """)
    for r in rows:
        suggestions.append({
            "titre": f"Session sans évaluation — {r['formation_titre']}",
            "description": f"La session ID {r['id_session']} (se termine le {r['date_fin']}) "
                           f"n'a aucune évaluation publiée.",
            "priorite": 2,
            "categorie": "sessions",
            "action_page": "Questionnaires",
            "action_params": "",
        })

    # Rule 2: Session ending within 14 days with no final grades (Attention)
    rows = db.query("""
        SELECT s.id_session, f.titre AS formation_titre, s.date_fin,
               COUNT(n.id_note) AS nb_notes
        FROM sessions s
        INNER JOIN formations f ON s.id_formation = f.id_formation
        LEFT JOIN notes n ON n.id_session = s.id_session
        WHERE s.statut = 'EnCours'
        AND date(s.date_fin) BETWEEN date('now') AND date('now', '+14 days')
        GROUP BY s.id_session
        HAVING COUNT(n.id_note) = 0
    """)
    for r in rows:
        suggestions.append({
            "titre": f"Session bientôt terminée — {r['formation_titre']}",
            "description": f"La session ID {r['id_session']} se termine le {r['date_fin']} "
                           f"mais aucune note n'a été saisie.",
            "priorite": 2,
            "categorie": "sessions",
            "action_page": "Sessions",
            "action_params": "",
        })

    # Rule 3: Session with <50% enrollment vs capacity (Info)
    rows = db.query("""
        SELECT s.id_session, f.titre AS formation_titre, s.capacite,
               COUNT(p.id_participation) AS nb_inscrits
        FROM sessions s
        INNER JOIN formations f ON s.id_formation = f.id_formation
        INNER JOIN participation p ON p.id_session = s.id_session AND p.role_participation = 'Stagiaire'
        WHERE s.statut = 'EnCours' AND s.capacite > 0
        GROUP BY s.id_session
        HAVING CAST(COUNT(p.id_participation) AS REAL) / s.capacite < 0.5
    """)
    for r in rows:
        pct = (r['nb_inscrits'] / r['capacite']) * 100
        suggestions.append({
            "titre": f"Sous-capacité — {r['formation_titre']}",
            "description": f"Session ID {r['id_session']}: {r['nb_inscrits']}/{r['capacite']} "
                           f"places occupées ({pct:.0f}%).",
            "priorite": 3,
            "categorie": "sessions",
            "action_page": "Sessions",
            "action_params": "",
        })

    return suggestions
