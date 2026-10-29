using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// #7053: what assigning <c>SourceShaderFile</c> does when the shader cannot load. SkiaGum has a default
/// resolver that compiles <c>.sksl</c>, and setting the resolver to null is the documented opt-out (#3998),
/// so only a registered resolver that does not handle the file is a loud failure here (it is reported like a
/// missing file, <see cref="GraphicalUiElement.MissingFileBehavior"/>, whatever the extension).
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

    // The documented opt-out (#3998): null ignores SourceShaderFile, with every MissingFileBehavior.
    [Theory]
    [InlineData("Shaders/Outline.sksl", false)]
    [InlineData("Shaders/Outline.fx", false)]
    [InlineData("Shaders/Outline.fx", true)]
    [InlineData("", false)]
    public void SourceShaderFile_ReportsNothing_WhenResolverIsSetToNull(string file, bool throwBehavior)
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
        GraphicalUiElement.MissingFileBehavior = throwBehavior ? MissingFileBehavior.ThrowException : MissingFileBehavior.ConsumeSilently;
        ContainerRuntime container = new();

        container.SetProperty("SourceShaderFile", file);

        _reported.ShouldBeEmpty();
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

    [Theory]
    [InlineData("Shaders/Outline.sksl")]
    [InlineData("Shaders/Outline.fx")]
    [InlineData("Shaders/Custom.glsl")]
    public void SourceShaderFile_ReportsFailureNamingThePackage_WhenResolverReturnsNull(string file)
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => null;
        ContainerRuntime container = new();

        container.SetProperty("SourceShaderFile", file);

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain("Check if the file exists");
        _reported[0].ShouldContain("did not handle this file");
        _reported[0].ShouldContain("Gum.SkiaSharp.ShadowDusk");
        _reported[0].ShouldContain("before creating any element");
        _reported[0].ShouldContain("does not retry");
    }

    [Fact]
    public void SourceShaderFile_MessageNamesTheElementTag_WhenResolverReturnsNull()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => null;
        ContainerRuntime container = new();
        container.Tag = "MyShadedContainer";

        container.SetProperty("SourceShaderFile", "Shaders/Outline.sksl");

        _reported.Count.ShouldBe(1);
        _reported[0].ShouldContain(" in MyShadedContainer");
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
        _reported[0].ShouldNotContain("did not handle this file");
    }
}
