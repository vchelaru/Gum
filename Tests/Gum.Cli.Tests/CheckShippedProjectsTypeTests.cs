using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// #7119: every instance-variable reference and variable type string in the projects this repo ships
/// resolves, so GUM0027 and GUM0028 never fire on content the tool produced.
/// </summary>
public class CheckShippedProjectsTypeTests : BaseTestClass
{
    [Fact]
    public void Check_ShippedProjects_ShouldNotReportGum0027OrGum0028()
    {
        string? root = AppContext.BaseDirectory;
        while (root != null && !File.Exists(Path.Combine(root, "Gum.slnx")))
        {
            root = Path.GetDirectoryName(root);
        }
        root.ShouldNotBeNull();

        List<string> found = new List<string>();
        int projectCount = 0;
        foreach (string path in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(file => (file.EndsWith(".gumx") || file.EndsWith(".gumj"))
                && !file.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj" or "node_modules" or ".git")))
        {
            ProjectLoadResult load = new ProjectLoader().Load(path);
            if (!load.Success)
            {
                continue;
            }
            projectCount++;
            HeadlessErrorChecker checker = new HeadlessErrorChecker(new DefaultTypeResolver()) { ReportsLeftoverInstanceVariables = true };
            found.AddRange(checker.GetAllErrors(load.Project!)
                .Where(error => error.Code == "GUM0027" || error.Code == "GUM0028")
                .Select(error => $"{Path.GetFileName(path)}: {error.Message}"));
        }

        projectCount.ShouldBeGreaterThan(20);
        found.ShouldBeEmpty();
    }
}
