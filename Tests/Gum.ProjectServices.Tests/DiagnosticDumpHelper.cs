using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Diagnostic helper for inspecting why a Standard appears to differ between a loaded
/// project and the StandardElementsManager reference. Unskip and run to dump the
/// XmlSerialize output for each loaded Standard's DefaultState vs the reference's,
/// and print a unified diff. NOT a real test.
/// </summary>
public class DiagnosticDumpHelper
{
    [Fact(Skip = "Manual diagnostic. Set Skip = null to dump XML diffs.")]
    public void DumpBubblegumXmlDiff()
    {
        string repoRoot = RegenerateStandardsHelper_FindRepoRoot();
        string bubblegumPath = Path.Combine(repoRoot,
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "Bubblegum",
            "GumProject.gumx");

        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loaded = loader.Load(bubblegumPath);
        loaded.Success.ShouldBeTrue();

        GumProjectSave reference = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(reference);

        foreach (StandardElementSave projectStandard in loaded.Project!.StandardElements)
        {
            StandardElementSave? refStandard = reference.StandardElements
                .FirstOrDefault(s => s.Name == projectStandard.Name);
            if (refStandard == null)
            {
                Console.WriteLine($"=== {projectStandard.Name}: project-only, skipping ===");
                continue;
            }

            StateSave projectClone = projectStandard.DefaultState.Clone();
            StateSave refClone = refStandard.DefaultState.Clone();
            projectClone.Variables.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            refClone.Variables.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            FileManager.XmlSerialize(projectClone, out string projectXml);
            FileManager.XmlSerialize(refClone, out string refXml);

            if (projectXml == refXml)
            {
                Console.WriteLine($"=== {projectStandard.Name}: identical ===");
                continue;
            }

            Console.WriteLine($"=== {projectStandard.Name}: DIFFER ===");
            string[] projectLines = projectXml.Split('\n');
            string[] refLines = refXml.Split('\n');
            int max = Math.Max(projectLines.Length, refLines.Length);
            for (int i = 0; i < max; i++)
            {
                string p = i < projectLines.Length ? projectLines[i] : "";
                string r = i < refLines.Length ? refLines[i] : "";
                if (p != r)
                {
                    Console.WriteLine($"  line {i}:");
                    Console.WriteLine($"    project:   {p.TrimEnd()}");
                    Console.WriteLine($"    reference: {r.TrimEnd()}");
                }
            }
        }
    }

    private static string RegenerateStandardsHelper_FindRepoRoot()
    {
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
