using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class ProjectLoaderExtendedStandardTests
{
    private readonly ProjectLoader _sut = new();

    [Fact]
    public void Load_ShouldLoadProject_WhenStandardElementIsArc()
    {
        AssertExtendedStandardLoadsCleanly("Arc");
    }

    [Fact]
    public void Load_ShouldLoadProject_WhenStandardElementIsCanvas()
    {
        AssertExtendedStandardLoadsCleanly("Canvas");
    }

    [Fact]
    public void Load_ShouldLoadProject_WhenStandardElementIsColoredCircle()
    {
        AssertExtendedStandardLoadsCleanly("ColoredCircle");
    }

    [Fact]
    public void Load_ShouldLoadProject_WhenStandardElementIsLine()
    {
        AssertExtendedStandardLoadsCleanly("Line");
    }

    [Fact]
    public void Load_ShouldLoadProject_WhenStandardElementIsLottieAnimation()
    {
        AssertExtendedStandardLoadsCleanly("LottieAnimation");
    }

    [Fact]
    public void Load_ShouldLoadProject_WhenStandardElementIsRoundedRectangle()
    {
        AssertExtendedStandardLoadsCleanly("RoundedRectangle");
    }

    [Fact]
    public void Load_ShouldLoadProject_WhenStandardElementIsSvg()
    {
        AssertExtendedStandardLoadsCleanly("Svg");
    }

    private void AssertExtendedStandardLoadsCleanly(string standardTypeName)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumLoaderTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            ProjectCreator creator = new ProjectCreator();
            creator.Create(gumxPath);

            string standardsDir = Path.Combine(tempDir, "Standards");
            string gutxPath = Path.Combine(standardsDir, $"{standardTypeName}.gutx");
            File.WriteAllText(gutxPath, $"""
                <?xml version="1.0" encoding="utf-8"?>
                <StandardElementSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                  <Name>{standardTypeName}</Name>
                  <State>
                    <Name>Default</Name>
                  </State>
                  <Behaviors />
                </StandardElementSave>
                """);

            string gumxContent = File.ReadAllText(gumxPath);
            string standardRef = $"""  <StandardElementReference Name="{standardTypeName}" />""";
            gumxContent = gumxContent.Replace("</GumProjectSave>", standardRef + "\n</GumProjectSave>");
            File.WriteAllText(gumxPath, gumxContent);

            ProjectLoadResult result = _sut.Load(gumxPath);

            result.Success.ShouldBeTrue(result.ErrorMessage);
            result.ErrorMessage.ShouldBeNull();
            result.LoadErrors.ShouldNotContain(e => e.Message.Contains("default state"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
