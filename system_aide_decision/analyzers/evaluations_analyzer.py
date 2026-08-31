"""Evaluations analyzer — detects low scores and unpublished evaluations."""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))


def analyze(db) -> list[dict]:
    suggestions = []

    # Rule 1: Questionnaire with AVG score < 50% (Attention)
    rows = db.query("""
        SELECT q.id_questionnaire, q.titre, q.type_evaluation,
               AVG(e.pourcentage) AS moyenne_pct, COUNT(e.id_evaluation) AS nb_evals
        FROM questionnaires q
        INNER JOIN evaluations e ON e.id_questionnaire = q.id_questionnaire
        WHERE e.statut = 'Terminee'
        GROUP BY q.id_questionnaire
        HAVING AVG(e.pourcentage) < 50 AND COUNT(e.id_evaluation) >= 2
    """)
    for r in rows:
        suggestions.append({
            "titre": f"Score évaluation faible — {r['titre']}",
            "description": f"Évaluation « {r['titre'] } » ({r['type_evaluation']}): "
                           f"moyenne de {r['moyenne_pct']:.1f}% "
                           f"sur {r['nb_evals']} passage(s). Seuil : < 50%.",
            "priorite": 2,
            "categorie": "evaluations",
            "action_page": "Evaluations",
            "action_params": "",
        })

    # Rule 2: Published questionnaire with 0 evaluations taken (Info)
    rows = db.query("""
        SELECT q.id_questionnaire, q.titre, q.type_evaluation
        FROM questionnaires q
        LEFT JOIN evaluations e ON e.id_questionnaire = q.id_questionnaire
        WHERE q.statut = 'Publie'
        GROUP BY q.id_questionnaire
        HAVING COUNT(e.id_evaluation) = 0
    """)
    for r in rows:
        suggestions.append({
            "titre": f"Évaluation non passée — {r['titre']}",
            "description": f"L'évaluation « {r['titre'] } » ({r['type_evaluation']}) "
                           f"est publiée mais n'a été passée par aucun stagiaire.",
            "priorite": 3,
            "categorie": "evaluations",
            "action_page": "Evaluations",
            "action_params": "",
        })

    return suggestions
