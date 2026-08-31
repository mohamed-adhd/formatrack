"""Analyzer registry — each analyzer is a function(db) -> list[dict]."""
from . import grades_analyzer
from . import absences_analyzer
from . import formations_analyzer
from . import sessions_analyzer
from . import timetable_analyzer
from . import evaluations_analyzer


def get_all_analyzers():
    """Return list of (name, analyzer_function) tuples."""
    return [
        ("grades", grades_analyzer.analyze),
        ("absences", absences_analyzer.analyze),
        ("formations", formations_analyzer.analyze),
        ("sessions", sessions_analyzer.analyze),
        ("timetable", timetable_analyzer.analyze),
        ("evaluations", evaluations_analyzer.analyze),
    ]
