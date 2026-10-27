using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;

namespace SkiaGum.Tests.GueDeriving;

public class ContainerShaderParametersTests
{
    public ContainerShaderParametersTests()
    {
        Gum.Wireframe.GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
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
        thickness.X.ShouldBe(3f);
    }

    [Fact]
    public void SetProperty_ShaderParameters_EmptyTextRemovesEveryParameter()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("ShaderParameters", "Thickness=3.0");

        sut.SetProperty("ShaderParameters", "");

        (GetParameters(sut)?.Count ?? 0).ShouldBe(0);
    }
}
