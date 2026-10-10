using Gum.ProjectServices;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gum.Cli.Tests;

/// <summary>
/// Every bundled Forms theme, created through <c>gumcli new --theme</c>, must generate code that does not
/// assign TextRuntime-only members through <c>Visual</c>, which is typed InteractiveGue (#5933).
/// </summary>
public class ThemeCodegenTests : IDisposable
{
    private static readonly Regex TextOnlyVisualAssignment = new Regex(
        @"Visual\.(Font|FontSize|IsBold|IsItalic|Red|Green|Blue)\s*=");

    private readonly string _tempDirectory;

    public ThemeCodegenTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliThemeCodegen_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public static IEnumerable<object[]> ThemeNames()
    {
        IFormsThemeLocator locator = FormsThemeLocator.CreateDefault(AppContext.BaseDirectory, Directory.GetCurrentDirectory());
        foreach (string theme in locator.GetAvailableThemes())
        {
            yield return new object[] { theme };
        }
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Codegen_ThemeProject_DoesNotAssignTextOnlyMembersThroughVisual(string theme)
    {
        string projectDirectory = Path.Combine(_tempDirectory, "ThemeGame");
        CliTestHelper.Run("new", projectDirectory, "--theme", theme, "--platform", "monogame", "--no-restore")
            .ExitCode.ShouldBe(0);
        string gumFolder = Path.Combine(projectDirectory, "Content", "GumProject");
        string gumxPath = Path.Combine(gumFolder, "GumProject.gumj");

        // The scaffolded project finds controls by name; only FullyInCode emits the per-variable assignments.
        CodeOutputProjectSettingsManager settingsManager = new CodeOutputProjectSettingsManager(
            new SilentLogger(), new FixedProjectDirectoryProvider(gumFolder + Path.DirectorySeparatorChar));
        CodeOutputProjectSettings settings = settingsManager.CreateOrLoadSettingsForProject();
        settings.ObjectInstantiationType = ObjectInstantiationType.FullyInCode;
        settingsManager.WriteSettingsForProject(settings);

        CliTestHelper.Run("codegen", gumxPath).ExitCode.ShouldBe(0);

        List<string> offending = new List<string>();
        string[] generatedFiles = Directory.GetFiles(projectDirectory, "*.Generated.cs", SearchOption.AllDirectories);
        generatedFiles.ShouldNotBeEmpty();
        foreach (string path in generatedFiles)
        {
            foreach (Match match in TextOnlyVisualAssignment.Matches(File.ReadAllText(path)))
            {
                offending.Add($"{Path.GetFileName(path)}: {match.Value}");
            }
        }

        offending.ShouldBeEmpty();
    }

    private class SilentLogger : ICodeGenLogger
    {
        public void PrintOutput(string message) { }

        public void PrintError(string message) { }
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
