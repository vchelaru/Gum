using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.IO;
using System.Text;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// #7104: <c>SourceShaderFile</c> hands a resolver a <see cref="ShaderSource"/> (logical name plus a way to
/// open the bytes) so a shader from a stream or bundle loads, and the original path-only resolver keeps working.
/// </summary>
public class ShaderSourceResolverTests : BaseTestClass
{
    private readonly Func<string, object?>? _previousResolver = CustomSetPropertyOnRenderable.RenderTargetEffectResolver;
    private readonly Func<string, Stream>? _previousHook = FileManager.CustomGetStreamFromFile;

    public ShaderSourceResolverTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
    }

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _previousResolver;
        FileManager.CustomGetStreamFromFile = _previousHook;
        base.Dispose();
    }

    private static ContainerRuntime CreateContainer() => new ContainerRuntime { IsRenderTarget = true };

    private static object? GetEffect(ContainerRuntime container) =>
        ((IRenderTargetRenderable)container.RenderableComponent!).RenderTargetEffect;

    [Fact]
    public void SourceShaderFile_HandsTheSourceResolverTheBytes_WhateverTheExtension()
    {
        string path = Path.Combine(Path.GetTempPath(), "Shader7104_" + Guid.NewGuid().ToString("N") + ".custom");
        File.WriteAllText(path, "shader text");
        try
        {
            string? seenText = null;
            string? seenName = null;
            object effect = new object();
            CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver = source =>
            {
                seenName = source.Name;
                seenText = source.ReadAllText();
                return effect;
            };
            ContainerRuntime container = CreateContainer();

            container.SourceShaderFile = path;

            seenText.ShouldBe("shader text");
            seenName.ShouldNotBeNull().ShouldEndWith(Path.GetFileName(path));
            GetEffect(container).ShouldBeSameAs(effect);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SourceShaderFile_ReadsAShaderTheFileHookServes_ThatIsNotOnDisk()
    {
        FileManager.CustomGetStreamFromFile = path =>
            path.EndsWith("bundled_shader", StringComparison.OrdinalIgnoreCase)
                ? new MemoryStream(Encoding.UTF8.GetBytes("from the bundle"))
                : throw new FileNotFoundException(path);
        string? seenText = null;
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver = source =>
        {
            seenText = source.ReadAllText();
            return new object();
        };
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = "Shaders/bundled_shader";

        seenText.ShouldBe("from the bundle");
    }

    [Fact]
    public void PathResolver_StillReceivesThePath()
    {
        string? seen = null;
        object effect = new object();
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path =>
        {
            seen = path;
            return effect;
        };
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = "Shaders/Outline.fx";

        seen.ShouldNotBeNull().ShouldEndWith("Outline.fx");
        GetEffect(container).ShouldBeSameAs(effect);
    }

    [Fact]
    public void SourceResolver_ReadsBackThroughThePathProperty_AsAnAdapter()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver = source => source.Name;

        object? result = CustomSetPropertyOnRenderable.RenderTargetEffectResolver!("some/path.fx");

        result.ShouldBe("some/path.fx");
    }

    [Fact]
    public void PathResolver_ReadsBackThroughTheSourceProperty_AsAnAdapter()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path => path.ToUpperInvariant();

        object? result = CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver!(
            new ShaderSource("some/path.fx", () => new MemoryStream()));

        result.ShouldBe("SOME/PATH.FX");
    }

    [Fact]
    public void AssigningNullToEitherProperty_ClearsTheResolver()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver = source => new object();

        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;

        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver.ShouldBeNull();
    }

    [Fact]
    public void PathResolverAssignedAfterSourceResolver_Wins()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver = _ => "source";
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => "path";
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = "Shaders/Outline.fx";

        GetEffect(container).ShouldBe("path");
    }

    [Fact]
    public void SourceResolverAssignedAfterPathResolver_Wins()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => "path";
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver = _ => "source";
        ContainerRuntime container = CreateContainer();

        container.SourceShaderFile = "Shaders/Outline.fx";

        GetEffect(container).ShouldBe("source");
    }
}
