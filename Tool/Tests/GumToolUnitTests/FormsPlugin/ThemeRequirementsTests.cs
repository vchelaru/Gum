using Gum.DataTypes;
using Gum.Logic;
using GumFormsPlugin.Services;
using Moq;
using Shouldly;

namespace GumToolUnitTests.FormsPlugin;

public class ThemeRequirementsTests
{
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
    public void Diff_SkipsSkiaShapeAdd_WhenRoundedRectangleAlreadyPresent()
    {
        var project = new GumProjectSave();
        project.StandardElementReferences.Add(new ElementReference
        {
            Name = "RoundedRectangle",
            ElementType = ElementType.Standard,
        });

        var requirements = ThemeRequirements.Parse("RequiresSkiaShapes: true");

        var diff = requirements.Diff(project);
        diff.AddSkiaShapes.ShouldBeFalse();
        diff.HasChanges.ShouldBeFalse();
    }

    [Fact]
    public void Diff_NoChanges_WhenProjectAlreadySatisfiesRequirements()
    {
        var project = new GumProjectSave { FontGenerator = FontGeneratorType.KernSmith };
        project.StandardElementReferences.Add(new ElementReference
        {
            Name = "RoundedRectangle",
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
}
