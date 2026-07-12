using Gum.GueDeriving;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// Reproduces the data-driven property-application path that loading a .gumx project uses
/// (<c>ApplyState</c> → <c>SetProperty(string, object)</c>), for Container/InvisibleRenderable
/// properties dispatched through <c>CustomSetPropertyOnRenderable.TrySetPropertyOnContainer</c>.
/// These pin that the explicit dispatch cases produce the same result as the MonoGame/KNI/FNA
/// copy of the same method (issue #3615 Container/InvisibleRenderable convergence).
/// </summary>
public class DataDrivenContainerPropertyTests : BaseTestClass
{
    [Fact]
    public void SetProperty_IsRenderTarget_OnContainer_AppliesValue()
    {
        ContainerRuntime sut = new();

        sut.SetProperty("IsRenderTarget", true);

        sut.IsRenderTarget.ShouldBeTrue();
    }

    [Fact]
    public void SetProperty_Alpha_OnContainer_WithIntValue_AppliesValue()
    {
        ContainerRuntime sut = new();

        sut.SetProperty("Alpha", 128);

        sut.Alpha.ShouldBe(128);
    }

    [Fact]
    public void SetProperty_Alpha_OnContainer_WithFloatValue_TruncatesToInt()
    {
        ContainerRuntime sut = new();

        sut.SetProperty("Alpha", 128.9f);

        sut.Alpha.ShouldBe(128);
    }

    [Fact]
    public void SetProperty_Alpha_OnContainer_WithUnsupportedValueType_DefaultsTo255()
    {
        ContainerRuntime sut = new();
        sut.Alpha = 10;

        sut.SetProperty("Alpha", "not a number");

        sut.Alpha.ShouldBe(255);
    }
}
