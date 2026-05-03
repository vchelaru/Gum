using System;
using System.Collections.Generic;
using System.IO;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// Coverage for the walker's font-cache resolution path. Mirrors the bug observed in the
/// Solitaire FRB2 sample (a 13-font Forms project bundled only 1 font) by exercising the
/// partial-state-override + Standards-inheritance scenario that the walker's old hand-rolled
/// six-variable conjunction silently dropped.
/// </summary>
public class GumProjectDependencyWalkerFontTests : IDisposable
{
    private readonly List<string> _tempDirectories;
    private static readonly byte[] EmptyContent = TestProjectBuilder.EmptyXmlBytes;

    public GumProjectDependencyWalkerFontTests()
    {
        _tempDirectories = new List<string>();
    }

    [Fact]
    public void Walk_with_FontCache_collects_font_when_state_sets_all_six_font_variables_directly()
    {
        // Regression guard for the simple direct-on-element path: a screen with a Text instance
        // whose state sets every font variable explicitly should still produce exactly one font
        // in the bundle. Prior to the FontReferenceCollector refactor this was the only case the
        // walker handled correctly.
        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        TestProjectBuilder.AddTextInstanceWithFontCache(screen, "Label", "Arial", 18);

        StandardElementSave textStandard = TestProjectBuilder.BuildStandard("Text");
        GumProjectSave project = TestProjectBuilder.BuildProject(
            screens: new[] { screen },
            standards: new[] { textStandard });

        string fntRelative = BmfcSave.GetFontCacheFileNameFor(
            18, "Arial", outline: 0, useFontSmoothing: true, isItalic: false, isBold: false).Replace('\\', '/');
        string fntBase = Path.GetFileNameWithoutExtension(fntRelative);

        string root = CreateProjectRoot(new[]
        {
            ("Screens/MainMenu.gusx", EmptyContent),
            ("Standards/Text.gutx", EmptyContent),
            (fntRelative, EmptyContent),
            ("FontCache/" + fntBase + "_0.png", EmptyContent),
        });

        ObjectFinder.Self.GumProjectSave = project;

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.FontCache);

        result.FontCacheFiles.ShouldContain(fntRelative);
        result.FontCacheFiles.ShouldContain("FontCache/" + fntBase + "_0.png");
    }

    [Fact]
    public void Walk_with_FontCache_resolves_partial_state_override_via_Standards_inheritance()
    {
        // Mirrors the Solitaire / Forms-themed Styles.gucx scenario:
        //   - Standards/Text.gutx default state defines all six font variables.
        //   - Components/Styles.gucx contains a Text instance "Strong" whose default state
        //     overrides ONLY FontSize and IsBold (Font, OutlineThickness, UseFontSmoothing,
        //     and IsItalic are intentionally not set on Strong).
        // The expected bundled font is the resolved combination — overrides from Strong
        // plus inherited defaults from Standards/Text.
        StandardElementSave textStandard = TestProjectBuilder.BuildStandard("Text");
        StateSave textDefault = textStandard.DefaultState;
        textDefault.Variables.Add(new VariableSave { SetsValue = true, Name = "Font", Value = "Arial" });
        textDefault.Variables.Add(new VariableSave { SetsValue = true, Name = "FontSize", Value = 14 });
        textDefault.Variables.Add(new VariableSave { SetsValue = true, Name = "OutlineThickness", Value = 0 });
        textDefault.Variables.Add(new VariableSave { SetsValue = true, Name = "UseFontSmoothing", Value = true });
        textDefault.Variables.Add(new VariableSave { SetsValue = true, Name = "IsItalic", Value = false });
        textDefault.Variables.Add(new VariableSave { SetsValue = true, Name = "IsBold", Value = false });

        ComponentSave styles = new ComponentSave { Name = "Styles", BaseType = "Container" };
        StateSave stylesDefault = new StateSave { Name = "Default", ParentContainer = styles };
        styles.States.Add(stylesDefault);
        InstanceSave strong = new InstanceSave { Name = "Strong", BaseType = "Text", ParentContainer = styles };
        styles.Instances.Add(strong);
        // Partial override: only FontSize and IsBold — the rest inherit from Standards/Text.
        stylesDefault.Variables.Add(new VariableSave { SetsValue = true, Name = "Strong.FontSize", Value = 20 });
        stylesDefault.Variables.Add(new VariableSave { SetsValue = true, Name = "Strong.IsBold", Value = true });

        GumProjectSave project = TestProjectBuilder.BuildProject(
            components: new[] { styles },
            standards: new[] { textStandard });

        string expectedFnt = BmfcSave.GetFontCacheFileNameFor(
            fontSize: 20, fontName: "Arial", outline: 0, useFontSmoothing: true,
            isItalic: false, isBold: true).Replace('\\', '/');
        string expectedFntBase = Path.GetFileNameWithoutExtension(expectedFnt);

        string root = CreateProjectRoot(new[]
        {
            ("Components/Styles.gucx", EmptyContent),
            ("Standards/Text.gutx", EmptyContent),
            (expectedFnt, EmptyContent),
            ("FontCache/" + expectedFntBase + "_0.png", EmptyContent),
        });

        ObjectFinder.Self.GumProjectSave = project;

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.Core | GumBundleInclusion.FontCache);

        result.FontCacheFiles.ShouldContain(expectedFnt,
            customMessage: $"Walker should resolve partial-state override + Standards inheritance to '{expectedFnt}'.");
        result.FontCacheFiles.ShouldContain("FontCache/" + expectedFntBase + "_0.png");
    }

    private string CreateProjectRoot(IEnumerable<(string, byte[])> files)
    {
        List<(string, byte[])> withProjectFile = new List<(string, byte[])>(files);
        bool hasProjectFile = false;
        foreach ((string path, _) in withProjectFile)
        {
            if (path.EndsWith("." + GumProjectSave.ProjectExtension, StringComparison.Ordinal))
            {
                hasProjectFile = true;
                break;
            }
        }
        if (!hasProjectFile)
        {
            withProjectFile.Add((TestProjectBuilder.DefaultProjectName + "." + GumProjectSave.ProjectExtension, EmptyContent));
        }
        string dir = TestProjectBuilder.CreateTempProjectDirectory(withProjectFile);
        _tempDirectories.Add(dir);
        return dir;
    }

    public void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
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
