using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// The Skia dispatcher's twin of the XNA-like/raylib routing (#7010 slice 3): a variable named like
/// <c>Outline_Thickness</c> reaches the container's shader parameter in any order with
/// <c>SourceShaderFile</c> and the legacy <c>ShaderParameters</c> text, and beats the text. The full matrix
/// is in <c>MonoGameGum.Tests.Runtimes.ContainerShaderVariablesTests</c>; both call the same
/// <see cref="ShaderParameterVariables"/>.
/// </summary>
public class ContainerShaderVariablesTests : IDisposable
{
    private const string OutlineFile = "Shaders/Outline.sksl";

    private readonly Func<string, SKRuntimeEffect?>? _previousResolver;

    public ContainerShaderVariablesTests()
    {
        Gum.Wireframe.GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        _previousResolver = CustomSetPropertyOnRenderable.RenderTargetEffectResolver;
        // No resolver: SourceShaderFile records the file but renders unshaded, so no shader file is needed.
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
    }

    public void Dispose()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _previousResolver;
    }

    private static ShaderParameterValue Get(ContainerRuntime container, string name)
    {
        ShaderParameterCollection? parameters = ((IRenderTargetRenderable)container.RenderableComponent).ShaderParameters;
        parameters.ShouldNotBeNull();
        parameters!.TryGetValue(name, out ShaderParameterValue value).ShouldBeTrue("expected parameter " + name);
        return value;
    }

    public static IEnumerable<object[]> Orders()
    {
        yield return new object[] { "file,variable,text" };
        yield return new object[] { "variable,text,file" };
        yield return new object[] { "text,file,variable" };
        yield return new object[] { "text,variable,file" };
    }

    [Theory]
    [MemberData(nameof(Orders))]
    public void SetProperty_VariableTextAndFile_AnyOrderGivesTheSameParameters(string order)
    {
        ContainerRuntime sut = new();

        foreach (string step in order.Split(','))
        {
            switch (step)
            {
                case "file": sut.SetProperty("SourceShaderFile", OutlineFile); break;
                case "variable": sut.SetProperty("Outline_Thickness", 7f); break;
                case "text": sut.SetProperty("ShaderParameters", "Thickness=2.0; Glow=true"); break;
            }
        }

        Get(sut, "Thickness").X.ShouldBe(7f);
        Get(sut, "Glow").X.ShouldBe(1f);
    }

    [Fact]
    public void SetProperty_ComponentVariables_AssembleOneVector()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("Outline_Tint__X", 0.1f);
        sut.SetProperty("SourceShaderFile", OutlineFile);
        sut.SetProperty("Outline_Tint__Y", 0.2f);
        sut.SetProperty("Outline_Tint__Z", 0.3f);
        sut.SetProperty("Outline_Tint__W", 0.4f);

        ShaderParameterValue tint = Get(sut, "Tint");
        tint.Count.ShouldBe(4);
        tint.X.ShouldBe(0.1f);
        tint.W.ShouldBe(0.4f);
    }

    [Fact]
    public void SetProperty_VariableOfAnotherShadersStem_IsInert()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", OutlineFile);

        sut.SetProperty("Glow_Thickness", 9f);

        ((IRenderTargetRenderable)sut.RenderableComponent).ShaderParameters.ShouldBeNull();
    }

    [Fact]
    public void SetProperty_StemMatchIgnoresCase()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", "Shaders/outline.SKSL");

        sut.SetProperty("Outline_Thickness", 6f);

        Get(sut, "Thickness").X.ShouldBe(6f);
    }
}
