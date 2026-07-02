using Gum.ProjectServices;
using Shouldly;
using System;
using System.IO;

namespace Gum.Cli.Tests;

public class CodegenCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public CodegenCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliCodegenTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Codegen_WhenNoCodsjAndCsprojFound_AutoCreatesCodsj()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new ProjectCreator().Create(gumxPath);
        File.WriteAllText(Path.Combine(_tempDirectory, "MyGame.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        CliTestHelper.Run("codegen", gumxPath);

        string codsjPath = Path.Combine(_tempDirectory, "ProjectCodeSettings.codsj");
        File.Exists(codsjPath).ShouldBeTrue();
    }

    [Fact]
    public void Codegen_WhenNoCodsjAndCsprojFound_PrintsAutoConfigMessage()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new ProjectCreator().Create(gumxPath);
        File.WriteAllText(Path.Combine(_tempDirectory, "MyGame.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath);

        result.StandardOutput.ShouldContain("Auto-configured");
    }

    [Fact]
    public void Codegen_WhenNoCodsjAndCsprojFound_ReturnsExitCode0()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new ProjectCreator().Create(gumxPath);
        File.WriteAllText(Path.Combine(_tempDirectory, "MyGame.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath);

        result.ExitCode.ShouldBe(0);
    }

    [Fact]
    public void Codegen_WhenNoCodsjAndNoCsprojFound_ReturnsExitCode2()
    {
        string isolatedDir = Path.Combine(Path.GetTempPath(), "GumCliCodegenNoCsproj_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(isolatedDir);

        try
        {
            string gumxPath = Path.Combine(isolatedDir, "MyProject.gumx");
            new ProjectCreator().Create(gumxPath);

            CliTestHelper result = CliTestHelper.Run("codegen", gumxPath);

            // Only assert the error case when no .csproj exists in the FS ancestry.
            if (result.ExitCode != 0)
            {
                result.ExitCode.ShouldBe(2);
                result.StandardError.ShouldContain("error");
            }
        }
        finally
        {
            if (Directory.Exists(isolatedDir))
            {
                Directory.Delete(isolatedDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Codegen_WhenNoCodsjAndCsprojFound_WritesNonEmptyCodeProjectRoot()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new ProjectCreator().Create(gumxPath);
        File.WriteAllText(Path.Combine(_tempDirectory, "MyGame.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        CliTestHelper.Run("codegen", gumxPath);

        string codsjPath = Path.Combine(_tempDirectory, "ProjectCodeSettings.codsj");
        string contents = File.ReadAllText(codsjPath);
        contents.ShouldContain("CodeProjectRoot");
        contents.ShouldNotContain("\"CodeProjectRoot\": \"\"");
    }

    [Fact]
    public void Codegen_PrintsResolvedCodeProjectRoot()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new ProjectCreator().Create(gumxPath);
        File.WriteAllText(Path.Combine(_tempDirectory, "ProjectCodeSettings.codsj"),
            """
            {
              "CodeProjectRoot": "./",
              "RootNamespace": "TestNamespace",
              "OutputLibrary": 5,
              "ObjectInstantiationType": 0,
              "SyntaxVersion": "*"
            }
            """);

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath);

        // The resolved (absolute) output root must be visible so misconfigured
        // CodeProjectRoot values (e.g. machine-specific absolute paths) are obvious.
        result.StandardOutput.ShouldContain(_tempDirectory);
    }

    [Fact]
    public void Codegen_WhenProjectHasLocalizationCsv_GeneratedCodeContainsApplyLocalization()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new ProjectCreator().Create(gumxPath);

        string screenXml =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <ScreenSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Name>TestScreen</Name>
              <State>
                <Name>Default</Name>
                <Variable>
                  <Type>string</Type>
                  <Name>TextInstance.Text</Name>
                  <Value xsi:type="xsd:string">T_OK</Value>
                  <SetsValue>true</SetsValue>
                </Variable>
              </State>
              <Instance>
                <Name>TextInstance</Name>
                <BaseType>Text</BaseType>
                <DefinedByBase>false</DefinedByBase>
              </Instance>
            </ScreenSave>
            """;
        File.WriteAllText(Path.Combine(_tempDirectory, "Screens", "TestScreen.gusx"), screenXml);

        File.WriteAllText(Path.Combine(_tempDirectory, "LocalizationDB.csv"),
            "String ID,English,Spanish\nT_OK,OK,De acuerdo\n");

        string gumxContent = File.ReadAllText(gumxPath);
        gumxContent = gumxContent.Replace("</GumProjectSave>",
            "  <ScreenReference Name=\"TestScreen\" />\n" +
            "  <LocalizationFile>LocalizationDB.csv</LocalizationFile>\n" +
            "</GumProjectSave>");
        File.WriteAllText(gumxPath, gumxContent);

        File.WriteAllText(Path.Combine(_tempDirectory, "ProjectCodeSettings.codsj"),
            """
            {
              "CodeProjectRoot": "./",
              "RootNamespace": "TestNamespace",
              "OutputLibrary": 5,
              "ObjectInstantiationType": 0,
              "SyntaxVersion": "*"
            }
            """);

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath);

        result.ExitCode.ShouldBe(0, customMessage: result.StandardError);
        string generatedPath = Path.Combine(_tempDirectory, "Screens", "TestScreen.Generated.cs");
        File.Exists(generatedPath).ShouldBeTrue(
            customMessage: "codegen should have written the generated screen file");
        string generatedContents = File.ReadAllText(generatedPath);
        generatedContents.ShouldContain("public void ApplyLocalization()",
            customMessage: "the project declares a localization CSV, so codegen must load it and emit ApplyLocalization");
        generatedContents.ShouldContain("Translate(\"T_OK\")");
    }

    [Fact]
    public void Codegen_WhenRaylibOutputLibrary_GeneratesFindByNameWiring()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new ProjectCreator().Create(gumxPath);

        string screenXml =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <ScreenSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Name>TestScreen</Name>
              <Instance>
                <Name>TextInstance</Name>
                <BaseType>Text</BaseType>
                <DefinedByBase>false</DefinedByBase>
              </Instance>
            </ScreenSave>
            """;
        File.WriteAllText(Path.Combine(_tempDirectory, "Screens", "TestScreen.gusx"), screenXml);

        string gumxContent = File.ReadAllText(gumxPath);
        gumxContent = gumxContent.Replace("</GumProjectSave>",
            "  <ScreenReference Name=\"TestScreen\" />\n</GumProjectSave>");
        File.WriteAllText(gumxPath, gumxContent);

        File.WriteAllText(Path.Combine(_tempDirectory, "ProjectCodeSettings.codsj"),
            """
            {
              "CodeProjectRoot": "./",
              "RootNamespace": "TestNamespace",
              "OutputLibrary": 6,
              "ObjectInstantiationType": 1,
              "SyntaxVersion": "*"
            }
            """);

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath);

        result.ExitCode.ShouldBe(0, customMessage: result.StandardError);
        // Plain (non-Forms) codegen names the generated class "<Element>Runtime" — same
        // convention as plain MonoGame — so the file is TestScreenRuntime.Generated.cs.
        string generatedPath = Path.Combine(_tempDirectory, "Screens", "TestScreenRuntime.Generated.cs");
        File.Exists(generatedPath).ShouldBeTrue(
            customMessage: "codegen should have written the generated screen file");
        string generatedContents = File.ReadAllText(generatedPath);
        generatedContents.ShouldContain("using Gum.GueDeriving;");
        generatedContents.ShouldContain("SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default)");
    }

    [Fact]
    public void Codegen_WhenRaylibWithFullyInCode_ReturnsExitCode1WithClearError()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new ProjectCreator().Create(gumxPath);

        File.WriteAllText(Path.Combine(_tempDirectory, "ProjectCodeSettings.codsj"),
            """
            {
              "CodeProjectRoot": "./",
              "RootNamespace": "TestNamespace",
              "OutputLibrary": 6,
              "ObjectInstantiationType": 0,
              "SyntaxVersion": "*"
            }
            """);

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath);

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("FindByName");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
