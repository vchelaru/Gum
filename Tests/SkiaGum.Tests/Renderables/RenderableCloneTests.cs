using Shouldly;
using SkiaGum.Renderables;
using System;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Skia-side parity for the SelectionInstance clone contract. Sprite and Text already
/// implement <see cref="ICloneable"/>; NineSlice did not, even though it's a stub
/// renderable today — adding it preempts the same NRE class
/// (<c>TextBoxBase.RefreshTemplateFromSelectionInstance</c> silently failing to assign
/// <c>selectionTemplate</c>) the moment a Skia consumer wires NineSlice into anything
/// that gets templated.
/// </summary>
public class RenderableCloneTests
{
    [Fact]
    public void NineSlice_Clone_ReturnsNewInstance()
    {
        var original = new NineSlice();

        var clone = ((ICloneable)original).Clone();

        clone.ShouldNotBeSameAs(original);
        clone.ShouldBeOfType<NineSlice>();
    }

    [Fact]
    public void NineSlice_ImplementsICloneable()
    {
        new NineSlice().ShouldBeAssignableTo<ICloneable>();
    }

    [Fact]
    public void Sprite_ImplementsICloneable()
    {
        new Sprite().ShouldBeAssignableTo<ICloneable>();
    }

    [Fact]
    public void Text_ImplementsICloneable()
    {
        new SkiaGum.Text().ShouldBeAssignableTo<ICloneable>();
    }
}
