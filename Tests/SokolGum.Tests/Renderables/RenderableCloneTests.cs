using Gum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;
using System;

namespace SokolGum.Tests.Renderables;

/// <summary>
/// Brings the Sokol renderables in line with the MonoGame contract: NineSlice, Sprite,
/// and Text must implement <see cref="ICloneable"/> so that
/// <c>TextBoxBase.RefreshTemplateFromSelectionInstance</c> can clone a SelectionInstance
/// template without falling through to the null-template path that NREs. Sprite gets
/// <see cref="ICloneable"/> for free via <see cref="InvisibleRenderable"/>; NineSlice
/// and Text need their own implementations.
/// </summary>
public class RenderableCloneTests : BaseTestClass
{
    [Fact]
    public void NineSlice_Clone_DoesNotShareChildrenWithOriginal()
    {
        var original = new NineSlice();
        original.Children.Add(new NineSlice());

        var clone = (NineSlice)((ICloneable)original).Clone();

        clone.Children.ShouldBeEmpty();
        original.Children.Count.ShouldBe(1);
    }

    [Fact]
    public void NineSlice_Clone_ResetsParent()
    {
        var parent = new NineSlice();
        var original = new NineSlice { Parent = parent };

        var clone = (NineSlice)((ICloneable)original).Clone();

        clone.Parent.ShouldBeNull();
    }

    [Fact]
    public void NineSlice_Clone_ReturnsNewInstance()
    {
        var original = new NineSlice { Width = 42, Height = 17 };

        var clone = ((ICloneable)original).Clone();

        clone.ShouldNotBeSameAs(original);
        clone.ShouldBeOfType<NineSlice>();
        ((NineSlice)clone).Width.ShouldBe(42);
        ((NineSlice)clone).Height.ShouldBe(17);
    }

    [Fact]
    public void NineSlice_ImplementsICloneable()
    {
        new NineSlice().ShouldBeAssignableTo<ICloneable>();
    }

    [Fact]
    public void Sprite_Clone_DoesNotShareChildrenWithOriginal()
    {
        var original = new Sprite();
        original.Children.Add(new Sprite());

        var clone = (InvisibleRenderable)((ICloneable)original).Clone();

        clone.Children.ShouldBeEmpty();
    }

    [Fact]
    public void Sprite_Clone_ResetsParent()
    {
        var parent = new Sprite();
        var original = new Sprite { Parent = parent };

        var clone = (InvisibleRenderable)((ICloneable)original).Clone();

        clone.Parent.ShouldBeNull();
    }

    [Fact]
    public void Sprite_ImplementsICloneable()
    {
        new Sprite().ShouldBeAssignableTo<ICloneable>();
    }

    [Fact]
    public void Text_Clone_DoesNotShareChildrenWithOriginal()
    {
        var original = new Text();
        original.Children.Add(new Text());

        var clone = (Text)((ICloneable)original).Clone();

        clone.Children.ShouldBeEmpty();
        original.Children.Count.ShouldBe(1);
    }

    [Fact]
    public void Text_Clone_ResetsParent()
    {
        var parent = new Text();
        var original = new Text { Parent = parent };

        var clone = (Text)((ICloneable)original).Clone();

        clone.Parent.ShouldBeNull();
    }

    [Fact]
    public void Text_Clone_ReturnsNewInstance()
    {
        var original = new Text { Width = 100, Height = 50 };

        var clone = ((ICloneable)original).Clone();

        clone.ShouldNotBeSameAs(original);
        clone.ShouldBeOfType<Text>();
        ((Text)clone).Width.ShouldBe(100);
        ((Text)clone).Height.ShouldBe(50);
    }

    [Fact]
    public void Text_ImplementsICloneable()
    {
        new Text().ShouldBeAssignableTo<ICloneable>();
    }
}
