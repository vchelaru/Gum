using System;
using System.IO;
using Gum.DataTypes;
using Gum.Logic;
using GumFormsPlugin.Services;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

public class ThemeRequirementsTests
{
    [Fact]
    public void BubblegumTheme_DoesNotRequireSkiaShapes()
    {
        // Bubblegum was migrated off the Skia-backed RoundedRectangle / ColoredCircle standards
        // onto the v3 Rectangle / Circle standards, so importing it must no longer inject the
        // full Skia shape bundle into the user's project.
        string themeDirectory = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "Bubblegum");

        ThemeRequirements requirements = ThemeRequirements.LoadFromThemeDirectory(themeDirectory);

        requirements.RequiresSkiaShapes.ShouldBeFalse();
        requirements.FontGenerator.ShouldBe(FontGeneratorType.KernSmith);
    }


    [Fact]
    public void Apply_AddsSkiaShapesAndSwitchesFontGenerator()
    {
        var project = new GumProjectSave { FontGenerator = FontGeneratorType.BmFont };
        var skiaShapes = new Mock<ISkiaShapeStandardsLogic>();

        var requirements = ThemeRequirements.Parse(
            "FontGenerator: KernSmith\nRequiresSkiaShapes: true\n");

        var diff = requirements.Diff(project);
        diff.HasChanges.ShouldBeTrue();
        diff.FontGeneratorChange.ShouldBe(FontGeneratorType.KernSmith);
        diff.AddSkiaShapes.ShouldBeTrue();

        diff.Apply(project, skiaShapes.Object);
        project.FontGenerator.ShouldBe(FontGeneratorType.KernSmith);
        skiaShapes.Verify(s => s.AddAllStandards(), Times.Once);
    }

    [Fact]
    public void Diff_SkipsSkiaShapeAdd_WhenSvgAlreadyPresent()
    {
        // Svg is the "already has the Skia bundle" proxy: it is added on every project version
        // (unlike the legacy RoundedRectangle / ColoredCircle, which are no longer added on V3+),
        // so its presence is the version-proof signal that the bundle is in place.
        var project = new GumProjectSave();
        project.StandardElementReferences.Add(new ElementReference
        {
            Name = "Svg",
            ElementType = ElementType.Standard,
        });

        var requirements = ThemeRequirements.Parse("RequiresSkiaShapes: true");

        var diff = requirements.Diff(project);
        diff.AddSkiaShapes.ShouldBeFalse();
        diff.HasChanges.ShouldBeFalse();
    }

    [Fact]
    public void Diff_AddsSkiaShapes_WhenOnlyLegacyShapePresent()
    {
        // A V3+ project no longer gets RoundedRectangle added, so RoundedRectangle's presence must
        // NOT be treated as proof the Skia bundle exists. A project that somehow has only the legacy
        // shape (e.g. an old project) but not Svg should still have the bundle applied.
        var project = new GumProjectSave();
        project.StandardElementReferences.Add(new ElementReference
        {
            Name = "RoundedRectangle",
            ElementType = ElementType.Standard,
        });

        var requirements = ThemeRequirements.Parse("RequiresSkiaShapes: true");

        var diff = requirements.Diff(project);
        diff.AddSkiaShapes.ShouldBeTrue();
    }

    [Fact]
    public void Diff_NoChanges_WhenProjectAlreadySatisfiesRequirements()
    {
        var project = new GumProjectSave { FontGenerator = FontGeneratorType.KernSmith };
        project.StandardElementReferences.Add(new ElementReference
        {
            Name = "Svg",
            ElementType = ElementType.Standard,
        });

        var requirements = ThemeRequirements.Parse(
            "FontGenerator: KernSmith\nRequiresSkiaShapes: true");

        var diff = requirements.Diff(project);
        diff.HasChanges.ShouldBeFalse();
        diff.FontGeneratorChange.ShouldBeNull();
        diff.AddSkiaShapes.ShouldBeFalse();
    }

    [Fact]
    public void NeonTheme_RequiresTheCheckMarkCharacter()
    {
        // Neon ships no cached check-mark font, so the importing project must generate it with U+2713.
        string themeDirectory = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "Neon");

        ThemeRequirements requirements = ThemeRequirements.LoadFromThemeDirectory(themeDirectory);

        global::RenderingLibrary.Graphics.Fonts.BmfcSave.ParseCharRanges(requirements.FontRanges!).ShouldContain(0x2713);
    }

    [Fact]
    public void LoadFromThemeDirectory_ReadsTheThemeProjectsFontRanges()
    {
        string themeDirectory = Path.Combine(Path.GetTempPath(), "GumThemeRanges_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(themeDirectory);
        try
        {
            File.WriteAllText(Path.Combine(themeDirectory, "GumProject.gumx"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<GumProjectSave>\n  <FontRanges>32-126,10003-10007</FontRanges>\n</GumProjectSave>");

            ThemeRequirements requirements = ThemeRequirements.LoadFromThemeDirectory(themeDirectory);

            requirements.FontRanges.ShouldBe("32-126,10003-10007");
        }
        finally
        {
            Directory.Delete(themeDirectory, recursive: true);
        }
    }

    [Fact]
    public void LoadFromThemeDirectory_HasNoFontRanges_WhenTheThemeProjectIsNotValidXml()
    {
        string themeDirectory = Path.Combine(Path.GetTempPath(), "GumThemeRanges_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(themeDirectory);
        try
        {
            File.WriteAllText(Path.Combine(themeDirectory, "GumProject.gumx"), "<GumProjectSave><FontRanges>32-126");

            ThemeRequirements requirements = ThemeRequirements.LoadFromThemeDirectory(themeDirectory);

            requirements.FontRanges.ShouldBeNull();
        }
        finally
        {
            Directory.Delete(themeDirectory, recursive: true);
        }
    }

    [Fact]
    public void Diff_MergesThemeFontRangesIntoTheProjects_WhenTheProjectLacksSomeCharacters()
    {
        var project = new GumProjectSave { FontRanges = "32-126,1024-1100" };
        var requirements = new ThemeRequirements { FontRanges = "32-126,9632-9727,10003-10007" };

        var diff = requirements.Diff(project);

        diff.HasChanges.ShouldBeTrue();
        diff.FontRangesChange.ShouldBe("32-126,1024-1100,9632-9727,10003-10007");
        diff.DescribeChanges().ShouldContain(line => line.Contains("font"));
        diff.Apply(project, new Mock<ISkiaShapeStandardsLogic>().Object);
        project.FontRanges.ShouldBe("32-126,1024-1100,9632-9727,10003-10007");
    }

    [Fact]
    public void Diff_NoFontRangesChange_WhenTheProjectAlreadyCoversTheThemesCharacters()
    {
        var project = new GumProjectSave { FontRanges = "32-500,9000-11000" };
        var requirements = new ThemeRequirements { FontRanges = "32-126,10003-10007" };

        var diff = requirements.Diff(project);

        diff.FontRangesChange.ShouldBeNull();
        diff.HasChanges.ShouldBeFalse();
    }

    [Fact]
    public void Parse_HandlesCommentsAndBlankLinesAndUnknownKeys()
    {
        var requirements = ThemeRequirements.Parse(
            "# header comment\n" +
            "\n" +
            "FontGenerator: KernSmith\n" +
            "FutureKey: somevalue\n" +
            "RequiresSkiaShapes: true\n");

        requirements.FontGenerator.ShouldBe(FontGeneratorType.KernSmith);
        requirements.RequiresSkiaShapes.ShouldBeTrue();
    }

    [Fact]
    public void Parse_EmptyInput_YieldsNoRequirements()
    {
        var requirements = ThemeRequirements.Parse(string.Empty);

        requirements.FontGenerator.ShouldBeNull();
        requirements.RequiresSkiaShapes.ShouldBeFalse();
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
