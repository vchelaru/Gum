using System.Diagnostics;
using Shouldly;

namespace Gum.AssemblyNameGuard.Tests;

/// <summary>
/// Proves the shared MSBuild guard (<c>AssemblyNameCollisionGuard.targets</c>, packed as
/// buildTransitive into every Gum runtime NuGet package) actually fails a real `dotnet build`
/// when the consuming project's AssemblyName collides with a Gum runtime assembly, and stays
/// out of the way otherwise. See issue #4311.
/// </summary>
public class AssemblyNameCollisionGuardTests
{
    [Fact]
    public void Build_WithCollidingAssemblyName_FailsWithGuardError()
    {
        BuildResult result = BuildTempProjectImportingGuard(
            assemblyName: "SilkNetGum",
            guardAssemblyName: "SilkNetGum",
            guardPackageId: "Gum.SilkNet");

        result.ExitCode.ShouldNotBe(0);
        result.Output.ShouldContain(
            "Your project's assembly name 'SilkNetGum' collides with the assembly shipped by Gum.SilkNet");
    }

    [Fact]
    public void Build_WithNonCollidingAssemblyName_SucceedsWithoutGuardError()
    {
        BuildResult result = BuildTempProjectImportingGuard(
            assemblyName: "MyGameSilkNetGum",
            guardAssemblyName: "SilkNetGum",
            guardPackageId: "Gum.SilkNet");

        result.ExitCode.ShouldBe(0);
        result.Output.ShouldNotContain("collides with the assembly shipped by");
    }

    private static BuildResult BuildTempProjectImportingGuard(
        string assemblyName, string guardAssemblyName, string guardPackageId)
    {
        string guardTargetsPath = Path.Combine(RepoPaths.RepoRoot, "AssemblyNameCollisionGuard.targets")
            .Replace('\\', '/');

        string tempDir = Path.Combine(Path.GetTempPath(), "GumAssemblyNameGuardTests_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        try
        {
            string csprojContents = $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <OutputType>Library</OutputType>
                    <AssemblyName>{assemblyName}</AssemblyName>
                    <GumRuntimeAssemblyName>{guardAssemblyName}</GumRuntimeAssemblyName>
                    <GumRuntimePackageId>{guardPackageId}</GumRuntimePackageId>
                  </PropertyGroup>
                  <Import Project="{guardTargetsPath}" />
                </Project>
                """;
            string csprojPath = Path.Combine(tempDir, "TestConsumer.csproj");
            File.WriteAllText(csprojPath, csprojContents);

            ProcessStartInfo startInfo = new("dotnet", $"build \"{csprojPath}\" --nologo")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using Process process = Process.Start(startInfo)!;
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return new BuildResult(process.ExitCode, stdout + stderr);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private readonly record struct BuildResult(int ExitCode, string Output);
}
