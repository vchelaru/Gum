using System;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// <c>gumcli new --theme</c>: a project is created from a bundled theme (Meadow, Bubblegum, ...) instead of
/// the plain Forms template.
/// </summary>
public class FormsThemeCreationTests : IDisposable
{
    private readonly string _tempDirectory;

    public FormsThemeCreationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumThemeCreation_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static string FindThemesRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            current = Path.GetDirectoryName(current) ?? throw new InvalidOperationException("Themes folder not found");
        }
        throw new InvalidOperationException("Themes folder not found");
    }

    [Fact]
    public void FindThemeDirectory_ThemeNameInAnyCase_ReturnsThemeFolder()
    {
        FormsThemeLocator locator = new FormsThemeLocator(new[] { FindThemesRoot() });

        string? directory = locator.FindThemeDirectory("meadow");

        directory.ShouldNotBeNull();
        Path.GetFileName(directory).ShouldBe("Meadow");
    }

    [Fact]
    public void FindThemeDirectory_UnknownTheme_ReturnsNull()
    {
        FormsThemeLocator locator = new FormsThemeLocator(new[] { FindThemesRoot() });

        locator.FindThemeDirectory("NoSuchTheme").ShouldBeNull();
    }

    [Fact]
    public void GetAvailableThemes_BundledThemes_ListsEveryThemeFolder()
    {
        FormsThemeLocator locator = new FormsThemeLocator(new[] { FindThemesRoot() });

        locator.GetAvailableThemes().ShouldBe(
            new[] { "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95" });
    }

    [Fact]
    public void CreateFromTheme_Meadow_ProducesLoadableProjectWithThemeContentAndBehaviors()
    {
        string themeDirectory = new FormsThemeLocator(new[] { FindThemesRoot() }).FindThemeDirectory("Meadow")!;
        string projectPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        StandardElementsManager.Self.Initialize();

        new FormsTemplateCreator().CreateFromTheme(projectPath, themeDirectory);

        ProjectLoadResult result = new ProjectLoader().Load(projectPath);
        result.Success.ShouldBeTrue();
        result.LoadErrors.ShouldBeEmpty();
        GumProjectSave project = result.Project!;
        project.Components.ShouldContain(item => item.Name == "Meadow/Controls/Label");
        project.Behaviors.ShouldNotBeEmpty();
        project.Behaviors.ShouldAllBe(item => !item.IsSourceFileMissing);
        project.BehaviorReferences.ShouldAllBe(item => string.IsNullOrEmpty(item.SourcePath));
        // Not project content: the theme's own code settings and gallery preview.
        File.Exists(Path.Combine(_tempDirectory, "ProjectCodeSettings.codsj")).ShouldBeFalse();
        File.Exists(Path.Combine(_tempDirectory, "preview.png")).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Bubblegum")]
    [InlineData("DarkPro")]
    [InlineData("Editor")]
    [InlineData("ForestGlade")]
    [InlineData("Hazard")]
    [InlineData("Meadow")]
    [InlineData("Neon")]
    [InlineData("Retro95")]
    public void CreateFromTheme_EveryTheme_WritesOnlyItsDemoScreen(string themeName)
    {
        string themeDirectory = new FormsThemeLocator(new[] { FindThemesRoot() }).FindThemeDirectory(themeName)!;
        string projectPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        StandardElementsManager.Self.Initialize();

        new FormsTemplateCreator().CreateFromTheme(projectPath, themeDirectory);

        string screensDirectory = Path.Combine(_tempDirectory, "Screens");
        Directory.GetFiles(screensDirectory, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(screensDirectory, file).Replace((char)92, '/'))
            .ShouldBe(new[] { themeName + "/DemoScreenGum.gusx" });
        new ProjectLoader().Load(projectPath).Project!.Screens.Select(screen => screen.Name)
            .ShouldBe(new[] { themeName + "/DemoScreenGum" });
    }

    // Retro95's submenu popup component and its behavior come along into a new project (#6720).
    [Fact]
    public void CreateFromTheme_Retro95_IncludesSubmenuPopupBehaviorAndComponent()
    {
        string themeDirectory = new FormsThemeLocator(new[] { FindThemesRoot() }).FindThemeDirectory("Retro95")!;
        string projectPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        StandardElementsManager.Self.Initialize();

        new FormsTemplateCreator().CreateFromTheme(projectPath, themeDirectory);

        ProjectLoadResult result = new ProjectLoader().Load(projectPath);
        result.Success.ShouldBeTrue();
        result.LoadErrors.ShouldBeEmpty();
        GumProjectSave project = result.Project!;
        BehaviorSave behavior = project.Behaviors.Single(item => item.Name == StandardFormsBehaviorNames.MenuItemSubmenuPopupBehaviorName);
        behavior.IsSourceFileMissing.ShouldBeFalse();
        behavior.DefaultImplementation.ShouldBe("Retro95/Controls/MenuItemSubmenuPopup");
        ComponentSave component = project.Components.Single(item => item.Name == "Retro95/Controls/MenuItemSubmenuPopup");
        component.Behaviors.ShouldContain(item => item.BehaviorName == StandardFormsBehaviorNames.MenuItemSubmenuPopupBehaviorName);
    }

    [Fact]
    public void CreateFromTheme_JsonPath_ProducesJsonProject()
    {
        string themeDirectory = new FormsThemeLocator(new[] { FindThemesRoot() }).FindThemeDirectory("Meadow")!;
        string projectPath = Path.Combine(_tempDirectory, "MyProject.gumj");
        StandardElementsManager.Self.Initialize();

        new FormsTemplateCreator().CreateFromTheme(projectPath, themeDirectory);

        File.Exists(projectPath).ShouldBeTrue();
        Directory.GetFiles(_tempDirectory, "*.gucx", SearchOption.AllDirectories).ShouldBeEmpty();
        ProjectLoadResult result = new ProjectLoader().Load(projectPath);
        result.Success.ShouldBeTrue();
        result.Project!.Components.ShouldContain(item => item.Name == "Meadow/Controls/Label");
    }
}
