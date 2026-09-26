using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A project made from a bundled template must survive the tool's load and save unchanged (#5156).
/// Loading back-fills standard-element defaults and saving sorts and reformats variables, so a
/// template that lags either one rewrites files the user never touched. When this fails after a
/// new standard variable is added, re-save the templates with <c>gumcli resave</c> (no <c>--raw</c>).
/// </summary>
public class TemplateResaveStabilityTests : IDisposable
{
    private readonly string _tempDirectory;

    public TemplateResaveStabilityTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumTemplateResaveTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    public static IEnumerable<object[]> ThemeNames => new[]
    {
        "Bubblegum", "DarkPro", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95"
    }.Select(name => new object[] { name });

    [Fact]
    public void Resave_ShouldNotChangeAnyFile_ForNewFormsProject()
    {
        string projectPath = Path.Combine(_tempDirectory, "GumProject.gumx");
        new FormsTemplateCreator().Create(projectPath);

        List<string> changedFiles = ResaveAndGetChangedFiles(projectPath, _tempDirectory);

        changedFiles.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Resave_ShouldNotChangeAnyFile_ForTheme(string themeName)
    {
        // Keep the theme's relative layout so its "../../FormsBehaviors" source paths still resolve.
        string templatesDir = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates");
        string themeDir = Path.Combine(_tempDirectory, "FormsThemes", themeName);
        CopyDirectory(Path.Combine(templatesDir, "FormsThemes", themeName), themeDir);
        CopyDirectory(Path.Combine(templatesDir, "FormsBehaviors"), Path.Combine(_tempDirectory, "FormsBehaviors"));

        List<string> changedFiles = ResaveAndGetChangedFiles(Path.Combine(themeDir, "GumProject.gumx"), themeDir);

        changedFiles.ShouldBeEmpty();
    }

    // Mirrors "gumcli resave" without --raw: load the way the tool does, then save everything.
    private static List<string> ResaveAndGetChangedFiles(string projectPath, string projectDir)
    {
        StandardElementsManager.Self.Initialize();

        Dictionary<string, byte[]> before = Directory.GetFiles(projectDir, "*", SearchOption.AllDirectories)
            .ToDictionary(path => path, File.ReadAllBytes);

        ProjectLoadResult result = new ProjectLoader().Load(projectPath);
        result.Success.ShouldBeTrue(result.ErrorMessage);
        result.Project!.Save(projectPath, saveElements: true);

        return Directory.GetFiles(projectDir, "*", SearchOption.AllDirectories)
            .Where(path => !before.TryGetValue(path, out byte[]? original) || !original.AsSpan().SequenceEqual(File.ReadAllBytes(path)))
            .Select(path => Path.GetRelativePath(projectDir, path))
            .ToList();
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates")))
            {
                return current;
            }
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current)
            {
                break;
            }
            current = parent;
        }
        throw new InvalidOperationException("could not locate repo root from " + AppContext.BaseDirectory);
    }
}
