using Shouldly;

namespace Gum.Cli.Tests;

public class CheckReferencesCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public CheckReferencesCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliCheckRefsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void CheckReferences_CleanProject_ExitCode0()
    {
        string filePath = CreateTestProject("Clean");

        CliTestHelper result = CliTestHelper.Run("check-references", filePath);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("No unpropagated references");
    }

    [Fact]
    public void CheckReferences_ProjectWithUnpropagatedComponent_ExitCode1()
    {
        string filePath = CreateProjectWithUnpropagatedComponent("Unpropagated");

        CliTestHelper result = CliTestHelper.Run("check-references", filePath);

        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldContain("BadComponent");
        result.StandardOutput.ShouldContain("missing materialized scalars");
    }

    [Fact]
    public void CheckReferences_WithFix_RewritesElementAndReturnsExitCode0()
    {
        string filePath = CreateProjectWithUnpropagatedComponent("FixOnSave");

        CliTestHelper result = CliTestHelper.Run("check-references", filePath, "--fix");

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("fixed: BadComponent");

        // The fixed .gucx on disk must now contain a materialized <Variable Name="X" ... />.
        string componentPath = Path.Combine(_tempDirectory, "FixOnSave", "Components", "BadComponent.gucx");
        string contents = File.ReadAllText(componentPath);
        contents.ShouldContain("Name=\"X\"",
            customMessage: "the propagated scalar should now be persisted in the component file");
    }

    [Fact]
    public void CheckReferences_NonexistentProject_ExitCode2()
    {
        string fakePath = Path.Combine(_tempDirectory, "nonexistent.gumx");

        CliTestHelper result = CliTestHelper.Run("check-references", fakePath);

        result.ExitCode.ShouldBe(2);
    }

    private string CreateTestProject(string name)
    {
        string filePath = Path.Combine(_tempDirectory, name, name + ".gumx");
        CliTestHelper.Run("new", filePath);
        return filePath;
    }

    /// <summary>
    /// Creates a project containing BadComponent.gucx that has a VariableReferences
    /// row but no materialized scalar — the AI-authored repro shape.
    /// </summary>
    private string CreateProjectWithUnpropagatedComponent(string name)
    {
        string filePath = CreateTestProject(name);

        string componentDir = Path.Combine(Path.GetDirectoryName(filePath)!, "Components");
        Directory.CreateDirectory(componentDir);

        // SourceX has a value; X has only a reference assignment but no materialized scalar.
        // Element-form XML matches the on-disk shape produced by gumcli new.
        string componentXml =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <ComponentSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
              <Name>BadComponent</Name>
              <BaseType>Container</BaseType>
              <State>
                <Name>Default</Name>
                <Variable Type="float" Name="SourceX" SetsValue="true">
                  <Value xsi:type="xsd:float">100</Value>
                </Variable>
                <VariableList xsi:type="VariableListSaveOfString">
                  <Type>string</Type>
                  <Name>VariableReferences</Name>
                  <IsFile>false</IsFile>
                  <IsHiddenInPropertyGrid>false</IsHiddenInPropertyGrid>
                  <Value>
                    <string>X = SourceX</string>
                  </Value>
                </VariableList>
              </State>
            </ComponentSave>
            """;

        string componentPath = Path.Combine(componentDir, "BadComponent.gucx");
        File.WriteAllText(componentPath, componentXml);

        string gumxContent = File.ReadAllText(filePath);
        const string componentRef = """  <ComponentReference Name="BadComponent" />""";
        gumxContent = gumxContent.Replace("</GumProjectSave>", componentRef + "\n</GumProjectSave>");
        File.WriteAllText(filePath, gumxContent);

        return filePath;
    }
}
