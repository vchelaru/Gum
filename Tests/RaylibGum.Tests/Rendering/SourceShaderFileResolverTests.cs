using System;
using System.Collections.Generic;
using System.IO;
using Gum.GueDeriving;
using Gum.Wireframe;
using RaylibGum.Renderables;
using Shouldly;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #7053: what assigning <c>SourceShaderFile</c> does when the shader cannot load. Raylib ships no shader
/// loader, so without the Gum.raylib.ShadowDusk package (or a resolver the game registers) an <c>.fx</c> or
/// <c>.slang</c> file is reported like a missing file (<see cref="GraphicalUiElement.MissingFileBehavior"/>).
/// The twin of <c>MonoGameGum.Tests.Runtimes.SourceShaderFileResolverTests</c>.
/// </summary>
public class SourceShaderFileResolverTests : BaseTestClass
{
    private readonly Func<string, object?>? _previousResolver = CustomSetPropertyOnRenderable.RenderTargetEffectResolver;
    private readonly MissingFileBehavior _previousBehavior = GraphicalUiElement.MissingFileBehavior;
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
        GraphicalUiElement.MissingFileBehavior = _previousBehavior;
        base.Dispose();
    }

    [Theory]
    [InlineData("resources/Custom.fs")]
    [InlineData("")]
    public void SourceShaderFile_ReportsNothing_WhenNoResolverAndNothingNeedsShadowDusk(string file)
    {
        ContainerRuntime container = new();

        container.SourceShaderFile = file;

        _reported.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("resources/Outline.fx")]
    [InlineData("resources/Outline.slang")]
    public void SourceShaderFile_ReportsError_WhenNoResolverAndShadowDuskExtensionAndMissingFileBehaviorIsConsumeSilently(string file)
    {
        ContainerRuntime container = new();

        container.SourceShaderFile = file;

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain(file);
        _reported[0].ShouldContain("no shader resolver is registered");
        _reported[0].ShouldContain("Gum.raylib.ShadowDusk");
    }

    [Fact]
    public void SourceShaderFile_Throws_WhenNoResolverAndShadowDuskExtensionAndMissingFileBehaviorIsThrow()
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
        ContainerRuntime container = new();

        FileNotFoundException exception = Should.Throw<FileNotFoundException>(
            () => container.SourceShaderFile = "resources/Outline.fx");

        exception.Message.ShouldContain("Gum.raylib.ShadowDusk");
    }

    [Fact]
    public void SourceShaderFile_ReportsFailure_WhenResolverReturnsNull()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => null;
        ContainerRuntime container = new();

        container.SourceShaderFile = "resources/Outline.fx";

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain("Check if the file exists");
        _reported[0].ShouldContain("Gum.raylib.ShadowDusk");
    }

    [Fact]
    public void SourceShaderFile_Throws_WhenResolverThrowsAndMissingFileBehaviorIsThrow()
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => throw new InvalidOperationException("compile failed");
        ContainerRuntime container = new();

        FileNotFoundException exception = Should.Throw<FileNotFoundException>(
            () => container.SourceShaderFile = "resources/Outline.fx");

        exception.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public void SourceShaderFile_MessageSaysToCallUseShadowDuskBeforeCreatingElements_WhenNoResolver()
    {
        ContainerRuntime container = new();

        container.SourceShaderFile = "resources/Outline.fx";

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain("before any element or screen");
        _reported[0].ShouldContain("does not retry");
    }

    [Fact]
    public void SourceShaderFile_MessageNamesTheElementTag_WhenNoResolver()
    {
        ContainerRuntime container = new();
        container.Tag = "MyShadedContainer";

        container.SourceShaderFile = "resources/Outline.fx";

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain(" in MyShadedContainer");
    }
}
