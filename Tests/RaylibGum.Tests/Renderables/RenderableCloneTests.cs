using Gum.Renderables;
using Shouldly;
using System;
using NineSlice = Gum.Renderables.NineSlice;
using Sprite = Gum.Renderables.Sprite;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Brings the Raylib renderables in line with the MonoGame contract: NineSlice, Sprite,
/// and Text must implement <see cref="ICloneable"/> so that
/// <c>TextBoxBase.RefreshTemplateFromSelectionInstance</c> can clone a SelectionInstance
/// template without going down the null-template path that NREs.
/// </summary>
public class RenderableCloneTests
{
    [Fact]
    public void NineSlice_Clone_CopiesFieldValues()
    {
        var original = new NineSlice
        {
            Width = 42,
            Height = 17,
            Color = new Raylib_cs.Color(10, 20, 30, 40)
        };

        var clone = (NineSlice)((ICloneable)original).Clone();

        clone.Width.ShouldBe(42);
        clone.Height.ShouldBe(17);
        clone.Color.R.ShouldBe((byte)10);
        clone.Color.A.ShouldBe((byte)40);
    }

    [Fact]
    public void NineSlice_Clone_DoesNotShareChildrenWithOriginal()
    {
        var original = new NineSlice();
        var child = new NineSlice();
        original.Children.Add(child);

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
    public void Sprite_Clone_DoesNotShareChildrenWithOriginal()
    {
        var original = new Sprite();
        var child = new Sprite();
        original.Children.Add(child);

        var clone = (Sprite)((ICloneable)original).Clone();

        clone.Children.ShouldBeEmpty();
    }

    [Fact]
    public void Sprite_Clone_ResetsParent()
    {
        var parent = new Sprite();
        var original = new Sprite { Parent = parent };

        var clone = (Sprite)((ICloneable)original).Clone();

        clone.Parent.ShouldBeNull();
    }

    [Fact]
    public void Sprite_Clone_ReturnsNewInstance()
    {
        var original = new Sprite { Width = 25, Height = 50 };

        var clone = ((ICloneable)original).Clone();

        clone.ShouldNotBeSameAs(original);
        clone.ShouldBeOfType<Sprite>();
        ((Sprite)clone).Width.ShouldBe(25);
        ((Sprite)clone).Height.ShouldBe(50);
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
        var child = new Text();
        original.Children.Add(child);

        var clone = (Text)((ICloneable)original).Clone();

        clone.Children.ShouldBeEmpty();
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
        var original = new Text { Width = 100 };

        var clone = ((ICloneable)original).Clone();

        clone.ShouldNotBeSameAs(original);
        clone.ShouldBeOfType<Text>();
        ((Text)clone).Width.ShouldBe(100);
    }

    [Fact]
    public void Text_ImplementsICloneable()
    {
        new Text().ShouldBeAssignableTo<ICloneable>();
    }
}
