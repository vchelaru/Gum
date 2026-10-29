using Gum.GueDeriving;
using Gum.Wireframe;
using RaylibGum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #7053: what assigning <c>SourceShaderFile</c> does when no resolver handles the file. It is reported like
/// a missing file (<see cref="GraphicalUiElement.MissingFileBehavior"/>) whatever the extension, because Gum
/// does not know which formats a resolver (such as ShadowDusk) can load.
/// </summary>
public class SourceShaderFileResolverTests : BaseTestClass
{
    private readonly Func<string, object?>? _previousResolver = CustomSetPropertyOnRenderable.RenderTargetEffectResolver;
    private readonly List<string> _reported = new List<string>();
    private readonly Action<string> _handler;

    public SourceShaderFileResolverTests()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
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

    [Theory]
    [InlineData("Shaders/Outline.fx")]
    [InlineData("Shaders/Outline.slang")]
    [InlineData("Shaders/OUTLINE.FX")]
    [InlineData("Shaders/Custom.fs")]
    [InlineData("Shaders/Custom.glsl")]
    [InlineData("Shaders/Custom.sksl")]
    [InlineData("Shaders/NoExtension")]
    public void SourceShaderFile_ReportsError_WhenNoResolverAndMissingFileBehaviorIsConsumeSilently(string file)
    {
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = file;

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain(file);
        _reported[0].ShouldContain("no shader resolver is registered");
        _reported[0].ShouldContain("Gum.raylib.ShadowDusk");
        GetEffect(container).ShouldBeNull();
    }

    [Theory]
    [InlineData("Shaders/Outline.fx")]
    [InlineData("Shaders/Custom.fs")]
    public void SourceShaderFile_Throws_WhenNoResolverAndMissingFileBehaviorIsThrow(string file)
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
        ContainerRuntime container = CreateContainer();

        FileNotFoundException exception = Should.Throw<FileNotFoundException>(
            () => container.SourceShaderFile = file);

        exception.Message.ShouldContain(file);
        exception.Message.ShouldContain("no shader resolver is registered");
        exception.Message.ShouldContain("Gum.raylib.ShadowDusk");
        exception.Message.ShouldContain("UseShadowDusk()");
    }

    [Fact]
    public void SourceShaderFile_MessageSaysToRegisterBeforeCreatingElements_WhenNoResolver()
    {
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = "Shaders/Outline.fx";

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain("before creating any element");
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

    [Fact]
    public void SourceShaderFile_StillTracksTheFileForShaderVariables_WhenNoResolver()
    {
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = "Shaders/Outline.fx";
        container.SetProperty("Outline_Thickness", 3f);

        ((IRenderTargetRenderable)container.RenderableComponent!).ShaderParameters.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("Shaders/Outline.fx")]
    [InlineData("Shaders/Custom.fs")]
    public void SourceShaderFile_ReportsFailureNamingThePackage_WhenResolverReturnsNull(string file)
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => null;
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = file;

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain("Check if the file exists");
        _reported[0].ShouldContain("did not handle this file");
        _reported[0].ShouldContain("Gum.raylib.ShadowDusk");
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
        _reported[0].ShouldNotContain("did not handle this file");
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
}
