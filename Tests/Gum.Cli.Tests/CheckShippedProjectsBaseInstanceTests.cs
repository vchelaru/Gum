using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// #7059, #7210: GUM0016 never fires on a project the tool saved, so none of the projects this repo
/// ships (all written by the tool) may report it.
/// </summary>
public class CheckShippedProjectsBaseInstanceTests : BaseTestClass
{
    [Fact]
    public void Load_ShippedProjects_ShouldNotReportGum0016()
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
            found.AddRange(load.LoadErrors
                .Where(error => error.Code == "GUM0016")
                .Select(error => $"{Path.GetRelativePath(root, path)}: {error.Message}"));
        }

        projectCount.ShouldBeGreaterThan(20);
        found.ShouldBeEmpty();
    }
}
