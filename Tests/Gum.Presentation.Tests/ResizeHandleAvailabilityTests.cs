using Gum.Services;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Color = System.Drawing.Color;

namespace Gum.Presentation.Tests;

/// <summary>
/// Resize handles are drawn and hit-tested only for sides whose axes can change (#7720):
/// edges need their own axis, corners need both.
/// </summary>
public class ResizeHandleAvailabilityTests : BaseTestClass
{
    private readonly SystemManagers? _previousDefault;

    public ResizeHandleAvailabilityTests()
    {
        _previousDefault = SystemManagers.Default;
        SystemManagers.Default = new SystemManagers
        {
            Renderer = new Renderer(),
            ShapeManager = new ShapeManager()
        };
        SystemManagers.Default.ShapeManager.Managers = SystemManagers.Default;
        SystemManagers.Default.Renderer.AddLayer();
    }

    public override void Dispose()
    {
        SystemManagers.Default = _previousDefault;
        base.Dispose();
    }

    [Theory]
    [InlineData(true, true, 8)]
    [InlineData(true, false, 2)]
    [InlineData(false, true, 2)]
    [InlineData(false, false, 0)]
    public void IsSideAvailable_CountsSidesByEnabledAxes(bool width, bool height, int expectedCount)
    {
        int count = Enumerable.Range(0, 8)
            .Count(i => ResizeHandleLayout.IsSideAvailable((ResizeSide)i, width, height));

        count.ShouldBe(expectedCount);
    }

    [Fact]
    public void IsSideAvailable_WidthOnly_AllowsOnlyLeftAndRight()
    {
        List<ResizeSide> sides = Enumerable.Range(0, 8).Select(i => (ResizeSide)i)
            .Where(side => ResizeHandleLayout.IsSideAvailable(side, widthEnabled: true, heightEnabled: false))
            .ToList();

        sides.ShouldBe(new[] { ResizeSide.Right, ResizeSide.Left }, ignoreOrder: true);
    }

    [Fact]
    public void IsSideAvailable_HeightOnly_AllowsOnlyTopAndBottom()
    {
        List<ResizeSide> sides = Enumerable.Range(0, 8).Select(i => (ResizeSide)i)
            .Where(side => ResizeHandleLayout.IsSideAvailable(side, widthEnabled: false, heightEnabled: true))
            .ToList();

        sides.ShouldBe(new[] { ResizeSide.Top, ResizeSide.Bottom }, ignoreOrder: true);
    }

    [Theory]
    [InlineData(true, true, 8)]
    [InlineData(true, false, 2)]
    [InlineData(false, true, 2)]
    [InlineData(false, false, 0)]
    public void GetSideOver_FindsOnlyAvailableHandles(bool width, bool height, int expectedHits)
    {
        ResizeHandles handles = CreateHandles(width, height);

        int hits = Enumerable.Range(0, 8)
            .Count(i => handles.GetSideOver(HandleCenter((ResizeSide)i).X, HandleCenter((ResizeSide)i).Y) == (ResizeSide)i);

        hits.ShouldBe(expectedHits);
    }

    [Fact]
    public void GetSideOver_WidthOnly_HitsRightButNotTopOrCorner()
    {
        ResizeHandles handles = CreateHandles(width: true, height: false);

        handles.GetSideOver(HandleCenter(ResizeSide.Right).X, HandleCenter(ResizeSide.Right).Y).ShouldBe(ResizeSide.Right);
        handles.GetSideOver(HandleCenter(ResizeSide.Top).X, HandleCenter(ResizeSide.Top).Y).ShouldBe(ResizeSide.None);
        handles.GetSideOver(HandleCenter(ResizeSide.TopLeft).X, HandleCenter(ResizeSide.TopLeft).Y).ShouldBe(ResizeSide.None);
    }

    [Fact]
    public void Visible_RestoresOnlyAvailableHandles()
    {
        Layer layer = SystemManagers.Default!.Renderer.MainLayer;
        ResizeHandles handles = new ResizeHandles(layer, Color.White, new CanvasDisplayScale());
        handles.SetAvailableAxes(widthEnabled: true, heightEnabled: false);

        handles.Visible = false;
        handles.Visible = true;

        // Outer and inner handles are added in pairs; the outer handle comes first.
        int visibleOuter = layer.Renderables.OfType<LineRectangle>()
            .Where((_, index) => index % 2 == 0)
            .Count(handle => handle.Visible);
        visibleOuter.ShouldBe(2);
        handles.Visible.ShouldBeTrue();
    }

    private static ResizeHandles CreateHandles(bool width, bool height)
    {
        Layer layer = SystemManagers.Default!.Renderer.MainLayer;
        ResizeHandles handles = new ResizeHandles(layer, Color.White, new CanvasDisplayScale());
        handles.X = 100;
        handles.Y = 50;
        handles.Width = 40;
        handles.Height = 20;
        handles.SetAvailableAxes(width, height);
        return handles;
    }

    private static Vector2 HandleCenter(ResizeSide side)
    {
        ResizeHandleLayout layout = new ResizeHandleLayout(new CanvasDisplayScale());
        float size = layout.GetHandleWorldSize(1);
        Vector2 topLeft = layout.GetHandlePosition(side, 100, 50, 40, 20, size);
        return topLeft + new Vector2(size / 2, size / 2);
    }
}
