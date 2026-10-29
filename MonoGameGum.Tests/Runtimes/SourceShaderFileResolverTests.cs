using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// #7053: what assigning <c>SourceShaderFile</c> does when the shader cannot load. A shader that cannot
/// load is reported like a missing file (<see cref="GraphicalUiElement.MissingFileBehavior"/>), including
/// when no resolver is registered because the ShadowDusk package is missing.
/// </summary>
public class SourceShaderFileResolverTests : BaseTestClass
{
    private readonly Func<string, object?>? _previousResolver = CustomSetPropertyOnRenderable.RenderTargetEffectResolver;
    private readonly List<string> _reported = new List<string>();
    private readonly Action<string> _handler;

    public SourceShaderFileResolverTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
        _handler = message => _reported.Add(message);
        CustomSetPropertyOnRenderable.PropertyAssignmentError += _handler;
    }

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError -= _handler;
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _previousResolver;
        base.Dispose();
    }

    private static ContainerRuntime CreateContainer()
    {
        ContainerRuntime container = new ContainerRuntime();
        container.IsRenderTarget = true;
        return container;
    }

    private static object? GetEffect(ContainerRuntime container) =>
        ((IRenderTargetRenderable)container.RenderableComponent!).RenderTargetEffect;

    [Fact]
    public void SourceShaderFile_ReportsNothing_WhenNoResolverAndNoFile()
    {
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = "";

        _reported.ShouldBeEmpty();
    }

    [Fact]
    public void SourceShaderFile_ReportsNothing_WhenNoResolverAndExtensionIsNotShadowDusk()
    {
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = "Shaders/Custom.fs";

        _reported.ShouldBeEmpty();
        GetEffect(container).ShouldBeNull();
    }

    [Fact]
    public void SourceShaderFile_Throws_WhenNoResolverAndShadowDuskExtensionAndMissingFileBehaviorIsThrow()
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
        ContainerRuntime container = CreateContainer();

        FileNotFoundException exception = Should.Throw<FileNotFoundException>(
            () => container.SourceShaderFile = "Shaders/Outline.fx");

        exception.Message.ShouldContain("Shaders/Outline.fx");
        exception.Message.ShouldContain("no shader resolver is registered");
        exception.Message.ShouldContain("Gum.MonoGame.ShadowDusk");
        exception.Message.ShouldContain("UseShadowDusk()");
    }

    [Theory]
    [InlineData("Shaders/Outline.fx")]
    [InlineData("Shaders/Outline.slang")]
    [InlineData("Shaders/OUTLINE.FX")]
    public void SourceShaderFile_ReportsError_WhenNoResolverAndShadowDuskExtensionAndMissingFileBehaviorIsConsumeSilently(string file)
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = file;

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain(file);
        _reported[0].ShouldContain("no shader resolver is registered");
        _reported[0].ShouldContain("Gum.MonoGame.ShadowDusk");
        GetEffect(container).ShouldBeNull();
    }

    [Fact]
    public void SourceShaderFile_StillTracksTheFileForShaderVariables_WhenNoResolver()
    {
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = "Shaders/Outline.fx";
        container.SetProperty("Outline_Thickness", 3f);

        ((IRenderTargetRenderable)container.RenderableComponent!).ShaderParameters.ShouldNotBeNull();
    }

    [Fact]
    public void SourceShaderFile_ReportsFailure_WhenResolverReturnsNull()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => null;
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = "Shaders/Outline.fx";

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain("Outline.fx");
        _reported[0].ShouldContain("Check if the file exists");
        _reported[0].ShouldContain("Gum.MonoGame.ShadowDusk");
    }

    [Fact]
    public void SourceShaderFile_Throws_WhenResolverReturnsNullAndMissingFileBehaviorIsThrow()
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => null;
        ContainerRuntime container = CreateContainer();

        Should.Throw<FileNotFoundException>(() => container.SourceShaderFile = "Shaders/Outline.fx");
    }

    [Fact]
    public void SourceShaderFile_ReportsFailureWithTheException_WhenResolverThrows()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => throw new InvalidOperationException("compile failed");
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = "Shaders/Outline.fx";

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain("compile failed");
    }

    [Fact]
    public void SourceShaderFile_Throws_WhenResolverThrowsAndMissingFileBehaviorIsThrow()
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => throw new InvalidOperationException("compile failed");
        ContainerRuntime container = CreateContainer();

        FileNotFoundException exception = Should.Throw<FileNotFoundException>(
            () => container.SourceShaderFile = "Shaders/Outline.fx");

        exception.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public void SourceShaderFile_AssignsTheEffect_WhenResolverReturnsOne()
    {
        object effect = new object();
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => effect;
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = "Shaders/Outline.fx";

        GetEffect(container).ShouldBeSameAs(effect);
        _reported.ShouldBeEmpty();
    }

    [Fact]
    public void SourceShaderFile_MessageSaysToCallUseShadowDuskBeforeCreatingElements_WhenNoResolver()
    {
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = "Shaders/Outline.fx";

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain("before any element or screen");
        _reported[0].ShouldContain("does not retry");
    }

    [Fact]
    public void SourceShaderFile_MessageNamesTheElementTag_WhenNoResolver()
    {
        ContainerRuntime container = CreateContainer();
        container.Tag = "MyShadedContainer";

        container.SourceShaderFile = "Shaders/Outline.fx";

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain(" in MyShadedContainer");
    }
}
