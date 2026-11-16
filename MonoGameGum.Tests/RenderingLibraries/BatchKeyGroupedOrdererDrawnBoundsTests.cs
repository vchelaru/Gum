using System.Collections.Generic;
using Gum.DataTypes;
using Gum.GueDeriving;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using static MonoGameGum.Tests.RenderingLibraries.BatchOrdererScenes;

namespace MonoGameGum.Tests.RenderingLibraries;

// #8152: the overlap test must use what a renderable draws, not its layout box. Each scene puts a
// draw between two same-texture backgrounds that touch at a seam. The draw's layout box stays in
// the first background, but what it draws reaches into the second, so it must stay before the
// second background (depth-first order) instead of letting the two backgrounds merge ahead of it.
public class BatchKeyGroupedOrdererDrawnBoundsTests : BaseTestClass
{
    private static readonly object BackgroundTexture = new object();

    private static OrdererFake Background(string name, float x, float y, float width, float height) =>
        new(name) { X = x, Y = y, Width = width, Height = height, BatchSortKey = BackgroundTexture };

    private static List<DrawCommand> Build(params IRenderableIpso[] renderables)
    {
        List<DrawCommand> commands = new();
        new BatchKeyGroupedOrderer().BuildDrawList(BuildLayer(renderables), commands);
        return commands;
    }

    private static int IndexOf(List<DrawCommand> commands, object target) =>
        commands.FindIndex(c => ReferenceEquals(c.Target, target));

    private static TextRuntime CreateText(float x, float y, float width, float height, string text) => new()
    {
        X = x, Y = y, Width = width, Height = height,
        WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute,
        Text = text,
    };

    [Fact]
    public void BuildDrawList_TextSpillingIntoNextCell_StaysUnderTheNextBackground()
    {
        OrdererFake first = Background("bg0", 0, 0, 100, 30);
        TextRuntime text = CreateText(4, 4, 90, 8, "line one\nline two\nline three\nline four");
        OrdererFake second = Background("bg1", 0, 30, 100, 30);

        // The layout box ends at y=12, the drawn lines reach past y=30 into the second background.
        (text.GetAbsoluteTop() + text.AbsoluteHeight).ShouldBeLessThan(30f);
        (text.GetAbsoluteTop() + ((Text)text.RenderableComponent!).WrappedTextHeight).ShouldBeGreaterThan(30f);

        List<DrawCommand> commands = Build(first, text, second);

        IndexOf(commands, text).ShouldBeLessThan(IndexOf(commands, second),
            "the spilled lines must draw under the next background, as in depth-first order");
    }

    [Fact]
    public void BuildDrawList_TextLineWiderThanItsBox_StaysUnderTheNextBackgroundToTheRight()
    {
        // Wrapping breaks a word between letters, but one letter cannot be broken, so a box
        // narrower than the letter draws it past the box's right edge.
        OrdererFake first = Background("bg0", 0, 0, 8, 30);
        TextRuntime text = CreateText(2, 4, 2, 20, "W");
        OrdererFake second = Background("bg1", 8, 0, 50, 30);

        (text.GetAbsoluteLeft() + text.AbsoluteWidth).ShouldBeLessThan(8f);
        (text.GetAbsoluteLeft() + ((Text)text.RenderableComponent!).WrappedTextWidth).ShouldBeGreaterThan(8f);

        List<DrawCommand> commands = Build(first, text, second);

        IndexOf(commands, text).ShouldBeLessThan(IndexOf(commands, second));
    }

    [Fact]
    public void BuildDrawList_TextDropshadowReachingNextCell_StaysUnderTheNextBackground()
    {
        OrdererFake first = Background("bg0", 0, 0, 100, 30);
        TextRuntime text = CreateText(4, 0, 90, 0, "shadowed");
        Text renderable = (Text)text.RenderableComponent!;
        float lineHeight = renderable.WrappedTextHeight;
        text.Height = lineHeight;
        text.Y = 28 - lineHeight;
        renderable.HasDropshadow = true;
        renderable.DropshadowOffsetY = 8;
        OrdererFake second = Background("bg1", 0, 30, 100, 30);

        // The box ends 2 pixels above the seam; the shadow is drawn 8 pixels below the glyphs.
        (text.GetAbsoluteTop() + text.AbsoluteHeight).ShouldBe(28f, tolerance: 0.01f);

        List<DrawCommand> commands = Build(first, text, second);

        IndexOf(commands, text).ShouldBeLessThan(IndexOf(commands, second));
    }

    [Fact]
    public void BuildDrawList_TextInsideItsCell_LetsTheBackgroundsMerge()
    {
        OrdererFake first = Background("bg0", 0, 0, 100, 30);
        TextRuntime text = CreateText(4, 4, 90, 20, "short");
        OrdererFake second = Background("bg1", 0, 30, 100, 30);

        List<DrawCommand> commands = Build(first, text, second);

        IndexOf(commands, second).ShouldBe(1, "text drawn inside its own cell must not block the merge");
    }

    [Fact]
    public void BuildDrawList_RotatedRenderableSwingingIntoNextCell_StaysUnderTheNextBackground()
    {
        OrdererFake first = Background("bg0", 0, 0, 100, 30);
        // Unrotated it spans y 20..25; turned 45 degrees about its top-left its far end swings
        // about 40 pixels away from y=20, into the second background whichever way it turns.
        OrdererFake turned = new("turned") { X = 10, Y = 20, Width = 60, Height = 5, Rotation = 45 };
        OrdererFake second = Background("bg1", 0, 30, 100, 30);

        List<DrawCommand> commands = Build(first, turned, second);

        IndexOf(commands, turned).ShouldBeLessThan(IndexOf(commands, second));
    }

    [Fact]
    public void BuildDrawList_RenderableWithUnknownDrawnBounds_OverlapsEverything()
    {
        OrdererFake first = Background("bg0", 0, 0, 100, 30);
        OrdererFake unknown = new("unknown") { X = 10, Y = 10, Width = 10, Height = 10, ReportsDrawnBounds = false };
        OrdererFake second = Background("bg1", 0, 300, 100, 30);

        List<DrawCommand> commands = Build(first, unknown, second);

        IndexOf(commands, unknown).ShouldBeLessThan(IndexOf(commands, second),
            "a renderable that cannot say where it draws must keep its depth-first place");
    }

    [Fact]
    public void BuildDrawList_SpriteWithOversizeSourceRectangle_DrawsInsideItsBox()
    {
        OrdererFake first = Background("bg0", 0, 0, 100, 30);
        SpriteRuntime sprite = new()
        {
            X = 10, Y = 5, Width = 20, Height = 20,
            WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute,
        };
        // A source region far larger than (and partly left of) any texture still lands on the box.
        ((Sprite)sprite.RenderableComponent!).SourceRectangle = new System.Drawing.Rectangle(-500, -500, 2000, 2000);
        OrdererFake second = Background("bg1", 0, 30, 100, 30);

        List<DrawCommand> commands = Build(first, sprite, second);

        IndexOf(commands, second).ShouldBe(1);
    }

    [Fact]
    public void BuildDrawList_NineSliceInsideItsBox_LetsTheBackgroundsMerge()
    {
        OrdererFake first = Background("bg0", 0, 0, 100, 30);
        NineSliceRuntime nineSlice = new()
        {
            X = 10, Y = 5, Width = 20, Height = 20,
            WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute,
        };
        OrdererFake second = Background("bg1", 0, 30, 100, 30);

        List<DrawCommand> commands = Build(first, nineSlice, second);

        IndexOf(commands, second).ShouldBe(1);
    }
}
