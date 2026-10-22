using Gum.Renderables;
using RenderingLibrary;
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
public class RenderableCloneTests : BaseTestClass
{
    [Fact]
    public void LinePolygon_Clone_DoesNotSharePoints()
    {
        LinePolygon original = new LinePolygon();
        original.SetPoints(new[] { new System.Numerics.Vector2(0, 0), new System.Numerics.Vector2(10, 0) });

        LinePolygon clone = (LinePolygon)((ICloneable)original).Clone();
        clone.SetPoints(new[] { new System.Numerics.Vector2(5, 5) });

        original.Points.Count.ShouldBe(2);
        clone.Points.Count.ShouldBe(1);
    }

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
    public void Text_Clone_DoesNotShareWrappedText()
    {
        Text original = new(SystemManagers.Default) { Width = null };
        original.RawText = "Original";

        Text clone = (Text)((ICloneable)original).Clone();
        clone.RawText = "Changed";

        original.WrappedText.ShouldBe(new[] { "Original" });
        clone.WrappedText.ShouldBe(new[] { "Changed" });
    }

    // Issue #6419: the styled-run cache belongs to one Text; shared, an original and its clone drawing with
    // different inline variables would rebuild each other's runs (and allocate) every frame.
    [Fact]
    public void Text_Clone_DoesNotShareTheStyledRunCache()
    {
        Text original = new(SystemManagers.Default) { Width = null };
        original.InlineVariables.Add(new RenderingLibrary.Graphics.InlineVariable
        {
            VariableName = "Color", Value = "Red", StartIndex = 2, CharacterCount = 4
        });
        Text clone = (Text)((ICloneable)original).Clone();
        clone.InlineVariables.Add(new RenderingLibrary.Graphics.InlineVariable
        {
            VariableName = "Color", Value = "Blue", StartIndex = 7, CharacterCount = 2
        });
        const string line = "abcdefghij";

        for (int i = 0; i < 10; i++)
        {
            original.GetCachedStyledSubstrings(0, 0, line);
            clone.GetCachedStyledSubstrings(0, 0, line);
        }
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20; i++)
        {
            original.GetCachedStyledSubstrings(0, 0, line);
            clone.GetCachedStyledSubstrings(0, 0, line);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        allocated.ShouldBe(0);
    }

    // #6420: the scratch list that holds the last wrap's lines belongs to one Text; a clone sharing it
    // would let one Text's re-wrap hand its lines to the other.
    [Fact]
    public void Text_Clone_GetsItsOwnPreviousWrappedTextScratch()
    {
        Text original = new(SystemManagers.Default);

        Text clone = (Text)((ICloneable)original).Clone();

        System.Reflection.FieldInfo field = typeof(Text).GetField(
            "mPreviousWrappedText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        field.GetValue(original).ShouldNotBeNull();
        field.GetValue(clone).ShouldNotBeSameAs(field.GetValue(original));
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
