"""Timetable analyzer — detects missing timetables for active formations."""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))


def analyze(db) -> list[dict]:
    suggestions = []

    # Rule 1: Active formation with 0 published timetables (Attention)
    rows = db.query("""
        SELECT f.id_formation, f.titre
        FROM formations f
        WHERE f.statut = 'EnCours'
        AND NOT EXISTS (
            SELECT 1 FROM emplois_du_temps e
            WHERE e.id_formation = f.id_formation AND e.statut = 'Publie'
        )
    """)
    for r in rows:
        suggestions.append({
            "titre": f"Aucun emploi du temps — {r['titre']}",
            "description": f"La formation « {r['titre'] } » est active mais n'a "
                           f"aucun emploi du temps publié.",
            "priorite": 2,
            "categorie": "emplois",
            "action_page": "Timetable",
            "action_params": "",
        })

    # Rule 2: Brouillon timetables exist but none published (Info)
    rows = db.query("""
        SELECT f.id_formation, f.titre,
               COUNT(e.id_emploi) AS nb_brouillons
        FROM formations f
        INNER JOIN emplois_du_temps e ON e.id_formation = f.id_formation
        WHERE e.statut = 'Brouillon'
        AND NOT EXISTS (
            SELECT 1 FROM emplois_du_temps e2
            WHERE e2.id_formation = f.id_formation AND e2.statut = 'Publie'
        )
        GROUP BY f.id_formation
    """)
    for r in rows:
        suggestions.append({
            "titre": f"Emploi du temps non publié — {r['titre']}",
            "description": f"{r['nb_brouillons']} brouillon(s) existe(nt) pour « {r['titre'] } » "
                           f"mais aucun n'est publié.",
            "priorite": 3,
            "categorie": "emplois",
            "action_page": "Timetable",
            "action_params": "",
        })

    return suggestions
