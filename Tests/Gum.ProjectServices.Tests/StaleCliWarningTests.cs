using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using System;
using System.IO;

namespace Gum.ProjectServices.Tests;

public class StaleCliWarningTests : IDisposable
{
    private readonly string _tempDirectory;

    public StaleCliWarningTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumStaleCliTests_" + Guid.NewGuid().ToString("N"));
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
    public void GetMessage_RuntimeNewerThanCli_NamesBothVersionsAndTheUpdateCommand()
    {
        string? message = StaleCliWarning.GetMessage("2026.9.2.1", "2026.10.1.1");

        message.ShouldNotBeNull();
        message.ShouldContain("v2026.9.2.1");
        message.ShouldContain("v2026.10.1.1");
        message.ShouldContain("dotnet tool update -g GumCli");
    }

    [Theory]
    [InlineData("2026.10.1.1", "2026.10.1.1")] // equal
    [InlineData("2026.10.1.1", "2026.9.2.1")]  // runtime older
    [InlineData("2026.10.1.1+abc123", "2026.10.1.1")] // build metadata on the CLI is ignored
    [InlineData("2026.10.1.1", "2026.10.1.1-preview.1")] // prerelease of the same version
    [InlineData("2026.10.1", "2026.10.1.0")] // missing trailing component is zero
    [InlineData("2026.10.1.1", null)] // no runtime detected
    [InlineData("2026.10.1.1", "*")] // unresolved floating version
    [InlineData("2026.10.1.1", "2026.*")]
    [InlineData("2026.10.1.1", "[2026.1,2027.0)")] // version range
    [InlineData("2026.10.1.1", "$(GumVersion)")] // msbuild property
    [InlineData("unknown", "2026.10.1.1")] // CLI version could not be determined
    public void GetMessage_NoStaleCliProven_ReturnsNull(string cliVersion, string? runtimeVersion)
    {
        StaleCliWarning.GetMessage(cliVersion, runtimeVersion).ShouldBeNull();
    }

    [Fact]
    public void FindRuntimePackageVersion_PackageReference_ReturnsDeclaredVersion()
    {
        WriteGameCsproj(@"<PackageReference Include=""Gum.MonoGame"" Version=""2026.10.1.1"" />");

        string? version = CreateSut(nuGetCacheRoot: Path.Combine(_tempDirectory, "empty-cache"))
            .FindRuntimePackageVersion(AutoDetectSettings, GameDirectory);

        version.ShouldBe("2026.10.1.1");
    }

    [Fact]
    public void FindRuntimePackageVersion_FloatingVersion_ReturnsHighestRestoredStableVersion()
    {
        string nuGetCacheRoot = Path.Combine(_tempDirectory, "nuget-cache");
        Directory.CreateDirectory(Path.Combine(nuGetCacheRoot, "gum.monogame", "2026.9.2.1"));
        Directory.CreateDirectory(Path.Combine(nuGetCacheRoot, "gum.monogame", "2026.10.1.1"));
        Directory.CreateDirectory(Path.Combine(nuGetCacheRoot, "gum.monogame", "2027.1.1.1-beta"));
        WriteGameCsproj(@"<PackageReference Include=""Gum.MonoGame"" Version=""*"" />");

        string? version = CreateSut(nuGetCacheRoot).FindRuntimePackageVersion(AutoDetectSettings, GameDirectory);

        version.ShouldBe("2026.10.1.1");
    }

    [Fact]
    public void FindRuntimePackageVersion_ProjectReferenceOnly_ReturnsNull()
    {
        WriteGameCsproj(@"<ProjectReference Include=""..\Gum\MonoGameGum\MonoGameGum.csproj"" />");

        string? version = CreateSut(nuGetCacheRoot: Path.Combine(_tempDirectory, "empty-cache"))
            .FindRuntimePackageVersion(AutoDetectSettings, GameDirectory);

        version.ShouldBeNull();
    }

    [Fact]
    public void FindRuntimePackageVersion_NoCsproj_ReturnsNull()
    {
        Directory.CreateDirectory(GameDirectory);

        string? version = CreateSut(nuGetCacheRoot: Path.Combine(_tempDirectory, "empty-cache"))
            .FindRuntimePackageVersion(AutoDetectSettings, GameDirectory);

        version.ShouldBeNull();
    }

    private string GameDirectory => Path.Combine(_tempDirectory, "game");

    private static CodeOutputProjectSettings AutoDetectSettings => new CodeOutputProjectSettings
    {
        SyntaxVersion = "*",
        CodeProjectRoot = "./"
    };

    private static SyntaxVersionDetectionService CreateSut(string nuGetCacheRoot) =>
        new SyntaxVersionDetectionService(new NullLogger(), nuGetCacheRoot);

    private class NullLogger : ICodeGenLogger
    {
        public void PrintOutput(string message) { }
        public void PrintError(string message) { }
    }

    private void WriteGameCsproj(string itemGroupBody)
    {
        Directory.CreateDirectory(GameDirectory);
        File.WriteAllText(Path.Combine(GameDirectory, "MyGame.csproj"),
$@"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    {itemGroupBody}
  </ItemGroup>
</Project>");
    }
}
