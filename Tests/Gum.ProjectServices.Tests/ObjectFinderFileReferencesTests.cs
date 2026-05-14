using System;
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
    /// which appends a trailing slash. The path must be <em>rooted on the host OS</em> — otherwise
    /// the impl treats it as relative and prepends the working directory. A <c>"C:/..."</c> path
    /// is rooted on Windows but NOT on macOS/Linux, so the base directory is chosen per-platform.
    /// </summary>
    private static readonly string FakeProjectDirectory =
        OperatingSystem.IsWindows() ? "C:/FakeGumProject/" : "/tmp/FakeGumProject/";
    private static readonly string FakeProjectFile = FakeProjectDirectory + "MyProject.gumx";

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
        // CustomFontFile is stored relative to the project directory; the impl resolves it
        // against that directory. Passing an absolute path here would double-resolve.
        const string customFontRelative = "Fonts/MyCustom.ttf";

        ScreenSave screen = BuildScreen("MainMenu");
        AddTextInstanceWithCustomFont(screen, "Text1", customFontRelative);
        Project.Screens.Add(screen);
        SetProjectFullFileName();

        IEnumerable<string> files = ObjectFinder.Self.GetAllFilesInProject();

        files.ShouldContain(Standardize(FakeProjectDirectory + customFontRelative));
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
        const string spriteRelative = "Textures/bg.png";

        ScreenSave screen = BuildScreen("MainMenu");
        AddSpriteInstance(screen, "Sprite1", spriteRelative);
        Project.Screens.Add(screen);
        SetProjectFullFileName();

        IEnumerable<string> files = ObjectFinder.Self.GetAllFilesInProject();

        files.ShouldContain(Standardize(FakeProjectDirectory + spriteRelative));
    }

    [Fact]
    public void GetAllFilesInProject_returns_absolute_paths_not_relative()
    {
        const string spriteRelative = "Textures/bg.png";

        ScreenSave screen = BuildScreen("MainMenu");
        AddSpriteInstance(screen, "Sprite1", spriteRelative);
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
        const string sharedTextureRelative = "Textures/shared.png";

        ScreenSave screenA = BuildScreen("ScreenA");
        AddSpriteInstance(screenA, "Sprite1", sharedTextureRelative);
        AddSpriteInstance(screenA, "Sprite2", sharedTextureRelative);

        ScreenSave screenB = BuildScreen("ScreenB");
        AddSpriteInstance(screenB, "Sprite1", sharedTextureRelative);

        Project.Screens.Add(screenA);
        Project.Screens.Add(screenB);
        SetProjectFullFileName();

        List<string> files = ObjectFinder.Self.GetAllFilesInProject().ToList();

        string expected = Standardize(FakeProjectDirectory + sharedTextureRelative);
        files.Count(f => f == expected).ShouldBe(1);
    }

    [Fact]
    public void GetFilesReferencedBy_excludes_files_referenced_only_by_other_elements()
    {
        const string ownTextureRelative = "Textures/own.png";
        const string otherTextureRelative = "Textures/other.png";

        ScreenSave screenA = BuildScreen("ScreenA");
        AddSpriteInstance(screenA, "Sprite1", ownTextureRelative);

        ScreenSave screenB = BuildScreen("ScreenB");
        AddSpriteInstance(screenB, "Sprite1", otherTextureRelative);

        Project.Screens.Add(screenA);
        Project.Screens.Add(screenB);
        SetProjectFullFileName();

        List<string> files = ObjectFinder.Self.GetFilesReferencedBy(screenA);

        files.ShouldContain(Standardize(FakeProjectDirectory + ownTextureRelative));
        files.ShouldNotContain(Standardize(FakeProjectDirectory + otherTextureRelative));
    }

    [Fact]
    public void GetFilesReferencedBy_returns_absolute_paths()
    {
        const string spriteRelative = "Textures/bg.png";

        ScreenSave screen = BuildScreen("MainMenu");
        AddSpriteInstance(screen, "Sprite1", spriteRelative);
        Project.Screens.Add(screen);
        SetProjectFullFileName();

        List<string> files = ObjectFinder.Self.GetFilesReferencedBy(screen);

        foreach (string file in files)
        {
            Path.IsPathRooted(file).ShouldBeTrue($"Expected absolute path, got '{file}'");
        }
    }
}
