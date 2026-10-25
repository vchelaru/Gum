using System;
using System.IO;
using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// An element that fails the per-element error check is skipped by <c>gumcli codegen</c>, which
/// leaves its checked-in generated file frozen at whatever an older generator wrote (#6787: a
/// stale DemoScreenGum.Generated.cs kept the obsolete MonoGameGum.GueDeriving namespace for years,
/// and CI's drift check cannot see it because the file never changes). Harness projects whose
/// generated code is compiled with analyzers must therefore contain no such elements.
/// </summary>
public class CodeGenHarnessFixtureTests
{
    private static string FindRepoFile(string relativePath)
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Could not find {relativePath} above {AppContext.BaseDirectory}");
    }

    [Theory]
    [InlineData("Tests/CodeGen_MonoGameForms_FullCodegen/Content/CodeGenProject.gumx")]
    [InlineData("Tests/CodeGen_MonoGameForms_ByReference/Content/GumProject/CodeGenTestProject.gumx")]
    [InlineData("Tests/CodeGen_MonoGameForms_Localization_ByReference/Content/GumProject/LocalizationCodeGenTestProject.gumx")]
    [InlineData("Tests/CodeGen_Raylib_ByReference/Content/GumProject/CodeGenTestProject.gumx")]
    [InlineData("Tests/CodeGen_Skia_ByReference/Content/GumProject/CodeGenTestProject.gumx")]
    [InlineData("Tests/CodeGen_Maui_FullCodegen/Content/GumProject/CodeGenTestProject.gumx")]
    public void Check_CodeGenHarnessProject_HasNoElementCodegenWouldSkip(string gumxRelativePath)
    {
        string gumxPath = FindRepoFile(gumxRelativePath);

        CliTestHelper result = CliTestHelper.Run("check", gumxPath);

        (result.StandardOutput + result.StandardError).ShouldNotContain("error:");
        result.ExitCode.ShouldBe(0);
    }
}
