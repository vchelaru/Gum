using Gum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Reproduces the data-driven property-application path that loading a .gumx project uses
/// (<c>ApplyState</c> → <c>SetProperty(string, object)</c>) for the v3 shape surface
/// (<c>IsFilled</c>, <c>FillRed</c>/<c>Green</c>/<c>Blue</c>, <c>StrokeWidth</c>) on
/// <see cref="RectangleRuntime"/> and <see cref="CircleRuntime"/>. Mirrors
/// <c>DataDrivenContainerPropertyTests</c>.
/// </summary>
/// <remarks>
/// <c>CustomSetPropertyOnRenderable.TrySetPropertyOnRectangleRuntime</c> /
/// <c>TrySetPropertyOnCircleRuntime</c> only forwarded the pre-#2768 legacy single-color
/// surface (<c>Color</c>/<c>Alpha</c>/<c>Red</c>/<c>Green</c>/<c>Blue</c>). Any project-file
/// variable outside that set (<c>IsFilled</c>, <c>FillRed</c>, <c>StrokeWidth</c>, etc.) fell
/// through to <c>SetPropertyThroughReflection</c>, which reflects on the contained renderable
/// slot (e.g. <c>SolidRectangle</c>) — not the runtime — where these properties don't exist, so
/// the value was silently dropped. A Rectangle with <c>IsFilled=true</c> saved to a .gumx and
/// reloaded therefore rendered with no fill.
/// </remarks>
public class DataDrivenShapePropertyTests : BaseTestClass
{
    [Fact]
    public void SetProperty_IsFilled_OnRectangleRuntime_AppliesValue()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("IsFilled", true);

        sut.IsFilled.ShouldBeTrue();
    }

    [Fact]
    public void SetProperty_FillRedGreenBlue_OnRectangleRuntime_AppliesValues()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("FillRed", 224);
        sut.SetProperty("FillGreen", 165);
        sut.SetProperty("FillBlue", 60);

        sut.FillRed.ShouldBe(224);
        sut.FillGreen.ShouldBe(165);
        sut.FillBlue.ShouldBe(60);
    }

    [Fact]
    public void SetProperty_StrokeWidth_OnRectangleRuntime_AppliesValue()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("StrokeWidth", 0f);

        sut.StrokeWidth.ShouldBe(0f);
    }

    [Fact]
    public void SetProperty_IsFilled_OnCircleRuntime_AppliesValue()
    {
        CircleRuntime sut = new();

        sut.SetProperty("IsFilled", true);

        sut.IsFilled.ShouldBeTrue();
    }

    [Fact]
    public void SetProperty_FillRedGreenBlue_OnCircleRuntime_AppliesValues()
    {
        CircleRuntime sut = new();

        sut.SetProperty("FillRed", 12);
        sut.SetProperty("FillGreen", 34);
        sut.SetProperty("FillBlue", 56);

        sut.FillRed.ShouldBe(12);
        sut.FillGreen.ShouldBe(34);
        sut.FillBlue.ShouldBe(56);
    }
}
