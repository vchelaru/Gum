using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// Two font files with the same name in different folders (<c>Fonts/Icons.ttf</c>, <c>Other/Icons.ttf</c>)
/// need separate atlases. The cache name only includes a hash of the file's path from
/// <see cref="GumProjectSave.GumxVersions.FontCachePathHash"/> on; older projects keep the legacy name
/// so the .fnt files they already ship are still the ones the runtime finds.
/// </summary>
public class FontReferenceCollectorSameNameFontFileTests : IDisposable
{
    public FontReferenceCollectorSameNameFontFileTests()
    {
        StandardElementsManager.Self.Initialize();
    }

    [Fact]
    public void Collect_generates_one_font_per_file_for_same_named_files_at_the_hashed_cache_version()
    {
        GumProjectSave project = BuildProjectWithSameNamedFontFiles(GumProjectSave.NativeVersion);

        Dictionary<string, BmfcSave> fonts = Collect(project);

        fonts.Count.ShouldBe(2);
        fonts.Values.Select(font => font.FontFile).OrderBy(file => file).ShouldBe(new[] { "Fonts/Icons.ttf", "Other/Icons.ttf" });
    }

    [Fact]
    public void Collect_keeps_the_legacy_cache_name_for_a_project_below_the_hashed_cache_version()
    {
        GumProjectSave project = BuildProjectWithSameNamedFontFiles((int)GumProjectSave.GumxVersions.ColorOperationExpansion);

        Dictionary<string, BmfcSave> fonts = Collect(project);

        fonts.Keys.Select(key => key.Replace('\\', '/')).ShouldBe(new[] { "FontCache/Font18Icons_ttf.fnt" });
    }

    [Fact]
    public void UsesPathHashedFontCacheNames_starts_at_the_FontCachePathHash_version()
    {
        new GumProjectSave { Version = (int)GumProjectSave.GumxVersions.FontCachePathHash - 1 }
            .UsesPathHashedFontCacheNames.ShouldBeFalse();
        new GumProjectSave { Version = (int)GumProjectSave.GumxVersions.FontCachePathHash }
            .UsesPathHashedFontCacheNames.ShouldBeTrue();
    }

    private static GumProjectSave BuildProjectWithSameNamedFontFiles(int projectVersion)
    {
        StandardElementSave textStandard = TestProjectBuilder.BuildStandard("Text");
        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        TestProjectBuilder.AddTextInstanceWithFontCache(screen, "IconsA", "Fonts/Icons.ttf", fontSize: 18);
        TestProjectBuilder.AddTextInstanceWithFontCache(screen, "IconsB", "Other/Icons.ttf", fontSize: 18);

        GumProjectSave project = TestProjectBuilder.BuildProject(screens: new[] { screen }, standards: new[] { textStandard });
        project.Version = projectVersion;
        ObjectFinder.Self.GumProjectSave = project;
        return project;
    }

    private static Dictionary<string, BmfcSave> Collect(GumProjectSave project)
    {
        FontReferenceCollector collector = new FontReferenceCollector(
            instance => ObjectFinder.Self.GetElementSave(instance));
        return collector.Collect(project, project.AllElements);
    }

    public void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }
}
