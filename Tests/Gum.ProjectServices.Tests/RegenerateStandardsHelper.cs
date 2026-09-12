using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// One-off helper to regenerate the bundled Templates/FormsThemes/Bubblegum/Standards/*.gutx
/// files from <see cref="StandardElementsManager"/>'s programmatic defaults. Marked Skip by
/// default — unskip and run to overwrite the on-disk Standards in the worktree,
/// then re-skip before committing. NOT a real test.
/// </summary>
/// <remarks>
/// Templates/Default/Standards/*.gutx (ProjectCreator's plain "gumcli new" scaffold) no longer
/// exists as a baked file tree — ProjectCreator builds it directly from
/// <see cref="StandardElementsManager"/> at runtime instead (#4676), so there's nothing to
/// regenerate there anymore.
/// </remarks>
public class RegenerateStandardsHelper
{
    [Fact(Skip = "Manual regeneration helper. Set Skip = null to run.")]
    public void Regenerate()
    {
        StandardElementsManager.Self.Initialize();
        StandardElementsManager.Self.RegisterExtendedDefaultStates();

        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

        // Existing files have variables sorted alphabetically per state — keep that
        // convention so the regenerated output diffs cleanly against history.
        foreach (StandardElementSave standard in project.StandardElements)
        {
            foreach (StateSave state in standard.AllStates)
            {
                state.Variables.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            }
        }

        string repoRoot = FindRepoRoot();
        string bubblegumDir = Path.Combine(repoRoot,
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "Bubblegum", "Standards");

        Directory.Exists(bubblegumDir).ShouldBeTrue($"missing {bubblegumDir}");

        foreach (StandardElementSave standard in project.StandardElements)
        {
            string fileName = standard.Name + "." + GumProjectSave.StandardExtension;
            standard.Save(Path.Combine(bubblegumDir, fileName), useCompactFormat: true);
        }
    }

    private static string FindRepoRoot()
    {
        // Walk up from the test assembly's directory until we find a sibling
        // with the expected templates directory.
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string templatesDir = Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates");
            if (Directory.Exists(templatesDir))
            {
                return current;
            }
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current) break;
            current = parent;
        }
        throw new InvalidOperationException("could not locate repo root from " + AppContext.BaseDirectory);
    }
}
