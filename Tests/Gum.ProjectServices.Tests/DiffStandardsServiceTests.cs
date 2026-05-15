using System.Reflection;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class DiffStandardsServiceTests : IDisposable
{
    private readonly DiffStandardsService _sut;
    private readonly string _projectDirectory;
    private readonly string _projectFilePath;
    private readonly string _standardsDirectory;

    public DiffStandardsServiceTests()
    {
        _sut = new DiffStandardsService();

        _projectDirectory = Path.Combine(
            Path.GetTempPath(),
            "GumDiffStandardsTests_" + Guid.NewGuid().ToString("N"));
        _standardsDirectory = Path.Combine(_projectDirectory, "Standards");
        Directory.CreateDirectory(_standardsDirectory);

        _projectFilePath = Path.Combine(_projectDirectory, "Project.gumx");
        File.WriteAllText(_projectFilePath, "<?xml version=\"1.0\"?><GumProjectSave />");

        ExtractDefaultStandardsToDisk(_standardsDirectory);
    }

    [Fact]
    public void Diff_ProjectWithDefaultStandards_ShouldReportNoDrift()
    {
        // The project's Standards directory is populated verbatim from the embedded
        // Default resources, so diff must find no drift. This is the "Default vs itself"
        // baseline the user asked for.
        DiffStandardsResult result = _sut.Diff(_projectFilePath);

        result.HasDrift.ShouldBeFalse();
        result.Differences.ShouldBeEmpty();
        result.MissingFromProject.ShouldBeEmpty();
        result.ProjectOnlyStandards.ShouldBeEmpty();
    }

    [Fact]
    public void Diff_ChangedVariableValue_ShouldReportChangedDiff()
    {
        // Flip Font from Arial to ComicSans in the on-disk Text.gutx.
        string textPath = Path.Combine(_standardsDirectory, "Text.gutx");
        string content = File.ReadAllText(textPath);
        File.WriteAllText(textPath, content.Replace(">Arial<", ">ComicSans<"));

        DiffStandardsResult result = _sut.Diff(_projectFilePath);

        result.HasDrift.ShouldBeTrue();
        StandardVariableDiff diff = result.Differences.ShouldHaveSingleItem();
        diff.StandardName.ShouldBe("Text");
        diff.VariableName.ShouldBe("Font");
        diff.Kind.ShouldBe(StandardVariableDiffKind.Changed);
        diff.DefaultValue.ShouldBe("Arial");
        diff.ProjectValue.ShouldBe("ComicSans");
    }

    [Fact]
    public void Diff_VariableAddedInProject_ShouldReportAddedDiff()
    {
        string textPath = Path.Combine(_standardsDirectory, "Text.gutx");
        string content = File.ReadAllText(textPath);
        const string injected =
            "    <Variable Type=\"string\" Name=\"NotInDefault\" SetsValue=\"true\">\n" +
            "      <Value xsi:type=\"xsd:string\">x</Value>\n" +
            "    </Variable>\n" +
            "  </State>";
        content = content.Replace("  </State>", injected);
        File.WriteAllText(textPath, content);

        DiffStandardsResult result = _sut.Diff(_projectFilePath);

        StandardVariableDiff diff = result.Differences.ShouldHaveSingleItem();
        diff.Kind.ShouldBe(StandardVariableDiffKind.AddedInProject);
        diff.VariableName.ShouldBe("NotInDefault");
        diff.DefaultValue.ShouldBe("(absent)");
        diff.ProjectValue.ShouldBe("x");
    }

    [Fact]
    public void Diff_StandardMissingFromProject_ShouldReportMissing()
    {
        File.Delete(Path.Combine(_standardsDirectory, "Text.gutx"));

        DiffStandardsResult result = _sut.Diff(_projectFilePath);

        result.MissingFromProject.ShouldBe(new[] { "Text" });
        result.HasDrift.ShouldBeTrue();
    }

    [Fact]
    public void Diff_ProjectOnlyStandard_ShouldBeListedButNotDiffed()
    {
        string extraPath = Path.Combine(_standardsDirectory, "RoundedRectangle.gutx");
        File.WriteAllText(extraPath,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<StandardElementSave xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" " +
            "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\n" +
            "  <Name>RoundedRectangle</Name>\n" +
            "  <State>\n" +
            "    <Name>Default</Name>\n" +
            "  </State>\n" +
            "</StandardElementSave>\n");

        DiffStandardsResult result = _sut.Diff(_projectFilePath);

        result.ProjectOnlyStandards.ShouldBe(new[] { "RoundedRectangle" });
        result.Differences.ShouldBeEmpty();
        result.HasDrift.ShouldBeFalse();
    }

    [Fact]
    public void Diff_NoStandardsDirectory_ShouldReportAllMissing()
    {
        Directory.Delete(_standardsDirectory, recursive: true);

        DiffStandardsResult result = _sut.Diff(_projectFilePath);

        result.MissingFromProject.Count.ShouldBe(DiffStandardsService.DefaultStandardNames.Count);
        result.Differences.ShouldBeEmpty();
    }

    /// <summary>
    /// Writes each embedded Default Standard resource to <paramref name="standardsDirectory"/>
    /// so the test project has a byte-identical Default baseline. Mutating a single file
    /// after this call produces a known-shape diff.
    /// </summary>
    private static void ExtractDefaultStandardsToDisk(string standardsDirectory)
    {
        Assembly assembly = typeof(DiffStandardsService).Assembly;
        foreach (string name in DiffStandardsService.DefaultStandardNames)
        {
            string resourceName = $"Gum.ProjectServices.Templates.Default.Standards.{name}.gutx";
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            stream.ShouldNotBeNull($"missing embedded resource {resourceName}");

            string outputPath = Path.Combine(standardsDirectory, $"{name}.gutx");
            using FileStream fileStream = File.Create(outputPath);
            stream.CopyTo(fileStream);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_projectDirectory))
        {
            Directory.Delete(_projectDirectory, recursive: true);
        }
    }
}
