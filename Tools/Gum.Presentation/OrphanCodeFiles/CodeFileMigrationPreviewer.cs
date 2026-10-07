using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services.Dialogs;

namespace OrphanCodeFilePlugin;

/// <summary>
/// Shows what migrating the code files left at old paths by a code settings change would do, for a
/// finished orphan scan. Read-only: nothing on disk changes.
/// </summary>
public class CodeFileMigrationPreviewer
{
    private readonly ICodeFileMigrationPlanner _planner;
    private readonly CodeFileMigrationPlanFormatter _formatter;
    private readonly IDialogService _dialogService;

    public CodeFileMigrationPreviewer(ICodeFileMigrationPlanner planner, CodeFileMigrationPlanFormatter formatter,
        IDialogService dialogService)
    {
        _planner = planner;
        _formatter = formatter;
        _dialogService = dialogService;
    }

    /// <summary>Plans <paramref name="scan"/>'s orphans and shows the plan, paths relative to the code root.</summary>
    public void ShowPreview(GumProjectSave project, CodeOutputProjectSettings projectSettings, OrphanCodeFileScanResult scan)
    {
        CodeFileMigrationPlan plan = _planner.CreatePlan(project, projectSettings, scan.Orphans);

        // With no code root the scan finds no code files, so the plan is empty and no path is formatted.
        string message = _formatter.Format(plan, scan.CodeRoot ?? string.Empty);
        if (scan.IsTruncated)
        {
            message += "\n" + OrphanCodeFileScanService.GetTruncatedMessage(scan.CodeRoot);
        }

        _dialogService.ShowMessage(message, "Preview Code File Migration");
    }
}
