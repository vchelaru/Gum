using Gum.ProjectServices;
using Shouldly;
using System.Text.Json;

namespace Gum.Cli.Tests;

/// <summary>
/// <c>gumcli check</c> reports the generated code files no element accounts for (GUM0005), the same rows the
/// tool's Errors tab shows (#8097).
/// </summary>
public class CheckOrphanedCodeFilesTests : BaseTestClass
{
    [Fact]
    public void Check_GeneratedFileWithNoElement_ShouldReportItAsAWarningAndExitZero()
    {
        string gumxPath = CreateProjectWithCodeSettings();
        string orphanPath = WriteGeneratedFile("DeletedScreen");

        CliTestHelper result = CliTestHelper.Run("check", gumxPath);

        result.ExitCode.ShouldBe(0, customMessage: result.StandardOutput);
        result.StandardOutput.ShouldContain("warning: DeletedScreen: ");
        result.StandardOutput.ShouldContain("DeletedScreen.Generated.cs");
        File.Exists(orphanPath).ShouldBeTrue("check is read-only");
    }

    [Fact]
    public void Check_Json_ShouldCarryCodeAndDetailsForTheOrphan()
    {
        string gumxPath = CreateProjectWithCodeSettings();
        string orphanPath = WriteGeneratedFile("DeletedScreen");

        CliTestHelper result = CliTestHelper.Run("check", gumxPath, "--json");

        using JsonDocument document = JsonDocument.Parse(result.StandardOutput);
        JsonElement entry = document.RootElement.EnumerateArray().Single();
        entry.GetProperty("code").GetString().ShouldBe("GUM0005");
        entry.GetProperty("severity").GetString().ShouldBe("Warning");
        JsonElement details = entry.GetProperty("details");
        details.GetProperty("kind").GetString().ShouldBe("Generated");
        details.GetProperty("elementExists").GetBoolean().ShouldBeFalse();
        Path.GetFullPath(details.GetProperty("filePath").GetString()!).ShouldBe(Path.GetFullPath(orphanPath));
    }

    [Fact]
    public void Check_GeneratedFileOfAnExistingElement_ShouldReportNothing()
    {
        string gumxPath = CreateProjectWithCodeSettings();
        string screenPath = Path.Combine(TempDirectory, "Screens", "MainScreen.gusx");
        File.WriteAllText(screenPath,
            """
            <?xml version="1.0" encoding="utf-8"?>
            <ScreenSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Name>MainScreen</Name>
              <State><Name>Default</Name></State>
            </ScreenSave>
            """);
        File.WriteAllText(gumxPath, File.ReadAllText(gumxPath).Replace("</GumProjectSave>",
            "  <ScreenReference Name=\"MainScreen\" />\n</GumProjectSave>"));
        WriteGeneratedFile("MainScreen");

        CliTestHelper result = CliTestHelper.Run("check", gumxPath, "--json");

        result.StandardOutput.ShouldNotContain("GUM0005");
    }

    private string CreateProjectWithCodeSettings()
    {
        string gumxPath = Path.Combine(TempDirectory, "MyProject.gumx");
        new ProjectCreator().Create(gumxPath);
        File.WriteAllText(Path.Combine(TempDirectory, "ProjectCodeSettings.codsj"),
            """
            {
              "CodeProjectRoot": "./",
              "RootNamespace": "TestNamespace",
              "OutputLibrary": 5,
              "ObjectInstantiationType": 0,
              "SyntaxVersion": "*"
            }
            """);
        return gumxPath;
    }

    private string WriteGeneratedFile(string elementName)
    {
        string screensDirectory = Path.Combine(TempDirectory, "Screens");
        Directory.CreateDirectory(screensDirectory);
        string path = Path.Combine(screensDirectory, elementName + ".Generated.cs");
        File.WriteAllText(path, $"//Code for {elementName}\r\npublic partial class {elementName} {{ }}");
        return path;
    }
}
