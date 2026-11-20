using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class AnimationNameRulesTests
{
    [Theory]
    [InlineData(null, "empty")]
    [InlineData("", "empty")]
    [InlineData("Has Space", "spaces")]
    [InlineData("fadein", "already")]
    public void GetWhyInvalid_ExplainsTheProblem(string? name, string expectedInReason)
    {
        AnimationNameRules.GetWhyInvalid(name, new[] { "FadeIn" }).ShouldContain(expectedInReason);
    }

    [Fact]
    public void GetWhyInvalid_AcceptsAFreeName()
    {
        AnimationNameRules.GetWhyInvalid("FadeOut", new[] { "FadeIn" }).ShouldBeNull();
    }
}
