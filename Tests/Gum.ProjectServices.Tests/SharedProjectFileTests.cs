using System.Xml.Linq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class SharedProjectFileTests
{
    private static readonly XNamespace MsBuildNs = "http://schemas.microsoft.com/developer/msbuild/2003";

    [Fact]
    public void GumCoreShared_VsSpecificImports_HaveExistsCondition()
    {
        // GumCoreShared.shproj is a legacy VS Shared Project. Its <Import>
        // elements referencing VS-only CodeSharing targets must be guarded with
        // Exists() conditions so the file can be evaluated outside Visual Studio
        // (Rider, dotnet CLI). Without the guards, MSBuild throws an unhandled
        // exception when the VS CodeSharing components are missing.
        string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string shprojPath = Path.Combine(repoRoot, "GumCoreShared.shproj");

        File.Exists(shprojPath).ShouldBeTrue($"Expected to find {shprojPath}");

        XDocument doc = XDocument.Load(shprojPath);
        XElement project = doc.Root!;

        List<XElement> imports = project.Elements(MsBuildNs + "Import").ToList();

        imports.ShouldNotBeEmpty("GumCoreShared.shproj should contain <Import> elements");

        // Only check imports that reference VS-specific paths (CodeSharing,
        // VisualStudio). The projitems import and MSBuild common props are
        // always available and do not need Exists() guards.
        foreach (XElement import in imports)
        {
            string projectPath = import.Attribute("Project")?.Value ?? "";
            bool isVsSpecific =
                projectPath.Contains("VisualStudio", StringComparison.OrdinalIgnoreCase) ||
                projectPath.Contains("CodeSharing", StringComparison.OrdinalIgnoreCase);

            if (isVsSpecific)
            {
                string condition = import.Attribute("Condition")?.Value ?? "";
                condition.ShouldNotBeNullOrWhiteSpace(
                    $"VS-specific import \"{projectPath}\" must have a Condition attribute " +
                    "with an Exists() guard so GumCoreShared.shproj can be evaluated " +
                    "outside Visual Studio (e.g. Rider, dotnet CLI).");
                condition.Contains("Exists(").ShouldBeTrue(
                    $"VS-specific import \"{projectPath}\" Condition should use Exists() " +
                    "to gracefully skip missing VS components outside Visual Studio.");
            }
        }
    }
}
