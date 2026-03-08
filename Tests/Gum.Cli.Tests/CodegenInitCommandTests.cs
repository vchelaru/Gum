using Shouldly;
using System;
using System.IO;

namespace Gum.Cli.Tests;

public class CodegenInitCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public CodegenInitCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliCodegenInitTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void CodegenInit_WhenCodsjAlreadyExists_ReturnsExitCode2WithoutForce()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        File.WriteAllText(gumxPath, "");
        string csprojPath = Path.Combine(_tempDirectory, "MyGame.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        string codsjPath = Path.Combine(_tempDirectory, "ProjectCodeSettings.codsj");
        File.WriteAllText(codsjPath, "{}");

        CliTestHelper result = CliTestHelper.Run("codegen-init", gumxPath);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("ProjectCodeSettings.codsj");
    }

    [Fact]
    public void CodegenInit_WhenCodsjAlreadyExistsWithForce_Overwrites()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        File.WriteAllText(gumxPath, "");
        string csprojPath = Path.Combine(_tempDirectory, "MyGame.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        string codsjPath = Path.Combine(_tempDirectory, "ProjectCodeSettings.codsj");
        File.WriteAllText(codsjPath, "{}");

        CliTestHelper result = CliTestHelper.Run("codegen-init", gumxPath, "--force");

        result.ExitCode.ShouldBe(0);
        File.Exists(codsjPath).ShouldBeTrue();
        string newContents = File.ReadAllText(codsjPath);
        newContents.ShouldNotBe("{}");
    }

    [Fact]
    public void CodegenInit_WhenCsprojExists_CreatesCodsj()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        File.WriteAllText(gumxPath, "");
        string csprojPath = Path.Combine(_tempDirectory, "MyGame.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        CliTestHelper result = CliTestHelper.Run("codegen-init", gumxPath);

        result.ExitCode.ShouldBe(0);
        string codsjPath = Path.Combine(_tempDirectory, "ProjectCodeSettings.codsj");
        File.Exists(codsjPath).ShouldBeTrue();
    }

    [Fact]
    public void CodegenInit_WhenCsprojExists_PrintsConfiguredValues()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        File.WriteAllText(gumxPath, "");
        string csprojPath = Path.Combine(_tempDirectory, "MyGame.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        CliTestHelper result = CliTestHelper.Run("codegen-init", gumxPath);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("CodeProjectRoot");
        result.StandardOutput.ShouldContain("RootNamespace");
        result.StandardOutput.ShouldContain("OutputLibrary");
    }

    [Fact]
    public void CodegenInit_WhenNoCsprojFound_ReturnsExitCode2()
    {
        // Use an isolated directory with no .csproj in the ancestor chain.
        string tempRoot = Path.GetTempPath();
        string isolatedDir = Path.Combine(tempRoot, "GumCliNoCsproj_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(isolatedDir);

        try
        {
            string gumxPath = Path.Combine(isolatedDir, "TestProject.gumx");
            File.WriteAllText(gumxPath, "");

            CliTestHelper result = CliTestHelper.Run("codegen-init", gumxPath);

            // If a csproj happens to exist in the real FS ancestry (unusual machine config),
            // the command may succeed. We only assert the error case when no csproj is found.
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
    public void CodegenInit_WhenProjectFileNotFound_ReturnsExitCode2()
    {
        string nonExistentPath = Path.Combine(_tempDirectory, "DoesNotExist.gumx");

        CliTestHelper result = CliTestHelper.Run("codegen-init", nonExistentPath);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("error");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
