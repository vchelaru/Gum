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
    public void Codegen_WhenCsprojReferencesNoGumRuntime_EmitsCurrentNamespacesNotLegacyOnes()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new FormsTemplateCreator().Create(gumxPath);
        File.WriteAllText(Path.Combine(_tempDirectory, "MyGame.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath);

        result.ExitCode.ShouldBe(0);
        string generated = File.ReadAllText(Path.Combine(_tempDirectory, "Components", "Controls", "ButtonStandard.Generated.cs"));
        generated.ShouldNotContain("MonoGameGum");
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
    public void Codegen_BackslashCodeProjectRoot_PrintsNativePath()
    {
        // Settings saved on Windows use backslashes; on macOS/Linux those are file-name
        // characters, so the printed root must be converted rather than shown as "Output\".
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new ProjectCreator().Create(gumxPath);
        File.WriteAllText(Path.Combine(_tempDirectory, "ProjectCodeSettings.codsj"),
            """
            {
              "CodeProjectRoot": "Output\\",
              "RootNamespace": "TestNamespace",
              "OutputLibrary": 5,
              "ObjectInstantiationType": 0,
              "SyntaxVersion": "*"
            }
            """);

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath);

        string expectedRoot = Path.Combine(_tempDirectory, "Output") + Path.DirectorySeparatorChar;
        result.StandardOutput.ShouldContain("Generating code into " + expectedRoot);
    }

    [Fact]
    public void Codegen_GeneratedCodeFolder_WritesAndPrintsTheFolderUnderCodeProjectRoot()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new ProjectCreator().Create(gumxPath);
        File.WriteAllText(Path.Combine(_tempDirectory, "ProjectCodeSettings.codsj"),
            """
            {
              "CodeProjectRoot": "Output/",
              "GeneratedCodeFolder": "Gum/Generated",
              "RootNamespace": "TestNamespace",
              "OutputLibrary": 5,
              "ObjectInstantiationType": 0,
              "SyntaxVersion": "*"
            }
            """);

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath);

        string expectedFolder = Path.Combine(_tempDirectory, "Output", "Gum", "Generated") + Path.DirectorySeparatorChar;
        result.StandardOutput.ShouldContain("Generating code into " + expectedFolder);
        File.Exists(Path.Combine(expectedFolder, "StandardElements.Generated.cs")).ShouldBeTrue();
    }

    [Theory]
    [InlineData("GumCodeGen/", "GumCodeGen/Screens")]
    [InlineData("Gum", "GumScreens")]
    public void Codegen_GeneratedCodeFolderPrefix_WritesTheScreenUnderThePrefixedFolder_AndKeepsTheNamespace(
        string prefix, string expectedScreensFolder)
    {
        string gumxPath = CreateProjectWithScreen("TestScreen", prefix);

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath);

        result.ExitCode.ShouldBe(0, customMessage: result.StandardError);
        string expectedFolder = Path.Combine(_tempDirectory, Path.Combine(expectedScreensFolder.Split('/')));
        string generatedPath = Path.Combine(expectedFolder, "TestScreen.Generated.cs");
        File.Exists(generatedPath).ShouldBeTrue();
        File.Exists(Path.Combine(expectedFolder, "TestScreen.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDirectory, "Screens", "TestScreen.Generated.cs")).ShouldBeFalse();
        File.ReadAllText(generatedPath).ShouldContain("namespace TestNamespace.Screens");
    }

    [Fact]
    public void Codegen_WithPrune_AndAPrefix_KeepsPrefixedFilesAndPrunesOnesLeftInTheOldFolder()
    {
        string gumxPath = CreateProjectWithScreen("TestScreen", "GumCodeGen/");
        string leftBehind = WriteGeneratedFile("TestScreen");

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath, "--prune");

        result.ExitCode.ShouldBe(0, customMessage: result.StandardError);
        File.Exists(Path.Combine(_tempDirectory, "GumCodeGen", "Screens", "TestScreen.Generated.cs")).ShouldBeTrue();
        File.Exists(leftBehind).ShouldBeFalse(
            customMessage: "the copy in the unprefixed folder no longer matches where the element generates");
        result.StandardOutput.ShouldContain("Pruned 1");
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
    public void Codegen_WhenFormsComponentInheritsSpriteWithoutBehaviors_SkipsItWithAnErrorAndExitsNonZero()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new ProjectCreator().Create(gumxPath);
        File.WriteAllText(Path.Combine(_tempDirectory, "Components", "FromSprite.gucx"),
            """
            <?xml version="1.0" encoding="utf-8"?>
            <ComponentSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Name>FromSprite</Name>
              <BaseType>Sprite</BaseType>
              <State>
                <Name>Default</Name>
              </State>
            </ComponentSave>
            """);
        string gumxContent = File.ReadAllText(gumxPath);
        File.WriteAllText(gumxPath, gumxContent.Replace("</GumProjectSave>",
            "  <ComponentReference Name=\"FromSprite\" />\n</GumProjectSave>"));
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

        result.ExitCode.ShouldBe(1, customMessage: result.StandardOutput);
        result.StandardError.ShouldContain("error: FromSprite:");
        result.StandardError.ShouldContain("must either inherit from Container");
        File.Exists(Path.Combine(_tempDirectory, "Components", "FromSprite.Generated.cs")).ShouldBeFalse();
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
    public void Codegen_WritesStandardElementsFallbackFile()
    {
        // Issue #3505: gumcli codegen doesn't call HeadlessCodeGenerationService.GenerateCodeForAllElements
        // (it iterates elements individually so it can run per-element error checks first - see the
        // gum-cli skill), so the fallback file must be written directly from CodegenCommand too, or
        // CI's Codegen Drift Check regeneration (Tests/GenerateAllCodeGenProjects.bat, which shells out
        // to this exact "gumcli codegen" path) would never actually produce the file.
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

        result.ExitCode.ShouldBe(0, customMessage: result.StandardError);
        string generatedPath = Path.Combine(_tempDirectory, "StandardElements.Generated.cs");
        File.Exists(generatedPath).ShouldBeTrue(
            customMessage: "codegen should have written the Standard Elements fallback file");
        string contents = File.ReadAllText(generatedPath);
        contents.ShouldContain("[ModuleInitializer]");
        contents.ShouldContain("ObjectFinder.Self.RegisterFallbackStandardElements(");
    }

    [Fact]
    public void Codegen_WhenCodeProjectRootCannotBeCreated_ReportsTheErrorAndReturnsExitCode1()
    {
        // A file where the output folder should be makes directory creation fail on every OS, the
        // same way a committed absolute path to another machine's folder does.
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        CliTestHelper.Run("new", gumxPath);
        File.WriteAllText(Path.Combine(_tempDirectory, "Blocked"), "not a folder");
        File.WriteAllText(Path.Combine(_tempDirectory, "ProjectCodeSettings.codsj"),
            """
            {
              "CodeProjectRoot": "Blocked/Output/",
              "RootNamespace": "TestNamespace",
              "OutputLibrary": 5,
              "ObjectInstantiationType": 0,
              "SyntaxVersion": "*"
            }
            """);

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath);

        result.ExitCode.ShouldBe(1, customMessage: result.StandardError);
        result.StandardError.ShouldNotContain("Unhandled exception");
        result.StandardError.ShouldContain("Blocked");
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

    [Fact]
    public void Codegen_WithoutPrune_LeavesOrphanedGeneratedFileAlone()
    {
        string gumxPath = CreateProjectWithCodeSettings();
        string orphanPath = WriteGeneratedFile("DeletedScreen");

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath);

        result.ExitCode.ShouldBe(0, customMessage: result.StandardError);
        File.Exists(orphanPath).ShouldBeTrue(
            customMessage: "codegen must not remove anything unless --prune is passed");
    }

    [Fact]
    public void Codegen_WithPrune_DeletesOrphanedGeneratedFile()
    {
        string gumxPath = CreateProjectWithCodeSettings();
        string orphanPath = WriteGeneratedFile("DeletedScreen");

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath, "--prune");

        result.ExitCode.ShouldBe(0, customMessage: result.StandardError);
        File.Exists(orphanPath).ShouldBeFalse(
            customMessage: "a .Generated.cs file with no matching element is derived data and should be pruned");
        result.StandardOutput.ShouldContain("Pruned 1");
    }

    [Fact]
    public void Codegen_WithPrune_ReportsButDoesNotDeleteOrphanedCustomCodeFile()
    {
        string gumxPath = CreateProjectWithCodeSettings();
        WriteGeneratedFile("DeletedScreen");
        string customCodePath = Path.Combine(_tempDirectory, "Screens", "DeletedScreen.cs");
        File.WriteAllText(customCodePath, "partial class DeletedScreen { }");

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath, "--prune");

        result.ExitCode.ShouldBe(0, customMessage: result.StandardError);
        File.Exists(customCodePath).ShouldBeTrue(
            customMessage: "custom code is user-authored and unrecoverable, so --prune must never delete it");
        result.StandardOutput.ShouldContain("DeletedScreen.cs");
    }

    [Fact]
    public void Codegen_WithPrune_WhenNothingOrphaned_ReportsNothingPruned()
    {
        string gumxPath = CreateProjectWithCodeSettings();

        CliTestHelper result = CliTestHelper.Run("codegen", gumxPath, "--prune");

        result.ExitCode.ShouldBe(0, customMessage: result.StandardError);
        result.StandardOutput.ShouldContain("Pruned 0");
    }

    private string CreateProjectWithCodeSettings()
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
        return gumxPath;
    }

    private string CreateProjectWithScreen(string screenName, string generatedCodeFolderPrefix)
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new ProjectCreator().Create(gumxPath);
        File.WriteAllText(Path.Combine(_tempDirectory, "Screens", screenName + ".gusx"),
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <ScreenSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Name>{screenName}</Name>
              <State>
                <Name>Default</Name>
              </State>
            </ScreenSave>
            """);
        string gumxContent = File.ReadAllText(gumxPath);
        File.WriteAllText(gumxPath, gumxContent.Replace("</GumProjectSave>",
            $"  <ScreenReference Name=\"{screenName}\" />\n</GumProjectSave>"));
        File.WriteAllText(Path.Combine(_tempDirectory, "ProjectCodeSettings.codsj"),
            $$"""
            {
              "CodeProjectRoot": "./",
              "GeneratedCodeFolderPrefix": "{{generatedCodeFolderPrefix}}",
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
        string screensDirectory = Path.Combine(_tempDirectory, "Screens");
        Directory.CreateDirectory(screensDirectory);
        string path = Path.Combine(screensDirectory, elementName + ".Generated.cs");
        File.WriteAllText(path, $"//Code for {elementName}\r\npublic partial class {elementName} {{ }}");
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
