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
