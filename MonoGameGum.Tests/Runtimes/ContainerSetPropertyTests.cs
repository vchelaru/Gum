using Gum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Covers the string-property-dispatch path (SetProperty -> CustomSetPropertyOnRenderable ->
// TrySetPropertyOnContainer) for Container, mirroring
// Tests/RaylibGum.Tests/Runtimes/ContainerSetPropertyTests.cs. These properties are set at runtime
// by the state/variable system (StateSave/VariableSave applied via GraphicalUiElement.SetProperty),
// not by direct C# property assignment, so this pins that the string-path dispatch produces the
// same result as direct C# usage (e.g. ContainerRuntime.Alpha = 128).
public class ContainerSetPropertyTests : BaseTestClass
{
    [Fact]
    public void SetProperty_Alpha_ShouldForwardToContainerRuntime()
    {
        ContainerRuntime sut = new();

        sut.SetProperty(nameof(ContainerRuntime.Alpha), 128);

        sut.Alpha.ShouldBe(128);
    }

    [Fact]
    public void SetProperty_IsRenderTarget_ShouldForwardToContainerRuntime()
    {
        ContainerRuntime sut = new();

        sut.SetProperty(nameof(ContainerRuntime.IsRenderTarget), true);

        sut.IsRenderTarget.ShouldBeTrue();
    }
}
