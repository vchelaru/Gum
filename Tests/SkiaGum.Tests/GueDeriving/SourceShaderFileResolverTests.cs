using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// #7053: what assigning <c>SourceShaderFile</c> does when the shader cannot load. SkiaGum compiles
/// <c>.sksl</c> itself, so only <c>.fx</c> and <c>.slang</c> need the Gum.SkiaSharp.ShadowDusk package;
/// without a resolver they are reported like a missing file (<see cref="GraphicalUiElement.MissingFileBehavior"/>).
/// The twin of <c>MonoGameGum.Tests.Runtimes.SourceShaderFileResolverTests</c>.
/// </summary>
public class SourceShaderFileResolverTests : IDisposable
{
    private readonly Func<string, SKRuntimeEffect?>? _previousResolver;
    private readonly MissingFileBehavior _previousBehavior;
    private readonly List<string> _reported = new List<string>();
    private readonly Action<string> _handler;

    public SourceShaderFileResolverTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        _previousResolver = CustomSetPropertyOnRenderable.RenderTargetEffectResolver;
        _previousBehavior = GraphicalUiElement.MissingFileBehavior;
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
        _handler = message => _reported.Add(message);
        CustomSetPropertyOnRenderable.PropertyAssignmentError += _handler;
    }

    public void Dispose()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError -= _handler;
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _previousResolver;
        GraphicalUiElement.MissingFileBehavior = _previousBehavior;
    }

    [Fact]
    public void SourceShaderFile_ReportsNothing_WhenNoResolverAndExtensionIsSksl()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
        ContainerRuntime container = new();

        container.SetProperty("SourceShaderFile", "Shaders/Outline.sksl");

        _reported.ShouldBeEmpty();
    }

    [Fact]
    public void SourceShaderFile_ReportsNothing_WhenNoResolverAndNoFile()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
        ContainerRuntime container = new();

        container.SetProperty("SourceShaderFile", "");

        _reported.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Shaders/Outline.fx")]
    [InlineData("Shaders/Outline.slang")]
    public void SourceShaderFile_ReportsError_WhenNoResolverAndShadowDuskExtensionAndMissingFileBehaviorIsConsumeSilently(string file)
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
        ContainerRuntime container = new();

        container.SetProperty("SourceShaderFile", file);

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain(file);
        _reported[0].ShouldContain("no shader resolver is registered");
        _reported[0].ShouldContain("Gum.SkiaSharp.ShadowDusk");
    }

    [Fact]
    public void SourceShaderFile_Throws_WhenNoResolverAndShadowDuskExtensionAndMissingFileBehaviorIsThrow()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
        ContainerRuntime container = new();

        FileNotFoundException exception = Should.Throw<FileNotFoundException>(
            () => container.SetProperty("SourceShaderFile", "Shaders/Outline.fx"));

        exception.Message.ShouldContain("Gum.SkiaSharp.ShadowDusk");
    }

    // The default resolver compiles .sksl only; its exception already names the package.
    [Fact]
    public void SourceShaderFile_ReportsFailureNamingThePackage_WhenDefaultResolverMeetsFx()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = SkiaGum.Content.SkSlRuntimeEffectLoader.Load;
        ContainerRuntime container = new();

        container.SetProperty("SourceShaderFile", "Shaders/Outline.fx");

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain("Gum.SkiaSharp.ShadowDusk");
    }

    [Fact]
    public void SourceShaderFile_Throws_WhenDefaultResolverMeetsFxAndMissingFileBehaviorIsThrow()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = SkiaGum.Content.SkSlRuntimeEffectLoader.Load;
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
        ContainerRuntime container = new();

        FileNotFoundException exception = Should.Throw<FileNotFoundException>(
            () => container.SetProperty("SourceShaderFile", "Shaders/Outline.fx"));

        exception.InnerException.ShouldBeOfType<NotSupportedException>();
    }

    [Fact]
    public void SourceShaderFile_ReportsFailure_WhenResolverReturnsNull()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => null;
        ContainerRuntime container = new();

        container.SetProperty("SourceShaderFile", "Shaders/Outline.sksl");

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain("Check if the file exists");
    }

    // A resolver that returns null for .fx declined it; the message names the package that handles it.
    [Fact]
    public void SourceShaderFile_ReportsFailureNamingThePackage_WhenResolverReturnsNullForFx()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => null;
        ContainerRuntime container = new();

        container.SetProperty("SourceShaderFile", "Shaders/Outline.fx");

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain("Gum.SkiaSharp.ShadowDusk");
    }

    [Fact]
    public void SourceShaderFile_Throws_WhenResolverReturnsNullAndMissingFileBehaviorIsThrow()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => null;
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
        ContainerRuntime container = new();

        Should.Throw<FileNotFoundException>(() => container.SetProperty("SourceShaderFile", "Shaders/Outline.sksl"));
    }

    [Fact]
    public void SourceShaderFile_ReportsFailureWithTheException_WhenResolverThrows()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => throw new InvalidOperationException("compile failed");
        ContainerRuntime container = new();

        container.SetProperty("SourceShaderFile", "Shaders/Outline.sksl");

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain("compile failed");
    }
}
