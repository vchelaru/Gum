using Shouldly;

namespace Gum.Cli.Tests;

public class CheckCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public CheckCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliCheckTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Check_CleanProject_ShouldReturnExitCode0()
    {
        string filePath = CreateTestProject("Clean");

        CliTestHelper result = CliTestHelper.Run("check", filePath);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("No errors found.");
    }

    [Fact]
    public void Check_CleanProject_WithJsonFlag_ShouldReturnEmptyArray()
    {
        string filePath = CreateTestProject("CleanJson");

        CliTestHelper result = CliTestHelper.Run("check", filePath, "--json");

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldBe("[]");
    }

    [Fact]
    public void Check_MissingProjectFile_ShouldReturnExitCode2()
    {
        string fakePath = Path.Combine(_tempDirectory, "nonexistent.gumx");

        CliTestHelper result = CliTestHelper.Run("check", fakePath);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("not found");
    }

    [Fact]
    public void Check_ProjectWithErrors_ShouldReturnExitCode1()
    {
        string filePath = CreateProjectWithBadInstance("Errors");

        CliTestHelper result = CliTestHelper.Run("check", filePath);

        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldContain("error:");
        result.StandardOutput.ShouldContain("error(s) found.");
    }

    [Fact]
    public void Check_ProjectWithErrors_JsonFlag_ShouldReturnJsonArray()
    {
        string filePath = CreateProjectWithBadInstance("ErrorsJson");

        CliTestHelper result = CliTestHelper.Run("check", filePath, "--json");

        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldStartWith("[");
        result.StandardOutput.ShouldContain("\"element\":");
        result.StandardOutput.ShouldContain("\"message\":");
        result.StandardOutput.ShouldContain("\"severity\":");
    }

    [Fact]
    public void Check_ProjectWithOnlyWarnings_ShouldReturnExitCode0AndNotReportAsError()
    {
        string filePath = CreateProjectWithMissingSpriteTexture("Warnings");

        CliTestHelper result = CliTestHelper.Run("check", filePath);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("warning:");
        result.StandardOutput.ShouldContain("No errors found.");
        result.StandardOutput.ShouldNotContain("error(s) found.");
    }

    [Fact]
    public void Check_ProjectWithErrorsAndWarnings_ShouldReportBothCountsSeparately()
    {
        string filePath = CreateProjectWithBadInstance("ErrorsAndWarnings");
        AddMissingSpriteTextureComponent(filePath);

        CliTestHelper result = CliTestHelper.Run("check", filePath);

        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldContain("1 error(s), 1 warning(s) found.");
    }

    /// <summary>
    /// Creates a clean project via the CLI and returns the .gumx path.
    /// </summary>
    private string CreateTestProject(string name)
    {
        string filePath = Path.Combine(_tempDirectory, name, name + ".gumx");
        CliTestHelper.Run("new", filePath);
        return filePath;
    }

    /// <summary>
    /// Creates a project that has a component with an instance referencing
    /// a non-existent base type, which triggers an error in HeadlessErrorChecker.
    /// </summary>
    private string CreateProjectWithBadInstance(string name)
    {
        string filePath = CreateTestProject(name);

        // Add a component with a bad instance by writing directly to disk
        string componentDir = Path.Combine(Path.GetDirectoryName(filePath)!, "Components");
        Directory.CreateDirectory(componentDir);

        string componentXml =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <ComponentSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
              <Name>BrokenComponent</Name>
              <BaseType>Container</BaseType>
              <Instance>
                <Name>BadChild</Name>
                <BaseType>NonExistentType</BaseType>
              </Instance>
            </ComponentSave>
            """;

        string componentPath = Path.Combine(componentDir, "BrokenComponent.gucx");
        File.WriteAllText(componentPath, componentXml);

        // Update the .gumx to reference the component.
        // The project is saved in v2 compact format, so references use XML attributes:
        // <ComponentReference Name="..." /> not child elements.
        string gumxContent = File.ReadAllText(filePath);
        const string componentRef = """  <ComponentReference Name="BrokenComponent" />""";

        gumxContent = gumxContent.Replace("</GumProjectSave>", componentRef + "\n</GumProjectSave>");
        File.WriteAllText(filePath, gumxContent);

        return filePath;
    }

    /// <summary>
    /// Creates a project with a component whose Sprite instance points its SourceFile at a
    /// texture that doesn't exist on disk. That's a Warning-only ErrorResult (GUM0006) - unlike
    /// a missing element/behavior file, it has no accompanying Error.
    /// </summary>
    private string CreateProjectWithMissingSpriteTexture(string name)
    {
        string filePath = CreateTestProject(name);
        AddMissingSpriteTextureComponent(filePath);
        return filePath;
    }

    /// <summary>
    /// Adds a component with a Sprite instance whose SourceFile doesn't exist on disk to the
    /// given .gumx, triggering a Warning-severity "missing referenced file" result (GUM0006).
    /// </summary>
    private static void AddMissingSpriteTextureComponent(string gumxFilePath)
    {
        string componentDir = Path.Combine(Path.GetDirectoryName(gumxFilePath)!, "Components");
        Directory.CreateDirectory(componentDir);

        string componentXml =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <ComponentSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
              <Name>SpriteWithMissingTexture</Name>
              <BaseType>Container</BaseType>
              <State>
                <Name>Default</Name>
                <Variable>
                  <IsFile>true</IsFile>
                  <Type>string</Type>
                  <Name>MySprite.SourceFile</Name>
                  <Value xsi:type="xsd:string">Textures/DoesNotExist.png</Value>
                  <SetsValue>true</SetsValue>
                </Variable>
              </State>
              <Instance>
                <Name>MySprite</Name>
                <BaseType>Sprite</BaseType>
              </Instance>
            </ComponentSave>
            """;

        string componentPath = Path.Combine(componentDir, "SpriteWithMissingTexture.gucx");
        File.WriteAllText(componentPath, componentXml);

        string gumxContent = File.ReadAllText(gumxFilePath);
        const string componentRef = """  <ComponentReference Name="SpriteWithMissingTexture" />""";

        gumxContent = gumxContent.Replace("</GumProjectSave>", componentRef + "\n</GumProjectSave>");
        File.WriteAllText(gumxFilePath, gumxContent);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
