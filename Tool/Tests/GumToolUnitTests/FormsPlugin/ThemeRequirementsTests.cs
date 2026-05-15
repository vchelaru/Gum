using System.Linq;
using Gum.DataTypes;
using GumFormsPlugin.Services;
using Shouldly;

namespace GumToolUnitTests.FormsPlugin;

public class ThemeRequirementsTests
{
    [Fact]
    public void Apply_AddsMissingStandardsAndSwitchesFontGenerator()
    {
        var project = new GumProjectSave { FontGenerator = FontGeneratorType.BmFont };
        project.StandardElementReferences.Add(new ElementReference { Name = "Rectangle", ElementType = ElementType.Standard });

        var requirements = ThemeRequirements.Parse(
            "FontGenerator: KernSmith\n" +
            "RequiredStandards: RoundedRectangle, ColoredCircle\n");

        var diff = requirements.Diff(project);
        diff.HasGumxChanges.ShouldBeTrue();
        diff.FontGeneratorChange.ShouldBe(FontGeneratorType.KernSmith);
        diff.StandardsToAdd.ShouldBe(new[] { "RoundedRectangle", "ColoredCircle" });

        diff.Apply(project);
        project.FontGenerator.ShouldBe(FontGeneratorType.KernSmith);
        project.StandardElementReferences.Select(r => r.Name)
            .ShouldBe(new[] { "Rectangle", "RoundedRectangle", "ColoredCircle" });
    }

    [Fact]
    public void Diff_IgnoresStandardsAlreadyPresentCaseInsensitively()
    {
        var project = new GumProjectSave();
        project.StandardElementReferences.Add(new ElementReference { Name = "roundedrectangle", ElementType = ElementType.Standard });

        var requirements = ThemeRequirements.Parse("RequiredStandards: RoundedRectangle, ColoredCircle");

        var diff = requirements.Diff(project);
        diff.StandardsToAdd.ShouldBe(new[] { "ColoredCircle" });
    }

    [Fact]
    public void Diff_NoChanges_WhenProjectAlreadySatisfiesRequirements()
    {
        var project = new GumProjectSave { FontGenerator = FontGeneratorType.KernSmith };
        project.StandardElementReferences.Add(new ElementReference { Name = "RoundedRectangle", ElementType = ElementType.Standard });

        var requirements = ThemeRequirements.Parse(
            "FontGenerator: KernSmith\nRequiredStandards: RoundedRectangle");

        var diff = requirements.Diff(project);
        diff.HasGumxChanges.ShouldBeFalse();
        diff.FontGeneratorChange.ShouldBeNull();
        diff.StandardsToAdd.ShouldBeEmpty();
    }

    [Fact]
    public void Parse_HandlesCommentsAndBlankLinesAndUnknownKeys()
    {
        var requirements = ThemeRequirements.Parse(
            "# header comment\n" +
            "\n" +
            "FontGenerator: KernSmith\n" +
            "FutureKey: somevalue\n" +
            "RuntimePackages: A, B\n");

        requirements.FontGenerator.ShouldBe(FontGeneratorType.KernSmith);
        requirements.RuntimePackages.ShouldBe(new[] { "A", "B" });
        requirements.RequiredStandards.ShouldBeEmpty();
    }

    [Fact]
    public void Parse_EmptyInput_YieldsNoRequirements()
    {
        var requirements = ThemeRequirements.Parse(string.Empty);

        requirements.FontGenerator.ShouldBeNull();
        requirements.RequiredStandards.ShouldBeEmpty();
        requirements.RuntimePackages.ShouldBeEmpty();
    }
}
