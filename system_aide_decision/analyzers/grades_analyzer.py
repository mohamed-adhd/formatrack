"""Grades analyzer — detects low scores, no grades entered, high failure rates."""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))


def analyze(db) -> list[dict]:
    suggestions = []

    # Rule 1: Promotion with average score < 10 (Critique)
    rows = db.query("""
        SELECT s.id_session, f.titre AS formation_titre, s.date_debut, s.date_fin,
               AVG(n.note) AS moyenne, COUNT(n.id_note) AS nb_notes
        FROM sessions s
        INNER JOIN formations f ON s.id_formation = f.id_formation
        INNER JOIN notes n ON n.id_session = s.id_session
        WHERE s.statut = 'EnCours'
        GROUP BY s.id_session
        HAVING AVG(n.note) < 10 AND COUNT(n.id_note) >= 5
    """)
    for r in rows:
        suggestions.append({
            "titre": f"Score promotion faible — {r['formation_titre']}",
            "description": f"La moyenne générale de la session (ID {r['id_session']}) est de {r['moyenne']:.1f}/20 "
                           f"avec {r['nb_notes']} notes saisies. Seuil critique : < 10/20.",
            "priorite": 1,
            "categorie": "notes",
            "action_page": "Grades",
            "action_params": "",
        })

    # Rule 2: Session with >30% failure rate (Attention)
    rows = db.query("""
        SELECT s.id_session, f.titre AS formation_titre,
               COUNT(n.id_note) AS total_notes,
               SUM(CASE WHEN n.note < 10 THEN 1 ELSE 0 END) AS echecs
        FROM sessions s
        INNER JOIN formations f ON s.id_formation = f.id_formation
        INNER JOIN notes n ON n.id_session = s.id_session
        WHERE s.statut = 'EnCours'
        GROUP BY s.id_session
        HAVING CAST(SUM(CASE WHEN n.note < 10 THEN 1 ELSE 0 END) AS REAL) / COUNT(n.id_note) > 0.3
               AND COUNT(n.id_note) >= 5
    """)
    for r in rows:
        pct = (r['echecs'] / r['total_notes']) * 100
        suggestions.append({
            "titre": f"Taux d'échec élevé — {r['formation_titre']}",
            "description": f"Session ID {r['id_session']}: {pct:.0f}% des notes sont inférieures à 10/20 "
                           f"({r['echecs']}/{r['total_notes']} notes).",
            "priorite": 2,
            "categorie": "notes",
            "action_page": "Grades",
            "action_params": "",
        })

    # Rule 3: Active session with zero notes (Attention)
    rows = db.query("""
        SELECT s.id_session, f.titre AS formation_titre, s.date_fin
        FROM sessions s
        INNER JOIN formations f ON s.id_formation = f.id_formation
        LEFT JOIN notes n ON n.id_session = s.id_session
        WHERE s.statut = 'EnCours' AND n.id_note IS NULL
    """)
    for r in rows:
        suggestions.append({
            "titre": f"Aucune note saisie — {r['formation_titre']}",
            "description": f"La session ID {r['id_session']} (se termine le {r['date_fin']}) "
                           f"n'a aucune note enregistrée.",
            "priorite": 2,
            "categorie": "notes",
            "action_page": "Grades",
            "action_params": "",
        })

    # Rule 4: Individual student average < 10 (Info)
    rows = db.query("""
        SELECT u.nom || ' ' || u.prenom AS stagiaire, u.promotion,
               AVG(n.note) AS moyenne, COUNT(n.id_note) AS nb_notes
        FROM utilisateurs u
        INNER JOIN notes n ON n.id_stagiaire = u.id_utilisateur
        WHERE u.role = 'Stagiaire'
        GROUP BY u.id_utilisateur
        HAVING AVG(n.note) < 10 AND COUNT(n.id_note) >= 3
    """)
    for r in rows:
        suggestions.append({
            "titre": f"Stagiaire en échec — {r['stagiaire']}",
            "description": f"{r['stagiaire']} ({r['promotion']}) a une moyenne de {r['moyenne']:.1f}/20 "
                           f"sur {r['nb_notes']} notes.",
            "priorite": 3,
            "categorie": "notes",
            "action_page": "Grades",
            "action_params": "",
        })

    return suggestions
