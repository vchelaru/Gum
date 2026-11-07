using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// A project's ordered show/hide rules decide which standard variables its target engine supports (#7720).
/// </summary>
public class ProjectVariableRuleMatcherTests : BaseTestClass
{
    private readonly ProjectVariableRuleMatcher _matcher = new ProjectVariableRuleMatcher();

    private static EngineProfile CreateProject(bool hideByDefault, params (string Pattern, VariableRuleAction Action)[] rules)
    {
        EngineProfile project = new EngineProfile { HideVariablesByDefault = hideByDefault };
        foreach ((string pattern, VariableRuleAction action) in rules)
        {
            project.VariableRules.Add(new VariableRule { Pattern = pattern, Action = action });
        }
        return project;
    }

    [Fact]
    public void IsHidden_ShouldBeFalse_WhenProjectHasNoRules()
    {
        EngineProfile project = CreateProject(hideByDefault: false);

        _matcher.IsHidden(project, "Sprite", "Rotation").ShouldBeFalse();
    }

    [Fact]
    public void IsHidden_ShouldHideEverythingExceptAllowedNames_WhenDefaultIsHide()
    {
        EngineProfile project = CreateProject(
            hideByDefault: true,
            ("Sprite.X", VariableRuleAction.Show),
            ("Sprite.Y", VariableRuleAction.Show));

        _matcher.IsHidden(project, "Sprite", "X").ShouldBeFalse();
        _matcher.IsHidden(project, "Sprite", "Y").ShouldBeFalse();
        _matcher.IsHidden(project, "Sprite", "Rotation").ShouldBeTrue();
    }

    [Fact]
    public void IsHidden_ShouldNotApplyQualifiedRuleToOtherTypes()
    {
        EngineProfile project = CreateProject(
            hideByDefault: true,
            ("Sprite.X", VariableRuleAction.Show));

        _matcher.IsHidden(project, "Rectangle", "X").ShouldBeTrue();
    }

    [Fact]
    public void IsHidden_ShouldApplyUnqualifiedRuleToEveryType()
    {
        EngineProfile project = CreateProject(
            hideByDefault: true,
            ("X", VariableRuleAction.Show));

        _matcher.IsHidden(project, "Sprite", "X").ShouldBeFalse();
        _matcher.IsHidden(project, "Rectangle", "X").ShouldBeFalse();
        _matcher.IsHidden(project, null, "X").ShouldBeFalse();
    }

    [Fact]
    public void IsHidden_ShouldLetTheLastMatchingRuleWin()
    {
        EngineProfile project = CreateProject(
            hideByDefault: false,
            ("Sprite.*", VariableRuleAction.Hide),
            ("Sprite.X", VariableRuleAction.Show));

        _matcher.IsHidden(project, "Sprite", "X").ShouldBeFalse();
        _matcher.IsHidden(project, "Sprite", "Y").ShouldBeTrue();
        _matcher.IsHidden(project, "Rectangle", "Y").ShouldBeFalse();
    }

    [Theory]
    [InlineData("Gradient*", "GradientType", true)]
    [InlineData("Gradient*", "UseGradient", false)]
    [InlineData("*Alpha", "FillAlpha", true)]
    [InlineData("*Alpha", "AlphaMode", false)]
    [InlineData("*", "Anything", true)]
    public void IsHidden_ShouldSupportWildcards(string pattern, string variableName, bool expectedHidden)
    {
        EngineProfile project = CreateProject(
            hideByDefault: false,
            (pattern, VariableRuleAction.Hide));

        _matcher.IsHidden(project, "Rectangle", variableName).ShouldBe(expectedHidden);
    }
}
