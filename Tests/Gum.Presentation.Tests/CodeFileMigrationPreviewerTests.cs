using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services.Dialogs;
using Moq;
using OrphanCodeFilePlugin;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Tests for <see cref="CodeFileMigrationPreviewer"/>, which shows the read-only migration preview
/// for a finished orphan scan (issue #5846).
/// </summary>
public class CodeFileMigrationPreviewerTests
{
    [Fact]
    public void ShowPreview_ShowsThePlanRelativeToTheCodeRoot_AndTheTruncationWarning()
    {
        string codeRoot = Path.Combine(Path.GetTempPath(), "GumGame") + Path.DirectorySeparatorChar;
        GumProjectSave project = new GumProjectSave();
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings();
        OrphanCodeFile orphan = new OrphanCodeFile(new FilePath(codeRoot + "A.Generated.cs"), OrphanCodeFileKind.Generated, "A");
        OrphanCodeFileScanResult scan = new OrphanCodeFileScanResult(new[] { orphan }, isTruncated: true, codeRoot);
        CodeFileMigrationPlan plan = new CodeFileMigrationPlan(new[]
        {
            new CodeFileMigrationStep("A", CodeFileMigrationAction.RemoveGenerated, orphan.FilePath),
        });
        Mock<ICodeFileMigrationPlanner> planner = new Mock<ICodeFileMigrationPlanner>();
        planner.Setup(p => p.CreatePlan(project, settings, scan.Orphans)).Returns(plan);
        Mock<IDialogService> dialogService = new Mock<IDialogService>();

        new CodeFileMigrationPreviewer(planner.Object, new CodeFileMigrationPlanFormatter(), dialogService.Object)
            .ShowPreview(project, settings, scan);

        string expected =
            "This will migrate your generated and custom code files to where your current code settings put them. " +
            "This is needed because your code generation settings changed (in the Code tab, through a pull, or with a " +
            "Gum upgrade) but the files were never migrated, so the old ones are left over.\n" +
            "\n" +
            "1 file(s) would change and 0 would be left alone. Nothing has been changed yet.\n" +
            "\n" +
            "Remove (generated code, rebuilt from the element):\n" +
            "  A.Generated.cs\n" +
            "\n" +
            OrphanCodeFileScanService.GetTruncatedMessage(codeRoot);
        dialogService.Verify(d => d.ShowMessage(expected, "Preview Code File Migration", null));
    }
}
