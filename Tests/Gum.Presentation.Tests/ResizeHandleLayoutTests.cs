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
// Aliased under a distinct name: the enclosing "Gum" namespace declares its own unrelated
// RectangleSelector, which would win over a same-named alias.
using TexCoordRectangleSelector = TextureCoordinateSelectionPlugin.RegionSelection.RectangleSelector;

namespace Gum.Presentation.Tests;

/// <summary>
/// The editor canvas's resize handles and the Texture Coordinates selector's handles share one
/// layout (#5030): 8 handles in <see cref="ResizeSide"/> order, sitting outside the rect's corners
/// and centered on its edges, sized by the display scale and zoom.
/// </summary>
public class ResizeHandleLayoutTests : BaseTestClass
{
    private readonly SystemManagers? _previousDefault;

    public ResizeHandleLayoutTests()
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

    [Fact]
    public void GetHandlePosition_PlacesHandlesOutsideCornersAndCenteredOnEdges()
    {
        ResizeHandleLayout layout = new ResizeHandleLayout(new CanvasDisplayScale());

        Vector2[] positions = Enumerable.Range(0, 8)
            .Select(i => layout.GetHandlePosition((ResizeSide)i, left: 100, top: 50, width: 40, height: 20, handleSize: 12))
            .ToArray();

        positions.ShouldBe(ExpectedPositions);
    }

    [Fact]
    public void GetHandleWorldSize_ScalesByDisplayScaleAndZoom()
    {
        ResizeHandleLayout layout = new ResizeHandleLayout(new CanvasDisplayScale { DisplayScale = 2 });

        layout.GetHandleWorldSize(zoom: 4).ShouldBe(6);
    }

    [Fact]
    public void EditorResizeHandles_PositionsOuterHandlesByTheSharedLayout()
    {
        Layer layer = SystemManagers.Default!.Renderer.MainLayer;
        ResizeHandles handles = new ResizeHandles(layer, Color.White, new CanvasDisplayScale());

        handles.X = 100;
        handles.Y = 50;
        handles.Width = 40;
        handles.Height = 20;

        // Outer and inner handles are added in pairs; the outer handle comes first.
        List<LineRectangle> outerHandles = layer.Renderables.OfType<LineRectangle>()
            .Where((_, index) => index % 2 == 0)
            .ToList();
        outerHandles.Select(handle => new Vector2(handle.X, handle.Y)).ShouldBe(ExpectedPositions);
    }

    [Fact]
    public void TextureRectangleSelector_PositionsHandlesByTheSharedLayout()
    {
        SystemManagers managers = SystemManagers.Default!;
        TexCoordRectangleSelector selector = new TexCoordRectangleSelector(managers, new CanvasDisplayScale());
        selector.AddToManagers(managers);

        selector.Left = 100;
        selector.Top = 50;
        selector.Width = 40;
        selector.Height = 20;

        // The first rectangle is the selector's outline; the 8 handles follow.
        List<LineRectangle> handleRectangles = managers.ShapeManager.Rectangles.Skip(1).Take(8).ToList();
        handleRectangles.Select(handle => new Vector2(handle.X, handle.Y)).ShouldBe(ExpectedPositions);
    }

    [Fact]
    public void EditorResizeHandles_DrawsBlackInnerHandleInsideEachHandle()
    {
        Layer layer = SystemManagers.Default!.Renderer.MainLayer;
        ResizeHandles handles = new ResizeHandles(layer, Color.White, new CanvasDisplayScale());

        handles.X = 100;
        handles.Y = 50;
        handles.Width = 40;
        handles.Height = 20;

        List<LineRectangle> innerHandles = layer.Renderables.OfType<LineRectangle>()
            .Where((_, index) => index % 2 == 1)
            .ToList();
        ShouldBeInnerHandles(innerHandles);
    }

    [Fact]
    public void TextureRectangleSelector_DrawsBlackInnerHandleInsideEachHandle()
    {
        SystemManagers managers = SystemManagers.Default!;
        TexCoordRectangleSelector selector = new TexCoordRectangleSelector(managers, new CanvasDisplayScale());
        selector.AddToManagers(managers);

        selector.Left = 100;
        selector.Top = 50;
        selector.Width = 40;
        selector.Height = 20;

        // Outline, then the 8 handles, then their 8 inner handles.
        List<LineRectangle> innerHandles = managers.ShapeManager.Rectangles.Skip(9).ToList();
        ShouldBeInnerHandles(innerHandles);
    }

    // Each inner handle is inset 1 unit from its 12-unit handle, so it is 10 units wide.
    private static void ShouldBeInnerHandles(List<LineRectangle> innerHandles)
    {
        innerHandles.Select(handle => new Vector2(handle.X, handle.Y))
            .ShouldBe(ExpectedPositions.Select(position => position + new Vector2(1, 1)));
        innerHandles.ShouldAllBe(handle => handle.Width == 10 && handle.Height == 10);
        innerHandles.ShouldAllBe(handle => handle.Color.ToArgb() == Color.Black.ToArgb());
    }

    // A 40x20 rect at (100, 50) with 12-unit handles, in ResizeSide order.
    private static readonly Vector2[] ExpectedPositions =
    {
        new Vector2(88, 38),
        new Vector2(114, 38),
        new Vector2(140, 38),
        new Vector2(140, 54),
        new Vector2(140, 70),
        new Vector2(114, 70),
        new Vector2(88, 70),
        new Vector2(88, 54),
    };
}
