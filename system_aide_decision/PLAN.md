# Systeme d'Aide a la Decision — Plan d'Implementation

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                  Avalonia App (C#)                   │
│                                                      │
│  Dashboard ──> SuggestionPanel ──> Click ──> Page     │
│      │                                              │
│      └── reads suggestions_aide table                │
│                                                      │
│  On dashboard load / refresh button:                 │
│      Process.Start("python3", "engine.py <db_path>") │
│                                                      │
└──────────────────────┬──────────────────────────────┘
                       │ writes to
                       ▼
              ┌─────────────────┐
              │   database.db   │
              │  (SQLite, same) │
              └────────▲────────┘
                       │ reads + analyzes
                       ▼
┌──────────────────────────────────────────────────────┐
│             Python Engine (system_aide_decision/)     │
│                                                      │
│  engine.py  ──> db.py (read SQLite)                  │
│      │                                               │
│      ├── analyzers/grades_analyzer.py                │
│      ├── analyzers/absences_analyzer.py              │
│      ├── analyzers/formations_analyzer.py            │
│      ├── analyzers/sessions_analyzer.py              │
│      ├── analyzers/timetable_analyzer.py             │
│      └── analyzers/evaluations_analyzer.py           │
│                                                      │
│  Output: INSERT INTO suggestions_aide (...)           │
└──────────────────────────────────────────────────────┘
```

## Communication Mechanism

**chosen: Subprocess call** — simplest, no network, no IPC.

- C# calls `python3 system_aide_decision/engine.py <db_path>` via `Process.Start`
- Python reads the same SQLite DB, runs analyzers, writes results to `suggestions_aide` table
- C# reads from `suggestions_aide` table and displays in dashboard
- Triggered on: dashboard load + manual refresh button (avoids continuous scanning overhead)

## New DB Table: `suggestions_aide`

```sql
CREATE TABLE IF NOT EXISTS suggestions_aide (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    titre TEXT NOT NULL,
    description TEXT NOT NULL,
    priorite INTEGER NOT NULL DEFAULT 3,   -- 1=Critique, 2=Attention, 3=Info
    categorie TEXT NOT NULL,                -- notes, absences, formations, sessions, emplois, evaluations
    action_page TEXT NOT NULL,              -- page key to navigate to
    action_params TEXT DEFAULT '',          -- JSON params (e.g., {"id_formation":1})
    est_lu INTEGER NOT NULL DEFAULT 0,
    date_generation TEXT NOT NULL DEFAULT (datetime('now'))
);
```

## Python Engine Analyzers

### 1. `grades_analyzer.py` — Notes & Resultats
| Rule | Condition | Priorite | action_page |
|------|-----------|----------|-------------|
| Promotion score faible | AVG(note) < 10 for any session | 1 (Critique) | Grades |
| Taux d'echec eleve | >30% notes < 10 in a session | 2 (Attention) | Grades |
| Aucune note saisie | Session active with 0 notes | 2 (Attention) | Grades |
| Stagiaire en echec | Individual AVG < 10 | 3 (Info) | Grades |

### 2. `absences_analyzer.py` — Absences & Retards
| Rule | Condition | Priorite | action_page |
|------|-----------|----------|-------------|
| Taux absence eleve | >3 unexcused absences per student | 2 (Attention) | Dashboard |
| Absences non justifiees | Any student with unjustified absences | 3 (Info) | Dashboard |

### 3. `formations_analyzer.py` — Formations
| Rule | Condition | Priorite | action_page |
|------|-----------|----------|-------------|
| Formation sans session | Formation with 0 sessions | 2 (Attention) | Formations |
| Charge horaire excessive | >200 hours in a formation | 3 (Info) | Formations |
| Formation planifiee inactive | Status='Planifiee' with no upcoming sessions | 3 (Info) | Formations |

### 4. `sessions_analyzer.py` — Sessions
| Rule | Condition | Priorite | action_page |
|------|-----------|----------|-------------|
| Session sans evaluation | Active session with 0 published questionnaires | 2 (Attention) | Questionnaires |
| Session bientot terminee | Ends within 14 days + no final grades | 2 (Attention) | Sessions |
| Session sous-capacite | <50% enrollment vs capacity | 3 (Info) | Sessions |

### 5. `timetable_analyzer.py` — Emploi du Temps
| Rule | Condition | Priorite | action_page |
|------|-----------|----------|-------------|
| Aucun emploi du temps | Active formation with 0 published timetables | 2 (Attention) | Timetable |
| Emploi non publie | Brouillon timetables exist but none published | 3 (Info) | Timetable |

### 6. `evaluations_analyzer.py` — Evaluations
| Rule | Condition | Priorite | action_page |
|------|-----------|----------|-------------|
| Score evaluation faible | AVG(pourcentage) < 50% on a questionnaire | 2 (Attention) | Evaluations |
| Evaluation non passee | Published questionnaire with 0 evaluations | 3 (Info) | Evaluations |

## C# Integration Steps

### Step 1: DB Schema + Model
- [ ] Add `suggestions_aide` CREATE TABLE to `AppDbContext.cs`
- [ ] Create `Models/SuggestionAide.cs`
- [ ] Create `Data/Repositories/ISuggestionAideRepository.cs`
- [ ] Create `Data/Repositories/SuggestionAideRepository.cs` (CRUD + mark as read)

### Step 2: Service Layer
- [ ] Create `Services/Interfaces/ISuggestionAideService.cs`
- [ ] Create `Services/SuggestionAideService.cs` (calls Python engine + reads results)
- [ ] Wire into `CompositionRoot.cs`

### Step 3: Python Engine
- [ ] Create `system_aide_decision/engine.py` — main entry, arg parsing, orchestrator
- [ ] Create `system_aide_decision/db.py` — SQLite connection helper
- [ ] Create `system_aide_decision/analyzers/__init__.py`
- [ ] Create all 6 analyzers (grades, absences, formations, sessions, timetable, evaluations)

### Step 4: Dashboard Integration
- [ ] Update `DashboardViewModel.cs` — load suggestions, add refresh command
- [ ] Update `DashboardView.axaml` — add suggestions panel (card list with priority colors)
- [ ] Make suggestions clickable → navigate to action_page with params

### Step 5: Navigation Mapping
| action_page | Navigation Method |
|-------------|-------------------|
| Grades | `OpenGrades()` |
| Formations | `OpenFormations()` |
| Sessions | `OpenSessions()` |
| Questionnaires | `OpenQuestionnaires()` |
| Evaluations | `OpenEvaluations()` |
| Timetable | `OpenTimetable()` |
| Dashboard | `OpenDashboard()` |
| Utilisateurs | `OpenUtilisateurs()` |

### Step 6: Polish
- [ ] Priority color coding: Red=Critique, Orange=Attention, Blue=Info
- [ ] Mark-as-read functionality (click dismisses suggestion)
- [ ] Timestamp display ("il y a 5 min")
- [ ] Empty state when no suggestions

## Files to Create

### Python (system_aide_decision/)
```
system_aide_decision/
├── engine.py              # Main entry point
├── db.py                  # SQLite connection + helpers
└── analyzers/
    ├── __init__.py        # Analyzer registry
    ├── grades_analyzer.py
    ├── absences_analyzer.py
    ├── formations_analyzer.py
    ├── sessions_analyzer.py
    ├── timetable_analyzer.py
    └── evaluations_analyzer.py
```

### C# (formatrack/)
```
formatrack/
├── Models/SuggestionAide.cs
├── Data/Repositories/ISuggestionAideRepository.cs
├── Data/Repositories/SuggestionAideRepository.cs
├── Services/Interfaces/ISuggestionAideService.cs
├── Services/SuggestionAideService.cs
├── ViewModels/Dashboard/DashboardViewModel.cs  (modify)
├── Views/Dashboard/DashboardView.axaml          (modify)
└── Data/AppDbContext.cs                          (modify)
```

## Decisions (Confirmed)

1. **Python** — Python 3.14.5 available on machine. `python3` command works.
2. **Refresh** — Dashboard load + manual refresh button only. No periodic auto-refresh.
3. **Mark as read** — Dimmed/grayed out when read, NOT hidden. Admin can still see them.
4. **Display** — Show ALL suggestions, scrollable, ranked by priority (1=Critique at top, 2=Attention middle, 3=Info bottom). No limit.

## No Blockers

All questions resolved. Ready to implement.
