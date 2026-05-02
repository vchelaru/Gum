using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Bundle.Tests;

public class GumProjectDependencyWalkerTests : IDisposable
{
    private readonly List<string> _tempDirectories;
    private static readonly byte[] EmptyContent = TestProjectBuilder.EmptyXmlBytes;

    public GumProjectDependencyWalkerTests()
    {
        _tempDirectories = new List<string>();
    }

    [Fact]
    public void Walk_does_not_duplicate_files_referenced_by_multiple_components()
    {
        const string sharedTexture = "Textures/shared.png";

        ComponentSave componentA = TestProjectBuilder.BuildComponent("CompA");
        TestProjectBuilder.AddSpriteInstance(componentA, "Sprite", sharedTexture);

        ComponentSave componentB = TestProjectBuilder.BuildComponent("CompB");
        TestProjectBuilder.AddSpriteInstance(componentB, "Sprite", sharedTexture);

        GumProjectSave project = TestProjectBuilder.BuildProject(components: new[] { componentA, componentB });
        string root = CreateProjectRoot(new[]
        {
            ("Components/CompA.gucx", EmptyContent),
            ("Components/CompB.gucx", EmptyContent),
            (sharedTexture, EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.Core | GumBundleInclusion.ExternalFiles);

        result.ExternalFiles.Count(p => p == sharedTexture).ShouldBe(1);
    }

    [Fact]
    public void Walk_finds_all_behavior_files_in_project()
    {
        BehaviorSave b1 = TestProjectBuilder.BuildBehavior("Toggle");
        BehaviorSave b2 = TestProjectBuilder.BuildBehavior("Focusable");
        GumProjectSave project = TestProjectBuilder.BuildProject(behaviors: new[] { b1, b2 });
        string root = CreateProjectRoot(new[]
        {
            ("Behaviors/Toggle.behx", EmptyContent),
            ("Behaviors/Focusable.behx", EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.Core);

        result.CoreFiles.ShouldContain("Behaviors/Toggle.behx");
        result.CoreFiles.ShouldContain("Behaviors/Focusable.behx");
    }

    [Fact]
    public void Walk_finds_all_component_files_in_project()
    {
        ComponentSave c1 = TestProjectBuilder.BuildComponent("Button");
        ComponentSave c2 = TestProjectBuilder.BuildComponent("Panel");
        GumProjectSave project = TestProjectBuilder.BuildProject(components: new[] { c1, c2 });
        string root = CreateProjectRoot(new[]
        {
            ("Components/Button.gucx", EmptyContent),
            ("Components/Panel.gucx", EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.Core);

        result.CoreFiles.ShouldContain("Components/Button.gucx");
        result.CoreFiles.ShouldContain("Components/Panel.gucx");
    }

    [Fact]
    public void Walk_finds_all_screen_files_in_project()
    {
        ScreenSave s1 = TestProjectBuilder.BuildScreen("MainMenu");
        ScreenSave s2 = TestProjectBuilder.BuildScreen("Game");
        GumProjectSave project = TestProjectBuilder.BuildProject(screens: new[] { s1, s2 });
        string root = CreateProjectRoot(new[]
        {
            ("Screens/MainMenu.gusx", EmptyContent),
            ("Screens/Game.gusx", EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.Core);

        result.CoreFiles.ShouldContain("Screens/MainMenu.gusx");
        result.CoreFiles.ShouldContain("Screens/Game.gusx");
    }

    [Fact]
    public void Walk_finds_all_standard_element_files_in_project()
    {
        StandardElementSave sprite = TestProjectBuilder.BuildStandard("Sprite");
        StandardElementSave text = TestProjectBuilder.BuildStandard("Text");
        GumProjectSave project = TestProjectBuilder.BuildProject(standards: new[] { sprite, text });
        string root = CreateProjectRoot(new[]
        {
            ("Standards/Sprite.gutx", EmptyContent),
            ("Standards/Text.gutx", EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.Core);

        result.CoreFiles.ShouldContain("Standards/Sprite.gutx");
        result.CoreFiles.ShouldContain("Standards/Text.gutx");
    }

    [Fact]
    public void Walk_normalizes_paths_to_forward_slashes()
    {
        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        // Use a backslash in the source-file value to confirm normalization.
        TestProjectBuilder.AddSpriteInstance(screen, "Sprite", "Textures\\bg.png");
        GumProjectSave project = TestProjectBuilder.BuildProject(screens: new[] { screen });
        string root = CreateProjectRoot(new[]
        {
            ("Screens/MainMenu.gusx", EmptyContent),
            ("Textures/bg.png", EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.Core | GumBundleInclusion.ExternalFiles);

        foreach (string path in result.AllIncludedFiles)
        {
            path.ShouldNotContain("\\");
            path.ShouldNotStartWith("/");
        }
        result.ExternalFiles.ShouldContain("Textures/bg.png");
    }

    [Fact]
    public void Walk_reports_missing_referenced_file_in_MissingFiles_without_throwing()
    {
        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        TestProjectBuilder.AddSpriteInstance(screen, "Sprite", "Textures/missing.png");
        GumProjectSave project = TestProjectBuilder.BuildProject(screens: new[] { screen });
        string root = CreateProjectRoot(new[]
        {
            ("Screens/MainMenu.gusx", EmptyContent),
            // Note: Textures/missing.png intentionally not written
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.Core | GumBundleInclusion.ExternalFiles);

        result.MissingFiles.ShouldContain(w => w.ReferencedPath == "Textures/missing.png");
    }

    [Fact]
    public void Walk_returns_relative_paths_not_absolute()
    {
        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        TestProjectBuilder.AddSpriteInstance(screen, "Sprite", "Textures/bg.png");
        GumProjectSave project = TestProjectBuilder.BuildProject(screens: new[] { screen });
        string root = CreateProjectRoot(new[]
        {
            ("Screens/MainMenu.gusx", EmptyContent),
            ("Textures/bg.png", EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.Core | GumBundleInclusion.ExternalFiles);

        foreach (string path in result.AllIncludedFiles)
        {
            Path.IsPathRooted(path).ShouldBeFalse($"Expected relative path, got '{path}'");
            path.ShouldNotContain(":");
        }
    }

    [Fact]
    public void Walk_with_Core_only_excludes_fonts_and_textures()
    {
        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        TestProjectBuilder.AddSpriteInstance(screen, "Sprite", "Textures/bg.png");
        TestProjectBuilder.AddTextInstanceWithFontCache(screen, "Text", "Arial", 18);
        GumProjectSave project = TestProjectBuilder.BuildProject(screens: new[] { screen });

        string fntRelative = BmfcSave.GetFontCacheFileNameFor(18, "Arial", 0, useFontSmoothing: true).Replace('\\', '/');
        string fntBase = Path.GetFileNameWithoutExtension(fntRelative);

        string root = CreateProjectRoot(new[]
        {
            ("Screens/MainMenu.gusx", EmptyContent),
            ("Textures/bg.png", EmptyContent),
            (fntRelative, EmptyContent),
            ("FontCache/" + fntBase + ".png", EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.Core);

        result.FontCacheFiles.ShouldBeEmpty();
        result.ExternalFiles.ShouldBeEmpty();
        result.CoreFiles.ShouldContain("Screens/MainMenu.gusx");
    }

    [Fact]
    public void Walk_with_ExternalFiles_flag_includes_custom_fonts_outside_FontCache()
    {
        const string customFontPath = "Fonts/MyCustom.ttf";
        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        TestProjectBuilder.AddTextInstanceWithCustomFont(screen, "Text", customFontPath);
        GumProjectSave project = TestProjectBuilder.BuildProject(screens: new[] { screen });
        string root = CreateProjectRoot(new[]
        {
            ("Screens/MainMenu.gusx", EmptyContent),
            (customFontPath, EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.ExternalFiles);

        result.ExternalFiles.ShouldContain(customFontPath);
    }

    [Fact]
    public void Walk_with_ExternalFiles_flag_includes_referenced_png_textures()
    {
        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        TestProjectBuilder.AddSpriteInstance(screen, "Sprite", "Textures/bg.png");
        GumProjectSave project = TestProjectBuilder.BuildProject(screens: new[] { screen });
        string root = CreateProjectRoot(new[]
        {
            ("Screens/MainMenu.gusx", EmptyContent),
            ("Textures/bg.png", EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.ExternalFiles);

        result.ExternalFiles.ShouldContain("Textures/bg.png");
    }

    [Fact]
    public void Walk_with_FontCache_flag_includes_all_png_pages_for_multipage_font()
    {
        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        TestProjectBuilder.AddTextInstanceWithFontCache(screen, "Text", "Arial", 18);
        GumProjectSave project = TestProjectBuilder.BuildProject(screens: new[] { screen });

        string fntRelative = BmfcSave.GetFontCacheFileNameFor(18, "Arial", 0, useFontSmoothing: true).Replace('\\', '/');
        string fntBase = Path.GetFileNameWithoutExtension(fntRelative);

        string root = CreateProjectRoot(new[]
        {
            ("Screens/MainMenu.gusx", EmptyContent),
            (fntRelative, EmptyContent),
            ("FontCache/" + fntBase + "_0.png", EmptyContent),
            ("FontCache/" + fntBase + "_1.png", EmptyContent),
            ("FontCache/" + fntBase + "_2.png", EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.FontCache);

        result.FontCacheFiles.ShouldContain("FontCache/" + fntBase + "_0.png");
        result.FontCacheFiles.ShouldContain("FontCache/" + fntBase + "_1.png");
        result.FontCacheFiles.ShouldContain("FontCache/" + fntBase + "_2.png");
    }

    [Fact]
    public void Walk_with_FontCache_flag_includes_referenced_fnt_and_png_pages()
    {
        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        TestProjectBuilder.AddTextInstanceWithFontCache(screen, "Text", "Arial", 18);
        GumProjectSave project = TestProjectBuilder.BuildProject(screens: new[] { screen });

        string fntRelative = BmfcSave.GetFontCacheFileNameFor(18, "Arial", 0, useFontSmoothing: true).Replace('\\', '/');
        string fntBase = Path.GetFileNameWithoutExtension(fntRelative);

        string root = CreateProjectRoot(new[]
        {
            ("Screens/MainMenu.gusx", EmptyContent),
            (fntRelative, EmptyContent),
            ("FontCache/" + fntBase + ".png", EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.FontCache);

        result.FontCacheFiles.ShouldContain(fntRelative);
        result.FontCacheFiles.ShouldContain("FontCache/" + fntBase + ".png");
    }

    private string CreateProjectRoot(IEnumerable<(string, byte[])> files)
    {
        // Always include the .gumx so the walker can find the project file.
        List<(string, byte[])> withProjectFile = new List<(string, byte[])>(files);
        if (!withProjectFile.Any(t => t.Item1.EndsWith("." + GumProjectSave.ProjectExtension, StringComparison.Ordinal)))
        {
            withProjectFile.Add((TestProjectBuilder.DefaultProjectName + "." + GumProjectSave.ProjectExtension, EmptyContent));
        }
        string dir = TestProjectBuilder.CreateTempProjectDirectory(withProjectFile);
        _tempDirectories.Add(dir);
        return dir;
    }

    public void Dispose()
    {
        foreach (string dir in _tempDirectories)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
