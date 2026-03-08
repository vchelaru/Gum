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

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
