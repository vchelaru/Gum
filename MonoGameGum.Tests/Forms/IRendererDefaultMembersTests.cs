using Moq;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// What an <see cref="IRenderer"/> written before layers were part of the interface gets: the default
/// interface members. Moq's CallBase runs them, as a third-party implementer would see them.
/// </summary>
public class IRendererDefaultMembersTests
{
    private static IRenderer CreateMinimalRenderer()
    {
        return new Mock<IRenderer> { CallBase = true }.Object;
    }

    [Fact]
    public void AddLayer_ByDefault_ShouldThrowNotSupported()
    {
        IRenderer renderer = CreateMinimalRenderer();

        Should.Throw<NotSupportedException>(() => renderer.AddLayer());
    }

    [Fact]
    public void RemoveLayer_ByDefault_ShouldThrowNotSupported()
    {
        IRenderer renderer = CreateMinimalRenderer();

        Should.Throw<NotSupportedException>(() => renderer.RemoveLayer(new Layer()));
    }

    [Fact]
    public void AddLayerAndRemoveLayer_OnTheMonoGameRenderer_ChangeTheLayers()
    {
        IRenderer renderer = new Renderer();

        Layer layer = renderer.AddLayer();

        renderer.Layers.ShouldContain(layer);
        renderer.RemoveLayer(layer);
        renderer.Layers.ShouldNotContain(layer);
    }
}
