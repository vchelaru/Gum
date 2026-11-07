using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests.Managers;

/// <summary>
/// What the loaded project's restrictions take away from the tool (#7720).
/// </summary>
public class ProjectRestrictionsTests : BaseTestClass
{
    private readonly GumProjectSave _project;
    private readonly ProjectRestrictions _sut = new ProjectRestrictions();

    public ProjectRestrictionsTests()
    {
        _project = new GumProjectSave { Version = GumProjectSave.NativeVersion };
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_project);
        ObjectFinder.Self.GumProjectSave = _project;
    }

    private void RestrictToSprite()
    {
        _project.HideVariablesByDefault = true;
        _project.VariableRules.Add(new VariableRule { Pattern = "Sprite.X", Action = VariableRuleAction.Show });
        _project.SupportedStandardTypes.Add("Sprite");
        _project.DisableComponents = true;
        _project.DisableBehaviors = true;
    }

    [Fact]
    public void Queries_ShouldBeFalse_WhenProjectHasNoRestrictions()
    {
        StandardElementSave container = _project.StandardElements.First(item => item.Name == "Container");
        ComponentSave component = new ComponentSave { Name = "Button" };

        _sut.IsInstanceTypeBlocked(container).ShouldBeFalse();
        _sut.IsInstanceTypeBlocked(component).ShouldBeFalse();
        _sut.IsVariableHidden("Container", "Parent").ShouldBeFalse();
        _sut.IsHierarchyBlockedFor(container).ShouldBeFalse();
        _sut.IsHierarchyBlocked.ShouldBeFalse();
        _sut.AreComponentsDisabled.ShouldBeFalse();
        _sut.AreBehaviorsDisabled.ShouldBeFalse();
    }

    [Fact]
    public void IsInstanceTypeBlocked_ShouldBlockUnlistedStandardsAndComponents()
    {
        RestrictToSprite();
        StandardElementSave sprite = _project.StandardElements.First(item => item.Name == "Sprite");
        StandardElementSave text = _project.StandardElements.First(item => item.Name == "Text");

        _sut.IsInstanceTypeBlocked(sprite).ShouldBeFalse();
        _sut.IsInstanceTypeBlocked(text).ShouldBeTrue();
        _sut.IsInstanceTypeBlocked(new ComponentSave { Name = "Button" }).ShouldBeTrue();
    }

    [Fact]
    public void IsHierarchyBlocked_ShouldFollowTheParentRule()
    {
        RestrictToSprite();

        _sut.IsHierarchyBlocked.ShouldBeTrue();

        _project.VariableRules.Add(new VariableRule { Pattern = "Sprite.Parent", Action = VariableRuleAction.Show });

        _sut.IsHierarchyBlocked.ShouldBeFalse();
    }

    [Fact]
    public void IsEveryVariableHidden_ShouldBeTrue_OnlyWhenAllAreHidden()
    {
        RestrictToSprite();
        StandardElementSave sprite = _project.StandardElements.First(item => item.Name == "Sprite");

        _sut.IsEveryVariableHidden(sprite, "TextureLeft", "Rotation").ShouldBeTrue();
        _sut.IsEveryVariableHidden(sprite, "X", "Rotation").ShouldBeFalse();
    }

    [Fact]
    public void IsVariableHidden_ShouldNeverHideExemptNames()
    {
        RestrictToSprite();

        _sut.IsVariableHidden("Sprite", "Name").ShouldBeFalse();
        _sut.IsVariableHidden("Sprite", "BaseType").ShouldBeFalse();
        _sut.IsVariableHidden("Sprite", "Rotation").ShouldBeTrue();
    }
}
