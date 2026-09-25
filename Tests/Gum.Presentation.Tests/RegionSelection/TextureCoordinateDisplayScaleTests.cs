using Gum.Services;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using System.Linq;
using TextureCoordinateSelectionPlugin.Logic;
// Aliased under a distinct name: the enclosing "Gum" namespace declares its own unrelated
// RectangleSelector, which would win over a same-named alias.
using TexCoordRectangleSelector = TextureCoordinateSelectionPlugin.RegionSelection.RectangleSelector;

namespace Gum.Presentation.Tests.RegionSelection;

/// <summary>
/// The Texture Coordinates overlay sizes its strokes and handles by the OS display scale, the same
/// way the editor canvas overlay does (#5024).
/// </summary>
public class TextureCoordinateDisplayScaleTests
{
    private static SystemManagers CreateManagers()
    {
        SystemManagers managers = new SystemManagers
        {
            Renderer = new Renderer(),
            ShapeManager = new ShapeManager()
        };
        managers.ShapeManager.Managers = managers;
        managers.Renderer.AddLayer();
        return managers;
    }

    [Fact]
    public void RectangleSelector_DisplayScale_ScalesOutlineAndHandles()
    {
        SystemManagers managers = CreateManagers();
        managers.Renderer.Camera.Zoom = 2;
        CanvasDisplayScale displayScale = new CanvasDisplayScale();
        TexCoordRectangleSelector selector = new TexCoordRectangleSelector(managers, displayScale);
        selector.AddToManagers(managers);

        displayScale.DisplayScale = 2;
        selector.RefreshDisplayScale();

        var rectangles = managers.ShapeManager.Rectangles.ToList();
        // The outline, 8 handles and 8 inner handles.
        rectangles.Count.ShouldBe(17);
        rectangles.ShouldAllBe(rectangle => rectangle.LinePixelWidth == 2);
        // 12 device-independent pixels at 2x display scale and 2x zoom is 12 world units.
        rectangles.Where(rectangle => rectangle.Width == 12 && rectangle.Height == 12).Count().ShouldBe(8);
        // Inner handles are 10 device-independent pixels, so 10 world units.
        rectangles.Where(rectangle => rectangle.Width == 10 && rectangle.Height == 10).Count().ShouldBe(8);
    }

    [Fact]
    public void NineSliceGuideManager_Refresh_SetsLineWidthToDisplayScale()
    {
        SystemManagers managers = CreateManagers();
        CanvasDisplayScale displayScale = new CanvasDisplayScale { DisplayScale = 2 };
        NineSliceGuideManager manager = new NineSliceGuideManager(displayScale);
        manager.Initialize(managers);

        manager.Refresh();

        var lines = managers.Renderer.MainLayer.Renderables.OfType<Line>().ToList();
        lines.Count.ShouldBe(4);
        lines.ShouldAllBe(line => line.LinePixelWidth == 2);
    }

    [Fact]
    public void TextureOutlineManager_Refresh_SetsLineWidthToDisplayScale()
    {
        SystemManagers managers = CreateManagers();
        CanvasDisplayScale displayScale = new CanvasDisplayScale { DisplayScale = 2 };
        TextureOutlineManager manager = new TextureOutlineManager(displayScale);
        manager.Initialize(managers);

        manager.Refresh();

        LineRectangle outline = managers.ShapeManager.Rectangles.Single();
        outline.LinePixelWidth.ShouldBe(2);
    }
}
