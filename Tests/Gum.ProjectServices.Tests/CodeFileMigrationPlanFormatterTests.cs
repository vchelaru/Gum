using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using System.IO;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="CodeFileMigrationPlanFormatter"/>, the read-only preview text of a
/// <see cref="CodeFileMigrationPlan"/> (issue #5846).
/// </summary>
public class CodeFileMigrationPlanFormatterTests
{
    private static readonly string BaseDirectory = Path.Combine(Path.GetTempPath(), "GumProject") + Path.DirectorySeparatorChar;

    [Fact]
    public void Format_GroupsStepsByAction_WithPathsRelativeToTheBaseDirectory()
    {
        CodeFileMigrationPlan plan = new CodeFileMigrationPlan(new[]
        {
            Step(CodeFileMigrationAction.RemoveGenerated, "Code/ButtonCloseRuntime.Generated.cs"),
            Step(CodeFileMigrationAction.MoveCustomCode, "Code/ButtonCloseRuntime.cs", "Code/ButtonClose.cs"),
            Step(CodeFileMigrationAction.RemoveGenerated, "Code/ButtonDenyRuntime.Generated.cs"),
            Step(CodeFileMigrationAction.RemoveUntouchedStub, "Code/ButtonDenyRuntime.cs"),
            Step(CodeFileMigrationAction.SkipConflict, "Code/IconRuntime.cs", "Code/Icon.cs"),
            Step(CodeFileMigrationAction.SkipNoElement, "Code/Deleted.Generated.cs"),
        });

        string text = new CodeFileMigrationPlanFormatter().Format(plan, BaseDirectory);

        text.ShouldBe(
            "This will migrate your generated and custom code files to where your current code settings put them. " +
            "This is needed because your code generation settings changed (in the Code tab, through a pull, or with a " +
            "Gum upgrade) but the files were never migrated, so the old ones are left over.\n" +
            "\n" +
            "4 file(s) would change and 2 would be left alone. Nothing has been changed yet.\n" +
            "\n" +
            "Remove (generated code, rebuilt from the element):\n" +
            "  Code/ButtonCloseRuntime.Generated.cs\n" +
            "  Code/ButtonDenyRuntime.Generated.cs\n" +
            "\n" +
            "Remove (custom code file with no code added):\n" +
            "  Code/ButtonDenyRuntime.cs\n" +
            "\n" +
            "Move (custom code file with your code; its namespace and class name are updated):\n" +
            "  Code/ButtonCloseRuntime.cs -> Code/ButtonClose.cs\n" +
            "\n" +
            "Leave alone, both files have code (merge them by hand):\n" +
            "  Code/IconRuntime.cs -> Code/Icon.cs\n" +
            "\n" +
            "Leave alone, no matching element (use Delete File in the Errors tab):\n" +
            "  Code/Deleted.Generated.cs\n");
    }

    [Fact]
    public void Format_SaysSo_WhenThereIsNothingToMigrate()
    {
        string text = new CodeFileMigrationPlanFormatter().Format(new CodeFileMigrationPlan(new CodeFileMigrationStep[0]), BaseDirectory);

        text.ShouldBe("No code files need migrating.");
    }

    private static CodeFileMigrationStep Step(CodeFileMigrationAction action, string source, string? destination = null) =>
        new CodeFileMigrationStep("Element", action, new FilePath(BaseDirectory + source),
            destination == null ? null : new FilePath(BaseDirectory + destination));
}
