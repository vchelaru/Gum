using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class ContainerRuntimeTests
{
    [Fact]
    public void Alpha_ShouldDefaultTo255()
    {
        ContainerRuntime sut = new();
        sut.Alpha.ShouldBe(255);
    }

    [Fact]
    public void Alpha_ShouldRoundTrip()
    {
        ContainerRuntime sut = new();
        sut.Alpha = 128;
        sut.Alpha.ShouldBe(128);
    }

    [Fact]
    public void Blend_ShouldBeNormal_WhenNotFullyInstantiated()
    {
        ContainerRuntime sut = new(fullInstantiation: false);

        sut.Blend.ShouldBe(Gum.RenderingLibrary.Blend.Normal);
        sut.BlendState.ShouldBe(Microsoft.Xna.Framework.Graphics.BlendState.NonPremultiplied);
    }

    [Fact]
    public void ContainedRenderable_ShouldBeInvisibleRenderable()
    {
        ContainerRuntime sut = new();
        sut.RenderableComponent.ShouldBeOfType<InvisibleRenderable>();
    }

    [Fact]
    public void ExposeChildrenEvents_ShouldBeTrue_ByDefault()
    {
        ContainerRuntime sut = new();
        sut.ExposeChildrenEvents.ShouldBeTrue();
    }

    [Fact]
    public void HasEvents_ShouldBeTrue_ByDefault()
    {
        ContainerRuntime sut = new();
        sut.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void Height_ShouldBe150_ByDefault()
    {
        ContainerRuntime sut = new();
        sut.Height.ShouldBe(150);
    }

    [Fact]
    public void IsRenderTarget_ShouldDefaultToFalse()
    {
        ContainerRuntime sut = new();
        sut.IsRenderTarget.ShouldBeFalse();
    }

    [Fact]
    public void IsRenderTarget_ShouldRoundTrip()
    {
        ContainerRuntime sut = new();
        sut.IsRenderTarget = true;
        sut.IsRenderTarget.ShouldBeTrue();
    }

    [Fact]
    public void RenderTargetEffect_ShouldDefaultToNull()
    {
        ContainerRuntime sut = new();
        sut.RenderTargetEffect.ShouldBeNull();
    }

    private static ShaderParameterValue GetShaderParameter(ContainerRuntime container, string name)
    {
        IRenderTargetRenderable owner = (IRenderTargetRenderable)container.RenderableComponent;
        owner.ShaderParameters.ShouldNotBeNull();
        owner.ShaderParameters!.TryGetValue(name, out ShaderParameterValue value).ShouldBeTrue();
        return value;
    }

    [Fact]
    public void SetShaderParameter_Overloads_StoreKindCountAndComponents()
    {
        ContainerRuntime sut = new();

        sut.SetShaderParameter("F", 1.5f);
        sut.SetShaderParameter("I", 3);
        sut.SetShaderParameter("B", true);
        sut.SetShaderParameter("V2", 1f, 2f);
        sut.SetShaderParameter("V3", 1f, 2f, 3f);
        sut.SetShaderParameter("V4", 1f, 2f, 3f, 4f);
        sut.SetShaderParameter("N3", new System.Numerics.Vector3(5f, 6f, 7f));

        GetShaderParameter(sut, "F").Kind.ShouldBe(ShaderParameterKind.Float);
        GetShaderParameter(sut, "F").X.ShouldBe(1.5f);
        GetShaderParameter(sut, "I").Kind.ShouldBe(ShaderParameterKind.Int);
        GetShaderParameter(sut, "I").X.ShouldBe(3f);
        GetShaderParameter(sut, "B").Kind.ShouldBe(ShaderParameterKind.Bool);
        GetShaderParameter(sut, "B").X.ShouldBe(1f);
        GetShaderParameter(sut, "V2").Count.ShouldBe(2);
        GetShaderParameter(sut, "V3").Count.ShouldBe(3);
        GetShaderParameter(sut, "V4").Count.ShouldBe(4);
        GetShaderParameter(sut, "V4").W.ShouldBe(4f);
        GetShaderParameter(sut, "N3").Count.ShouldBe(3);
        GetShaderParameter(sut, "N3").Z.ShouldBe(7f);
    }

    [Fact]
    public void SetShaderParameter_ValuesAreIndependentPerContainer()
    {
        ContainerRuntime first = new();
        ContainerRuntime second = new();

        first.SetShaderParameter("Strength", 1f);
        second.SetShaderParameter("Strength", 2f);

        GetShaderParameter(first, "Strength").X.ShouldBe(1f);
        GetShaderParameter(second, "Strength").X.ShouldBe(2f);
    }

    [Fact]
    public void ClearShaderParameters_RemovesEverySetValue()
    {
        ContainerRuntime sut = new();
        sut.SetShaderParameter("Strength", 1f);

        sut.ClearShaderParameters();

        IRenderTargetRenderable owner = (IRenderTargetRenderable)sut.RenderableComponent;
        (owner.ShaderParameters?.Count ?? 0).ShouldBe(0);
    }

    [Fact]
    public void Visible_ShouldBeTrue_ByDefault()
    {
        ContainerRuntime sut = new();
        sut.Visible.ShouldBeTrue();
    }

    [Fact]
    public void Width_ShouldBe150_ByDefault()
    {
        ContainerRuntime sut = new();
        sut.Width.ShouldBe(150);
    }
}
