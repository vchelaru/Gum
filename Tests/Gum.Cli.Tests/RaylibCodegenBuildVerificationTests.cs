using Gum.ProjectServices;
using Shouldly;
using System;
using System.Diagnostics;
using System.IO;

namespace Gum.Cli.Tests;

/// <summary>
/// End-to-end proof that Raylib codegen (issue #3430) produces code that actually compiles
/// against RaylibGum. Generates code via the same headless pipeline the CLI uses, then shells
/// out to a real <c>dotnet build</c> of a fixture project that references RaylibGum.csproj via
/// ProjectReference — an in-memory Roslyn compile would not catch bugs where the consuming
/// project's own csproj settings (e.g. &lt;Nullable&gt;) interact with the shared linked source
/// files (see the gum-cross-platform-unification skill, issue #3218 precedent), so this test
/// deliberately goes through a real csproj + dotnet build instead.
/// </summary>
public class RaylibCodegenBuildVerificationTests : IDisposable
{
    private readonly string _tempDirectory;

    public RaylibCodegenBuildVerificationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumRaylibCodegenBuildFixture_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    private static string FindRepoRoot()
    {
        string? current = AppContext.BaseDirectory;

        while (current != null)
        {
            string candidate = Path.Combine(current, "Runtimes", "RaylibGum", "RaylibGum.csproj");
            if (File.Exists(candidate))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException(
            $"Could not locate the repo root (a directory containing Runtimes/RaylibGum/RaylibGum.csproj) by walking up from {AppContext.BaseDirectory}.");
    }

    [Fact]
    public void GeneratedRaylibCode_CompilesAgainstRaylibGumViaRealDotnetBuild()
    {
        string repoRoot = FindRepoRoot();
        string raylibGumCsprojPath = Path.Combine(repoRoot, "Runtimes", "RaylibGum", "RaylibGum.csproj");

        // Nullable is deliberately enabled: the consuming project's own <Nullable> setting is
        // what an in-memory compile can't exercise (issue #3218 class of bug).
        string fixtureCsprojContents =
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="{raylibGumCsprojPath}" />
              </ItemGroup>
            </Project>
            """;
        string fixtureCsprojPath = Path.Combine(_tempDirectory, "RaylibCodegenFixture.csproj");
        File.WriteAllText(fixtureCsprojPath, fixtureCsprojContents);

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
              "RootNamespace": "RaylibFixture",
              "OutputLibrary": 6,
              "ObjectInstantiationType": 1,
              "SyntaxVersion": "*"
            }
            """);

        CliTestHelper codegenResult = CliTestHelper.Run("codegen", gumxPath);
        codegenResult.ExitCode.ShouldBe(0, customMessage: $"codegen failed: {codegenResult.StandardError}");

        string generatedPath = Path.Combine(_tempDirectory, "Screens", "TestScreenRuntime.Generated.cs");
        File.Exists(generatedPath).ShouldBeTrue(customMessage: "codegen should have written the generated screen file");

        Process buildProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{fixtureCsprojPath}\" -v minimal --nologo",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        buildProcess.Start();
        string buildStdOut = buildProcess.StandardOutput.ReadToEnd();
        string buildStdErr = buildProcess.StandardError.ReadToEnd();
        bool exitedInTime = buildProcess.WaitForExit(180_000);

        exitedInTime.ShouldBeTrue(customMessage: "dotnet build of the Raylib codegen fixture did not complete within 180s");
        buildProcess.ExitCode.ShouldBe(0,
            customMessage: $"dotnet build of the Raylib codegen fixture failed.\nSTDOUT:\n{buildStdOut}\nSTDERR:\n{buildStdErr}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch (IOException)
            {
                // MSBuild's persistent build server can briefly hold handles on bin/obj right
                // after a build completes; leaking a stray %TEMP% dir here is harmless.
            }
        }
    }
}
