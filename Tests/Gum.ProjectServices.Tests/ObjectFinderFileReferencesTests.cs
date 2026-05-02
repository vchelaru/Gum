using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Characterization tests for <see cref="ObjectFinder.GetAllFilesInProject"/> and
/// <see cref="ObjectFinder.GetFilesReferencedBy"/>. Pin down current behavior so the
/// upcoming consolidation onto <c>GumProjectDependencyWalker</c> can be verified safe.
/// </summary>
public class ObjectFinderFileReferencesTests : BaseTestClass
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Project directory used in characterization tests. The existing implementation
    /// derives the project directory from <c>FullFileName</c> via <c>FileManager.GetDirectory</c>,
    /// which appends a trailing slash. Using a forward-slash absolute-style path keeps the
    /// test platform-independent and matches the format strings the existing impl emits.
    /// </summary>
    private const string FakeProjectDirectory = "C:/FakeGumProject/";
    private const string FakeProjectFile = FakeProjectDirectory + "MyProject.gumx";

    private void SetProjectFullFileName()
    {
        Project.FullFileName = FakeProjectFile;
        ObjectFinder.Self.GumProjectSave = Project;
    }

    private static ScreenSave BuildScreen(string name)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        return screen;
    }

    private static ComponentSave BuildComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        return component;
    }

    private static InstanceSave AddSpriteInstance(ElementSave element, string instanceName, string sourceFileRelativePath)
    {
        InstanceSave instance = new InstanceSave { Name = instanceName, BaseType = "Sprite", ParentContainer = element };
        element.Instances.Add(instance);

        StateSave state = element.DefaultState;
        state.Variables.Add(new VariableSave
        {
            Name = instanceName + ".SourceFile",
            Type = "string",
            Value = sourceFileRelativePath,
            IsFile = true,
            SetsValue = true,
        });
        return instance;
    }

    private static InstanceSave AddTextInstanceWithDefaultFont(ElementSave element, string instanceName, string fontName, int fontSize)
    {
        InstanceSave instance = new InstanceSave { Name = instanceName, BaseType = "Text", ParentContainer = element };
        element.Instances.Add(instance);

        StateSave state = element.DefaultState;
        state.Variables.Add(new VariableSave { Name = instanceName + ".UseCustomFont", Type = "bool", Value = false, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = instanceName + ".Font", Type = "string", Value = fontName, IsFont = true, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = instanceName + ".FontSize", Type = "int", Value = fontSize, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = instanceName + ".OutlineThickness", Type = "int", Value = 0, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = instanceName + ".UseFontSmoothing", Type = "bool", Value = true, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = instanceName + ".IsItalic", Type = "bool", Value = false, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = instanceName + ".IsBold", Type = "bool", Value = false, SetsValue = true });
        return instance;
    }

    private static InstanceSave AddTextInstanceWithCustomFont(ElementSave element, string instanceName, string customFontFilePath)
    {
        InstanceSave instance = new InstanceSave { Name = instanceName, BaseType = "Text", ParentContainer = element };
        element.Instances.Add(instance);

        StateSave state = element.DefaultState;
        state.Variables.Add(new VariableSave { Name = instanceName + ".UseCustomFont", Type = "bool", Value = true, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = instanceName + ".CustomFontFile", Type = "string", Value = customFontFilePath, IsFile = true, SetsValue = true });
        return instance;
    }

    private static InstanceSave AddComponentInstance(ElementSave element, string instanceName, string baseComponentName)
    {
        InstanceSave instance = new InstanceSave { Name = instanceName, BaseType = baseComponentName, ParentContainer = element };
        element.Instances.Add(instance);
        return instance;
    }

    /// <summary>
    /// Standardizes a path the same way <c>FilePath.StandardizedCaseSensitive</c> does for
    /// already-absolute paths: forward slashes, no leading "./". Used to build expectations
    /// that match the historical (existing impl) output format.
    /// </summary>
    private static string Standardize(string absolutePath)
    {
        return new FilePath(absolutePath).StandardizedCaseSensitive;
    }

    // -----------------------------------------------------------------------
    // Tests (alphabetical)
    // -----------------------------------------------------------------------

    [Fact]
    public void GetAllFilesInProject_includes_custom_font_file_when_UseCustomFont_is_true()
    {
        const string customFontPath = FakeProjectDirectory + "Fonts/MyCustom.ttf";

        ScreenSave screen = BuildScreen("MainMenu");
        AddTextInstanceWithCustomFont(screen, "Text1", customFontPath);
        Project.Screens.Add(screen);
        SetProjectFullFileName();

        IEnumerable<string> files = ObjectFinder.Self.GetAllFilesInProject();

        files.ShouldContain(Standardize(customFontPath));
    }

    [Fact]
    public void GetAllFilesInProject_includes_font_cache_fnt_path_for_text_with_default_font_settings()
    {
        ScreenSave screen = BuildScreen("MainMenu");
        AddTextInstanceWithDefaultFont(screen, "Text1", "Arial", 18);
        Project.Screens.Add(screen);
        SetProjectFullFileName();

        string fntRelative = BmfcSave.GetFontCacheFileNameFor(
            fontSize: 18, fontName: "Arial", outline: 0, useFontSmoothing: true, isItalic: false, isBold: false);
        string expectedFntAbsolute = Standardize(FakeProjectDirectory + fntRelative);

        IEnumerable<string> files = ObjectFinder.Self.GetAllFilesInProject();

        files.ShouldContain(expectedFntAbsolute);
    }

    [Fact]
    public void GetAllFilesInProject_includes_gucx_paths_for_component_instances()
    {
        ComponentSave button = BuildComponent("Button");
        Project.Components.Add(button);

        ScreenSave screen = BuildScreen("MainMenu");
        AddComponentInstance(screen, "MyButton", "Button");
        Project.Screens.Add(screen);
        SetProjectFullFileName();

        IEnumerable<string> files = ObjectFinder.Self.GetAllFilesInProject();

        string expected = Standardize(FakeProjectDirectory + "Components/Button." + button.FileExtension);
        files.ShouldContain(expected);
    }

    [Fact]
    public void GetAllFilesInProject_includes_referenced_textures_via_SourceFile()
    {
        const string spritePath = FakeProjectDirectory + "Textures/bg.png";

        ScreenSave screen = BuildScreen("MainMenu");
        AddSpriteInstance(screen, "Sprite1", spritePath);
        Project.Screens.Add(screen);
        SetProjectFullFileName();

        IEnumerable<string> files = ObjectFinder.Self.GetAllFilesInProject();

        files.ShouldContain(Standardize(spritePath));
    }

    [Fact]
    public void GetAllFilesInProject_returns_absolute_paths_not_relative()
    {
        const string spritePath = FakeProjectDirectory + "Textures/bg.png";

        ScreenSave screen = BuildScreen("MainMenu");
        AddSpriteInstance(screen, "Sprite1", spritePath);
        Project.Screens.Add(screen);
        SetProjectFullFileName();

        IEnumerable<string> files = ObjectFinder.Self.GetAllFilesInProject();

        foreach (string file in files)
        {
            // Absolute on Windows means rooted (drive letter) or starts with /.
            // The existing impl always returns paths that FilePath considers rooted.
            Path.IsPathRooted(file).ShouldBeTrue($"Expected absolute path, got '{file}'");
        }
    }

    [Fact]
    public void GetAllFilesInProject_returns_distinct_paths_when_same_file_referenced_multiply()
    {
        const string sharedTexture = FakeProjectDirectory + "Textures/shared.png";

        ScreenSave screenA = BuildScreen("ScreenA");
        AddSpriteInstance(screenA, "Sprite1", sharedTexture);
        AddSpriteInstance(screenA, "Sprite2", sharedTexture);

        ScreenSave screenB = BuildScreen("ScreenB");
        AddSpriteInstance(screenB, "Sprite1", sharedTexture);

        Project.Screens.Add(screenA);
        Project.Screens.Add(screenB);
        SetProjectFullFileName();

        List<string> files = ObjectFinder.Self.GetAllFilesInProject().ToList();

        string expected = Standardize(sharedTexture);
        files.Count(f => f == expected).ShouldBe(1);
    }

    [Fact]
    public void GetFilesReferencedBy_excludes_files_referenced_only_by_other_elements()
    {
        const string ownTexture = FakeProjectDirectory + "Textures/own.png";
        const string otherTexture = FakeProjectDirectory + "Textures/other.png";

        ScreenSave screenA = BuildScreen("ScreenA");
        AddSpriteInstance(screenA, "Sprite1", ownTexture);

        ScreenSave screenB = BuildScreen("ScreenB");
        AddSpriteInstance(screenB, "Sprite1", otherTexture);

        Project.Screens.Add(screenA);
        Project.Screens.Add(screenB);
        SetProjectFullFileName();

        List<string> files = ObjectFinder.Self.GetFilesReferencedBy(screenA);

        files.ShouldContain(Standardize(ownTexture));
        files.ShouldNotContain(Standardize(otherTexture));
    }

    [Fact]
    public void GetFilesReferencedBy_returns_absolute_paths()
    {
        const string spritePath = FakeProjectDirectory + "Textures/bg.png";

        ScreenSave screen = BuildScreen("MainMenu");
        AddSpriteInstance(screen, "Sprite1", spritePath);
        Project.Screens.Add(screen);
        SetProjectFullFileName();

        List<string> files = ObjectFinder.Self.GetFilesReferencedBy(screen);

        foreach (string file in files)
        {
            Path.IsPathRooted(file).ShouldBeTrue($"Expected absolute path, got '{file}'");
        }
    }
}
