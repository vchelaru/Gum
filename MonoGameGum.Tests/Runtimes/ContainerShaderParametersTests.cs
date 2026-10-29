using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class ContainerShaderParametersTests : BaseTestClass
{
    public ContainerShaderParametersTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    private static ShaderParameterCollection? GetParameters(ContainerRuntime container) =>
        ((IRenderTargetRenderable)container.RenderableComponent).ShaderParameters;

    [Fact]
    public void SetProperty_ShaderParameters_ParsesTheTextIntoTheContainersParameters()
    {
        ContainerRuntime sut = new();

        sut.SetProperty("ShaderParameters", "Glow=true; Thickness=3.0");

        ShaderParameterCollection? parameters = GetParameters(sut);
        parameters.ShouldNotBeNull();
        parameters!.Count.ShouldBe(2);
        parameters.TryGetValue("Thickness", out ShaderParameterValue thickness).ShouldBeTrue();
        thickness.Kind.ShouldBe(ShaderParameterKind.Float);
        thickness.X.ShouldBe(3f);
    }

    [Fact]
    public void SetProperty_ShaderParameters_ReplacesValuesSetEarlier()
    {
        ContainerRuntime sut = new();
        sut.SetShaderParameter("Old", 1f);

        sut.SetProperty("ShaderParameters", "New=2.0");

        ShaderParameterCollection? parameters = GetParameters(sut);
        parameters.ShouldNotBeNull();
        parameters!.TryGetValue("Old", out _).ShouldBeFalse();
        parameters.TryGetValue("New", out _).ShouldBeTrue();
    }

    [Fact]
    public void SetProperty_ShaderParameters_EmptyTextRemovesEveryParameter()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("ShaderParameters", "Thickness=3.0");

        sut.SetProperty("ShaderParameters", "");

        (GetParameters(sut)?.Count ?? 0).ShouldBe(0);
    }

    [Fact]
    public void SetProperty_ShaderParameters_KeepsTheEntriesItCanRead()
    {
        ContainerRuntime sut = new();

        sut.SetProperty("ShaderParameters", "Thickness=abc; Glow=true");

        ShaderParameterCollection? parameters = GetParameters(sut);
        parameters.ShouldNotBeNull();
        parameters!.Count.ShouldBe(1);
        parameters.TryGetValue("Glow", out _).ShouldBeTrue();
    }
}
