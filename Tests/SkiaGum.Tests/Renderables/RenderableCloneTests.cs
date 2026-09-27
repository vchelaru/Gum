using RenderingLibrary.Graphics;
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
    public static TheoryData<string> CloneableRenderableNames => new()
    {
        nameof(Circle),
        nameof(LineRectangle),
        nameof(NineSlice),
        nameof(RoundedRectangle),
        nameof(Sprite),
        nameof(SkiaGum.Text),
    };

    private static IRenderableIpso CreateRenderable(string name) => name switch
    {
        nameof(Circle) => new Circle(),
        nameof(LineRectangle) => new LineRectangle(),
        nameof(NineSlice) => new NineSlice(),
        nameof(RoundedRectangle) => new RoundedRectangle(),
        nameof(Sprite) => new Sprite(),
        nameof(SkiaGum.Text) => new SkiaGum.Text(),
        _ => throw new ArgumentException(name),
    };

    [Theory]
    [MemberData(nameof(CloneableRenderableNames))]
    public void Clone_ShouldNotShareParentOrChildren(string renderableName)
    {
        IRenderableIpso parent = new LineRectangle();
        IRenderableIpso source = CreateRenderable(renderableName);
        IRenderableIpso sourceChild = new LineRectangle();
        source.Parent = parent;
        sourceChild.Parent = source;

        IRenderableIpso clone = (IRenderableIpso)((ICloneable)source).Clone();
        IRenderableIpso cloneChild = new LineRectangle();
        cloneChild.Parent = clone;

        clone.Parent.ShouldBeNull();
        parent.Children.ShouldNotContain(clone);
        clone.Children.ShouldNotBeSameAs(source.Children);
        clone.Children.ShouldNotContain(sourceChild);
        source.Children.ShouldNotContain(cloneChild);
    }

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
