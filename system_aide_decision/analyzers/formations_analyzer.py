"""Formations analyzer — detects inactive formations, overloaded hours, etc."""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))


def analyze(db) -> list[dict]:
    suggestions = []

    # Rule 1: Formation with 0 sessions (Attention)
    rows = db.query("""
        SELECT f.id_formation, f.titre, f.statut
        FROM formations f
        LEFT JOIN sessions s ON s.id_formation = f.id_formation
        WHERE s.id_session IS NULL
    """)
    for r in rows:
        suggestions.append({
            "titre": f"Formation sans session — {r['titre']}",
            "description": f"La formation « {r['titre']} » (statut : {r['statut']}) "
                           f"n'a aucune session programmée.",
            "priorite": 2,
            "categorie": "formations",
            "action_page": "Formations",
            "action_params": "",
        })

    # Rule 2: Formation with >200 hours (Info)
    rows = db.query("""
        SELECT f.id_formation, f.titre, f.duree_heures
        FROM formations f
        WHERE f.duree_heures > 200
    """)
    for r in rows:
        suggestions.append({
            "titre": f"Charge horaire élevée — {r['titre']}",
            "description": f"La formation « {r['titre'] } » dure {r['duree_heures']} heures, "
                           f"ce qui dépasse le seuil de 200h.",
            "priorite": 3,
            "categorie": "formations",
            "action_page": "Formations",
            "action_params": "",
        })

    # Rule 3: Planified formation with no upcoming sessions (Info)
    rows = db.query("""
        SELECT f.id_formation, f.titre
        FROM formations f
        WHERE f.statut = 'Planifiee'
        AND NOT EXISTS (
            SELECT 1 FROM sessions s
            WHERE s.id_formation = f.id_formation AND s.date_fin >= date('now')
        )
    """)
    for r in rows:
        suggestions.append({
            "titre": f"Formation inactive — {r['titre']}",
            "description": f"La formation « {r['titre'] } » est planifiée mais n'a "
                           f"aucune session à venir.",
            "priorite": 3,
            "categorie": "formations",
            "action_page": "Formations",
            "action_params": "",
        })

    return suggestions
