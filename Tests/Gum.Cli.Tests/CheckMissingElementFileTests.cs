using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// A component whose file is missing is reported as missing (GUM0004), not also as a component with no BaseType:
/// the BaseType was never read (#7059).
/// </summary>
public class CheckMissingElementFileTests : CheckHandWrittenJsonFileHarness
{
    [Fact]
    public void Check_JsonComponentFileMissing_ShouldNotAlsoReportGum0015()
    {
        string project = CreateJsonProject("MissingJsonComponent");
        AddJsonReference(project, "ComponentReferences", "Gone");

        Check(project).Entries.ShouldNotContain(e => e.Code == "GUM0015" && e.Element == "Gone");
    }

    [Fact]
    public void Check_XmlComponentFileMissing_ShouldNotAlsoReportGum0015()
    {
        string project = CreateProject("MissingXmlComponent");
        AddReference(project, """  <ComponentReference Name="Gone" />""");

        Check(project).Entries.ShouldNotContain(e => e.Code == "GUM0015" && e.Element == "Gone");
    }
}
