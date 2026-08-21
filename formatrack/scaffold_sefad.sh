#!/usr/bin/env bash
# SEFAD - Avalonia/C# scaffolding script
# Run this from inside your "formatrack" project root (where formatrack.csproj lives).
# Creates directories + empty skeleton files only (no logic/content filled in).

set -e

NS="formatrack" # change if your root namespace differs

# ---------- helper ----------
mkcs() {
  # $1 = path, $2 = namespace suffix, $3 = class name, $4 = base/interface (optional)
  local path="$1" ns="$2" cls="$3" base="${4:-}"
  local inherit=""
  [ -n "$base" ] && inherit=" : $base"
  mkdir -p "$(dirname "$path")"
  cat > "$path" <<EOF
namespace $NS.$ns;

public class $cls$inherit
{
}
EOF
}

mkinterface() {
  local path="$1" ns="$2" cls="$3"
  mkdir -p "$(dirname "$path")"
  cat > "$path" <<EOF
namespace $NS.$ns;

public interface $cls
{
}
EOF
}

mkaxaml() {
  # $1 = axaml path, $2 = namespace suffix, $3 = control class name
  local path="$1" ns="$2" cls="$3"
  mkdir -p "$(dirname "$path")"
  cat > "$path" <<EOF
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:$NS.ViewModels.$ns"
             x:Class="$NS.Views.$ns.$cls"
             x:DataType="vm:${cls%View}ViewModel">

</UserControl>
EOF
  cat > "${path}.cs" <<EOF
using Avalonia.Controls;

namespace $NS.Views.$ns;

public partial class $cls : UserControl
{
    public $cls()
    {
        InitializeComponent();
    }
}
EOF
}

# =========================================================
# MODELS
# =========================================================
for m in Utilisateur Formation Session Questionnaire Question Reponse Evaluation Critere Participation; do
  mkcs "Models/$m.cs" "Models" "$m"
done

mkdir -p Models/Enums
for e in RoleUtilisateur StatutFormation StatutSession TypeQuestion TypeEvaluation StatutEvaluation; do
  cat > "Models/Enums/$e.cs" <<EOF
namespace $NS.Models.Enums;

public enum $e
{
}
EOF
done

# =========================================================
# DATA (EF Core / repositories)
# =========================================================
mkcs "Data/AppDbContext.cs" "Data" "AppDbContext"
mkinterface "Data/Repositories/IRepository.cs" "Data.Repositories" "IRepository<T>"
mkcs "Data/Repositories/Repository.cs" "Data.Repositories" "Repository<T>"
for r in Utilisateur Formation Session Questionnaire Question Reponse Evaluation Critere Participation; do
  mkinterface "Data/Repositories/I${r}Repository.cs" "Data.Repositories" "I${r}Repository"
  mkcs "Data/Repositories/${r}Repository.cs" "Data.Repositories" "${r}Repository"
done

# =========================================================
# SERVICES (business logic + API client to Python module)
# =========================================================
mkdir -p Services/Interfaces
for s in Auth Utilisateur Formation Session Questionnaire Evaluation Statistique Navigation Dialog DecisionSupportApi; do
  mkinterface "Services/Interfaces/I${s}Service.cs" "Services.Interfaces" "I${s}Service"
  mkcs "Services/${s}Service.cs" "Services" "${s}Service" "I${s}Service"
done

# =========================================================
# VIEWMODELS (existing ViewModelBase / MainWindowViewModel / LoginViewModel kept as-is)
# =========================================================
mkdir -p ViewModels/Dashboard
mkcs "ViewModels/Dashboard/DashboardViewModel.cs" "ViewModels.Dashboard" "DashboardViewModel" "ViewModelBase"

mkdir -p ViewModels/Utilisateurs
for v in UtilisateursListViewModel UtilisateurDetailViewModel UtilisateurFormViewModel; do
  mkcs "ViewModels/Utilisateurs/$v.cs" "ViewModels.Utilisateurs" "$v" "ViewModelBase"
done

mkdir -p ViewModels/Formations
for v in FormationsListViewModel FormationDetailViewModel FormationFormViewModel; do
  mkcs "ViewModels/Formations/$v.cs" "ViewModels.Formations" "$v" "ViewModelBase"
done

mkdir -p ViewModels/Sessions
for v in SessionsListViewModel SessionDetailViewModel SessionFormViewModel; do
  mkcs "ViewModels/Sessions/$v.cs" "ViewModels.Sessions" "$v" "ViewModelBase"
done

mkdir -p ViewModels/Questionnaires
for v in QuestionnairesListViewModel QuestionnaireEditorViewModel QuestionEditorViewModel; do
  mkcs "ViewModels/Questionnaires/$v.cs" "ViewModels.Questionnaires" "$v" "ViewModelBase"
done

mkdir -p ViewModels/Evaluations
for v in EvaluationsListViewModel EvaluationPasserViewModel EvaluationResultatViewModel; do
  mkcs "ViewModels/Evaluations/$v.cs" "ViewModels.Evaluations" "$v" "ViewModelBase"
done

mkdir -p ViewModels/Statistiques
for v in StatistiquesViewModel RapportViewModel; do
  mkcs "ViewModels/Statistiques/$v.cs" "ViewModels.Statistiques" "$v" "ViewModelBase"
done

mkdir -p ViewModels/Shared
mkcs "ViewModels/Shared/NavigationItemViewModel.cs" "ViewModels.Shared" "NavigationItemViewModel" "ViewModelBase"

# =========================================================
# VIEWS (mirrors ViewModels; MainWindow.axaml kept as-is)
# =========================================================
mkaxaml "Views/Dashboard/DashboardView.axaml" "Dashboard" "DashboardView"

for v in UtilisateursListView UtilisateurDetailView UtilisateurFormView; do
  mkaxaml "Views/Utilisateurs/$v.axaml" "Utilisateurs" "$v"
done

for v in FormationsListView FormationDetailView FormationFormView; do
  mkaxaml "Views/Formations/$v.axaml" "Formations" "$v"
done

for v in SessionsListView SessionDetailView SessionFormView; do
  mkaxaml "Views/Sessions/$v.axaml" "Sessions" "$v"
done

for v in QuestionnairesListView QuestionnaireEditorView QuestionEditorView; do
  mkaxaml "Views/Questionnaires/$v.axaml" "Questionnaires" "$v"
done

for v in EvaluationsListView EvaluationPasserView EvaluationResultatView; do
  mkaxaml "Views/Evaluations/$v.axaml" "Evaluations" "$v"
done

for v in StatistiquesView RapportView; do
  mkaxaml "Views/Statistiques/$v.axaml" "Statistiques" "$v"
done

echo "SEFAD Avalonia scaffold created."
