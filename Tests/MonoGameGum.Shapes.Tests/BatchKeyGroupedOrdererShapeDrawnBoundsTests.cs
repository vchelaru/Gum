using System.Collections.Generic;
using Gum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// #8152: BatchKeyGroupedOrderer must test overlap with what a shape draws, not its layout box. The
// shape sits between two SolidRectangle backgrounds that touch at y=30. The orderer would like to
// draw both backgrounds together (one SpriteBatch run) and the shape after them, which is only
// right when the shape does not reach the second background.
public class BatchKeyGroupedOrdererShapeDrawnBoundsTests
{
    private static List<DrawCommand> Build(params IRenderableIpso[] renderables)
    {
        Layer layer = new Layer();
        foreach (IRenderableIpso renderable in renderables)
        {
            layer.Add(renderable);
        }
        List<DrawCommand> commands = new();
        new BatchKeyGroupedOrderer().BuildDrawList(layer, commands);
        return commands;
    }

    private static int IndexOf(List<DrawCommand> commands, object target) =>
        commands.FindIndex(c => ReferenceEquals(c.Target, target));

    private static SolidRectangle Background(float y) => new() { X = 0, Y = y, Width = 100, Height = 30 };

    [Fact]
    public void BuildDrawList_DropshadowReachingNextCell_StaysUnderTheNextBackground()
    {
        SolidRectangle first = Background(0);
        RoundedRectangle shape = new()
        {
            X = 10, Y = 5, Width = 80, Height = 20,
            HasDropshadow = true, DropshadowOffsetY = 10, DropshadowBlurX = 0, DropshadowBlurY = 0,
        };
        SolidRectangle second = Background(30);

        List<DrawCommand> commands = Build(first, shape, second);

        IndexOf(commands, shape).ShouldBeLessThan(IndexOf(commands, second));
    }

    [Fact]
    public void BuildDrawList_DropshadowBlurReachingNextCell_StaysUnderTheNextBackground()
    {
        SolidRectangle first = Background(0);
        RoundedRectangle shape = new()
        {
            X = 10, Y = 5, Width = 80, Height = 20,
            HasDropshadow = true, DropshadowOffsetY = 0, DropshadowBlurX = 8, DropshadowBlurY = 8,
        };
        SolidRectangle second = Background(30);

        List<DrawCommand> commands = Build(first, shape, second);

        IndexOf(commands, shape).ShouldBeLessThan(IndexOf(commands, second));
    }

    [Fact]
    public void BuildDrawList_ShapeInsideItsCell_LetsTheBackgroundsMerge()
    {
        SolidRectangle first = Background(0);
        RoundedRectangle shape = new() { X = 10, Y = 5, Width = 80, Height = 20 };
        SolidRectangle second = Background(30);

        List<DrawCommand> commands = Build(first, shape, second);

        IndexOf(commands, second).ShouldBe(1);
    }

    [Fact]
    public void BuildDrawList_ZeroHeightLineWithThickStroke_StaysUnderTheNextBackground()
    {
        SolidRectangle first = Background(0);
        // The box is the segment y=28; a 6 pixel stroke centered on it reaches y=31.
        Line line = new() { X = 10, Y = 28, Width = 80, Height = 0, StrokeWidth = 6 };
        SolidRectangle second = Background(30);

        List<DrawCommand> commands = Build(first, line, second);

        IndexOf(commands, line).ShouldBeLessThan(IndexOf(commands, second));
    }

    [Fact]
    public void BuildDrawList_Svg_KeepsItsPlaceAgainstEveryDraw()
    {
        SolidRectangle first = Background(0);
        Svg svg = new() { X = 10, Y = 5, Width = 20, Height = 20 };
        SolidRectangle second = Background(300);

        List<DrawCommand> commands = Build(first, svg, second);

        IndexOf(commands, svg).ShouldBeLessThan(IndexOf(commands, second),
            "an SVG may draw outside its viewBox, so it must not be reordered past any draw");
    }
}
