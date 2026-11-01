using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// #7210: every instance variable in the projects this repo ships is known to its instance's type, or is a tolerated
/// leftover, so GUM0029 never fires on content the tool produced.
/// </summary>
public class CheckShippedProjectsUnknownVariableTests : BaseTestClass
{
    // A real defect: the component has no "Float variable with space", so the value is ignored. The fixture is a
    // codegen name-escaping test; fixing it changes generated output, so it is tracked rather than fixed here.
    private const string KnownDefectFixture = "CodeGen_Maui_FullCodegen";

    [Fact]
    public void Check_ShippedProjects_ShouldNotReportGum0029()
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
                && !file.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj" or "node_modules" or ".git" or KnownDefectFixture)))
        {
            ProjectLoadResult load = new ProjectLoader().Load(path);
            if (!load.Success)
            {
                continue;
            }
            projectCount++;
            HeadlessErrorChecker checker = new HeadlessErrorChecker(new DefaultTypeResolver()) { ReportsLeftoverInstanceVariables = true };
            found.AddRange(checker.GetAllErrors(load.Project!)
                .Where(error => error.Code == "GUM0029")
                .Select(error => $"{Path.GetFileName(path)}: {error.Message}"));
        }

        projectCount.ShouldBeGreaterThan(20);
        found.ShouldBeEmpty();
    }
}
