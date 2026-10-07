using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Turns a <see cref="CodeFileMigrationPlan"/> into the read-only preview text shown to the user.
/// </summary>
public class CodeFileMigrationPlanFormatter
{
    // In display order, each with its heading.
    private static readonly (CodeFileMigrationAction Action, string Heading)[] Sections =
    {
        (CodeFileMigrationAction.RemoveGenerated, "Remove (generated code, rebuilt from the element):"),
        (CodeFileMigrationAction.RemoveUntouchedStub, "Remove (custom code file with no code added):"),
        (CodeFileMigrationAction.MoveCustomCode, "Move (custom code file with your code; its namespace and class name are updated):"),
        (CodeFileMigrationAction.SkipConflict, "Leave alone, both files have code (merge them by hand):"),
        (CodeFileMigrationAction.SkipNoElement, "Leave alone, no matching element (use Delete File in the Errors tab):"),
    };

    /// <summary>
    /// Formats <paramref name="plan"/>, grouped by action, with paths relative to <paramref name="baseDirectory"/>.
    /// </summary>
    public string Format(CodeFileMigrationPlan plan, string baseDirectory)
    {
        ///////////////////Early Out///////////////////
        if (plan.Steps.Count == 0)
        {
            return "No code files need migrating.";
        }
        /////////////////End Early Out/////////////////

        int leftAlone = plan.Steps.Count(step => IsSkip(step.Action));
        int changed = plan.Steps.Count - leftAlone;

        StringBuilder text = new StringBuilder();
        // The old settings are unknown after a pull or upgrade, so the cause is named generally and
        // the list below carries each file's from -> to.
        text.Append("This will migrate your generated and custom code files to where your current code settings put them. " +
            "This is needed because your code generation settings changed (in the Code tab, through a pull, or with a " +
            "Gum upgrade) but the files were never migrated, so the old ones are left over.\n\n");
        text.Append($"{changed} file(s) would change and {leftAlone} would be left alone. Nothing has been changed yet.\n");

        foreach ((CodeFileMigrationAction action, string heading) in Sections)
        {
            List<CodeFileMigrationStep> steps = plan.Steps.Where(step => step.Action == action).ToList();
            if (steps.Count == 0)
            {
                continue;
            }

            text.Append('\n').Append(heading).Append('\n');
            foreach (CodeFileMigrationStep step in steps)
            {
                text.Append("  ").Append(Relative(step.Source, baseDirectory));
                if (step.Destination != null)
                {
                    text.Append(" -> ").Append(Relative(step.Destination, baseDirectory));
                }
                text.Append('\n');
            }
        }

        return text.ToString();
    }

    private static bool IsSkip(CodeFileMigrationAction action) =>
        action == CodeFileMigrationAction.SkipConflict || action == CodeFileMigrationAction.SkipNoElement;

    private static string Relative(FilePath file, string baseDirectory) =>
        Path.GetRelativePath(baseDirectory, file.FullPath).Replace('\\', '/');
}
