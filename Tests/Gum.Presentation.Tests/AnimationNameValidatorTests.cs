using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using StateAnimationPlugin.Validation;
using StateAnimationPlugin.ViewModels;

namespace Gum.Presentation.Tests;

public class AnimationNameValidatorTests
{
    private readonly Mock<INameVerifier> _verifier = new Mock<INameVerifier>();
    private readonly ComponentSave _element = new ComponentSave { Name = "Popup" };

    public AnimationNameValidatorTests()
    {
        string common = "";
        CommonValidationError commonError = default;
        _verifier.Setup(item => item.IsNameValidCommon(It.IsAny<string?>(), out common, out commonError)).Returns(true);
        string? topLevel = null;
        _verifier.Setup(item => item.IsNameValidTopLevel(It.IsAny<string>(), It.IsAny<ElementSave>(), It.IsAny<object?>(), out topLevel))
            .Returns(true);
    }

    private static AnimationViewModel Animation(string name) =>
        new AnimationViewModel(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = name };

    [Theory]
    [InlineData("", "empty")]
    [InlineData("Has Space", "spaces")]
    [InlineData("fadein", "already being used")]
    public void IsAnimationNameValid_RefusesWithTheReason(string name, string expectedInReason)
    {
        NameValidator validator = new NameValidator(_verifier.Object);

        validator.IsAnimationNameValid(name, _element, new[] { Animation("FadeIn") }, out string? why).ShouldBeFalse();

        why!.ShouldContain(expectedInReason);
    }

    [Fact]
    public void IsAnimationNameValid_IgnoresTheAnimationBeingRenamed()
    {
        NameValidator validator = new NameValidator(_verifier.Object);
        AnimationViewModel renamed = Animation("FadeIn");

        validator.IsAnimationNameValid("FadeIn", _element, new[] { renamed }, out _, renamed).ShouldBeTrue();
    }

    [Fact]
    public void IsAnimationNameValid_RefusesWhatTheCommonRulesRefuse()
    {
        string common = "Bad characters";
        CommonValidationError commonError = default;
        _verifier.Setup(item => item.IsNameValidCommon("A.B", out common, out commonError)).Returns(false);
        NameValidator validator = new NameValidator(_verifier.Object);

        validator.IsAnimationNameValid("A.B", _element, new List<AnimationViewModel>(), out string? why).ShouldBeFalse();

        why.ShouldBe("Bad characters");
    }

    [Fact]
    public void IsAnimationNameValid_RefusesANameAnotherTopLevelItemUses()
    {
        string? topLevel = "The name Spin is already used by a(n) Instance";
        _verifier.Setup(item => item.IsNameValidTopLevel("Spin", _element, It.IsAny<object?>(), out topLevel)).Returns(false);
        NameValidator validator = new NameValidator(_verifier.Object);

        validator.IsAnimationNameValid("Spin", _element, new List<AnimationViewModel>(), out string? why).ShouldBeFalse();

        why.ShouldContain("Instance");
    }
}
