"""Absences analyzer — detects high absence rates and unjustified absences."""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))


def analyze(db) -> list[dict]:
    suggestions = []

    # Rule 1: Students with >3 unexcused absences (Attention)
    rows = db.query("""
        SELECT u.nom || ' ' || u.prenom AS stagiaire, u.promotion,
               COUNT(a.id) AS nb_absences
        FROM utilisateurs u
        INNER JOIN absences_retards a ON a.utilisateur_id = u.id_utilisateur
        WHERE a.type = 'Absence' AND a.justifiee = 0
        GROUP BY u.id_utilisateur
        HAVING COUNT(a.id) > 3
    """)
    for r in rows:
        suggestions.append({
            "titre": f"Absences non justifiées — {r['stagiaire']}",
            "description": f"{r['stagiaire']} ({r['promotion']}) a {r['nb_absences']} "
                           f"absences non justifiées.",
            "priorite": 2,
            "categorie": "absences",
            "action_page": "Dashboard",
            "action_params": "",
        })

    # Rule 2: Any unjustified absences (Info)
    rows = db.query("""
        SELECT u.nom || ' ' || u.prenom AS stagiaire,
               a.cours, a.date, a.type
        FROM utilisateurs u
        INNER JOIN absences_retards a ON a.utilisateur_id = u.id_utilisateur
        WHERE a.justifiee = 0
        ORDER BY a.date DESC
        LIMIT 5
    """)
    if rows:
        descriptions = []
        for r in rows:
            descriptions.append(f"{r['stagiaire']}: {r['type']} le {r['date']} ({r['cours']})")
        suggestions.append({
            "titre": f"{len(rows)} absence(s)/retard(s) non justifié(s)",
            "description": "Derniers cas : " + " | ".join(descriptions),
            "priorite": 3,
            "categorie": "absences",
            "action_page": "Dashboard",
            "action_params": "",
        })

    return suggestions
