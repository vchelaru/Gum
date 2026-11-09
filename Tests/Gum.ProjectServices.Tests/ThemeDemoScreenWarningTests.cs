using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.Expressions;
using Gum.Managers;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// #7770: a freshly generated project must have no warnings, so every bundled theme's screens
/// (the demo screen) check clean.
/// </summary>
public class ThemeDemoScreenWarningTests
{
    private static readonly string[] ThemeNames =
    {
        "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95"
    };

    public static IEnumerable<object[]> Themes() => ThemeNames.Select(t => new object[] { t });

    [Theory]
    [MemberData(nameof(Themes))]
    public void ThemeScreens_ShouldProduceNoErrorsOrWarnings(string theme)
    {
        StandardElementsManager.Self.Initialize();
        GumExpressionService.Initialize();
        string projectPath = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", theme, "GumProject.gumx");
        ProjectLoadResult result = new ProjectLoader().Load(projectPath);
        result.Success.ShouldBeTrue();

        HeadlessErrorChecker checker = new HeadlessErrorChecker(Mock.Of<ITypeResolver>());
        List<string> problems = new();
        foreach (ScreenSave screen in result.Project!.Screens)
        {
            foreach (ErrorResult error in checker.GetErrorsFor(screen, result.Project))
            {
                problems.Add($"{screen.Name} {error.Code}: {error.Message}");
            }
        }

        problems.ShouldBeEmpty();
    }

    private static string FindRepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "Gum.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        return dir ?? throw new InvalidOperationException("repo root not found");
    }
}
