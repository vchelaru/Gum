using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolStates;
using Moq;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// A behavior's name must be unused by another behavior, compared the way the file system and the
/// rest of the tool compare names: renaming onto an existing behavior overwrote its file (#7019).
/// </summary>
public class BehaviorNameValidationTests : BaseTestClass
{
    private readonly NameVerifier _nameVerifier = new NameVerifier(
        new Mock<IVariableSaveLogic>().Object, new Mock<IPluginManager>().Object, new Mock<ISelectedState>().Object);
    private readonly BehaviorSave _clickable = new BehaviorSave { Name = "Clickable" };
    private readonly BehaviorSave _other = new BehaviorSave { Name = "Other" };

    public BehaviorNameValidationTests()
    {
        GumProjectSave project = new GumProjectSave();
        project.Behaviors.Add(_clickable);
        project.Behaviors.Add(_other);
        ObjectFinder.Self.GumProjectSave = project;
    }

    [Theory]
    [InlineData("Other")]
    [InlineData("other")]
    [InlineData("OTHER")]
    public void IsBehaviorNameValid_OnAnotherBehaviorsName_IsRejected(string name)
    {
        _nameVerifier.IsBehaviorNameValid(name, _clickable, out string? whyNotValid).ShouldBeFalse();
        whyNotValid.ShouldBe("There is already a behavior named Other so this name can't be used.");
    }

    [Fact]
    public void IsBehaviorNameValid_WhenAddingAnExistingName_IsRejected()
    {
        _nameVerifier.IsBehaviorNameValid("Clickable", null, out string? whyNotValid).ShouldBeFalse();
        whyNotValid.ShouldBe("There is already a behavior named Clickable so this name can't be used.");
    }

    [Theory]
    [InlineData("Clickable")]
    [InlineData("clickable")]
    [InlineData("Pressable")]
    public void IsBehaviorNameValid_OnTheBehaviorsOwnNameOrAFreeName_IsAccepted(string name)
    {
        _nameVerifier.IsBehaviorNameValid(name, _clickable, out _).ShouldBeTrue();
    }
}
