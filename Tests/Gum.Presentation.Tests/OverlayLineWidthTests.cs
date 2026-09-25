using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Plugins.InternalPlugins.EditorTab.Views;
using Gum.Wireframe;
using Gum.Wireframe.Editors;
using Gum.Wireframe.Editors.Handlers;
using Gum.Wireframe.Editors.Visuals;
using Gum.Input;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Gum.Presentation.Tests;

/// <summary>
/// Overlay lines and circles draw with a stroke as wide as the OS display scale, so on a 2x
/// display they are 2 physical pixels wide like the rest of the overlay (#4986).
/// </summary>
public class OverlayLineWidthTests : BaseTestClass
{
    private readonly SystemManagers? _previousDefault;

    public OverlayLineWidthTests()
    {
        _previousDefault = SystemManagers.Default;
        SystemManagers.Default = new SystemManagers
        {
            Renderer = new Renderer(),
            ShapeManager = new ShapeManager(),
            TextManager = new TextManager()
        };
    }

    public override void Dispose()
    {
        SystemManagers.Default = _previousDefault;
        base.Dispose();
    }

    [Fact]
    public void DimensionDisplayVisual_UpdateToSelection_SetsLineWidthToDisplayScale()
    {
        CanvasDisplayScale displayScale = new CanvasDisplayScale { DisplayScale = 2 };
        EditorContext context = EditorContextTestHelper.Create(displayScale: displayScale);
        ResizeInputHandler resizeInputHandler = new ResizeInputHandler(context, Mock.Of<IResizeHandlesVisual>());
        DimensionDisplayVisual visual = new DimensionDisplayVisual(
            context, WidthOrHeight.Width, resizeInputHandler, Mock.Of<IToolFontService>());

        visual.UpdateToSelection(new List<GraphicalUiElement> { new GraphicalUiElement() });

        List<Line> lines = context.OverlayLayer.Renderables.OfType<Line>().ToList();
        lines.Count.ShouldBe(3);
        lines.ShouldAllBe(line => line.LinePixelWidth == 2);
    }

    [Fact]
    public void DistanceArrows_SetFrom_SetsLineWidthToDisplayScale()
    {
        Layer layer = new Layer();
        Mock<IToolLayerService> layerService = new Mock<IToolLayerService>();
        layerService.SetupGet(item => item.TopLayer).Returns(layer);
        CanvasDisplayScale displayScale = new CanvasDisplayScale { DisplayScale = 2 };
        DistanceArrows arrows = new DistanceArrows(
            SystemManagers.Default, Mock.Of<IToolFontService>(), layerService.Object, displayScale);
        arrows.AddToManagers();

        arrows.SetFrom(new Vector2(0, 0), new Vector2(200, 0));

        List<Line> lines = layer.Renderables.OfType<Line>().ToList();
        lines.Count.ShouldBe(6);
        lines.ShouldAllBe(line => line.LinePixelWidth == 2);
    }

    [Fact]
    public void OriginDisplay_UpdateTo_SetsLineWidthToDisplayScale()
    {
        Layer layer = new Layer();
        CanvasDisplayScale displayScale = new CanvasDisplayScale { DisplayScale = 2 };
        OriginDisplay originDisplay = new OriginDisplay(layer, displayScale);
        GraphicalUiElement element = new GraphicalUiElement();

        originDisplay.UpdateTo(element);
        originDisplay.SetOriginXPosition(element);

        List<Line> lines = layer.Renderables.OfType<Line>().ToList();
        lines.Count.ShouldBe(6);
        lines.ShouldAllBe(line => line.LinePixelWidth == 2);
    }

    [Fact]
    public void Ruler_TicksAndGuides_UseDisplayScaleLineWidth()
    {
        CanvasDisplayScale displayScale = new CanvasDisplayScale { DisplayScale = 2 };
        Mock<IToolLayerService> toolLayerService = new Mock<IToolLayerService>();
        toolLayerService.SetupGet(item => item.TopLayer).Returns(new Layer());
        LayerService layerService = new LayerService();
        layerService.Initialize();
        Ruler ruler = new Ruler(null, new InputLibrary.Cursor(), Mock.Of<IToolFontService>(),
            toolLayerService.Object, layerService, Mock.Of<IHotkeyManager>(), displayScale);

        ruler.GuideValues = new[] { 50f };

        List<Line> lines = layerService.RulerLayer.Renderables.OfType<Line>().ToList();
        lines.Count.ShouldBeGreaterThan(1);
        lines.ShouldAllBe(line => line.LinePixelWidth == 2);
    }

    [Fact]
    public void RotationHandleVisual_UpdateToSelection_SetsCircleWidthToDisplayScale()
    {
        CanvasDisplayScale displayScale = new CanvasDisplayScale { DisplayScale = 2 };
        EditorContext context = EditorContextTestHelper.Create(displayScale: displayScale);
        GraphicalUiElement element = new GraphicalUiElement();
        context.SelectedObjects.Add(element);
        RotationHandleVisual visual = new RotationHandleVisual(context, System.Drawing.Color.Yellow);

        visual.UpdateToSelection(new List<GraphicalUiElement> { element });

        visual.Handle.LinePixelWidth.ShouldBe(2);
    }
}
