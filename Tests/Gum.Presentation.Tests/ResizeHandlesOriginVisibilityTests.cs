using Gum.DataTypes;
using Gum.ToolStates;
using Gum.Wireframe;
using Gum.Wireframe.Editors;
using Gum.Wireframe.Editors.Visuals;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Presentation.Tests;

/// <summary>
/// The origin marker is independent of the resize handles: it draws whenever the rules show an
/// origin variable, even when Width and Height are both unavailable (#7720).
/// </summary>
public class ResizeHandlesOriginVisibilityTests : BaseTestClass
{
    private readonly SystemManagers? _previousDefault;

    public ResizeHandlesOriginVisibilityTests()
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
    public void UpdateToSelection_WidthAndHeightDisabled_ShowsOriginWithoutHandles()
    {
        Layer layer;
        ResizeHandlesVisual visual = Select(width: false, height: false, showOrigin: true, locked: false, out layer);

        IsOriginVisible(layer).ShouldBeTrue();
        VisibleHandleRectangleCount(layer).ShouldBe(0);
    }

    [Fact]
    public void UpdateToSelection_WidthAndHeightDisabled_OriginHidden_ShowsNothing()
    {
        Layer layer;
        ResizeHandlesVisual visual = Select(width: false, height: false, showOrigin: false, locked: false, out layer);

        IsOriginVisible(layer).ShouldBeFalse();
        VisibleHandleRectangleCount(layer).ShouldBe(0);
    }

    [Fact]
    public void UpdateToSelection_Unrestricted_ShowsAllHandlesAndOrigin()
    {
        Layer layer;
        ResizeHandlesVisual visual = Select(width: true, height: true, showOrigin: true, locked: false, out layer);

        IsOriginVisible(layer).ShouldBeTrue();
        // Outer and inner rectangle per handle.
        VisibleHandleRectangleCount(layer).ShouldBe(16);
    }

    [Fact]
    public void UpdateToSelection_WidthOnly_ShowsLeftRightHandlesAndOrigin()
    {
        Layer layer;
        ResizeHandlesVisual visual = Select(width: true, height: false, showOrigin: true, locked: false, out layer);

        IsOriginVisible(layer).ShouldBeTrue();
        VisibleHandleRectangleCount(layer).ShouldBe(4);
    }

    [Fact]
    public void UpdateToSelection_Locked_ShowsNothing()
    {
        Layer layer;
        ResizeHandlesVisual visual = Select(width: true, height: true, showOrigin: true, locked: true, out layer);

        IsOriginVisible(layer).ShouldBeFalse();
        VisibleHandleRectangleCount(layer).ShouldBe(0);
    }

    [Fact]
    public void ShowOrigin_ChangedWhileSelected_UpdatesExistingOriginMarker()
    {
        Layer layer;
        ResizeHandlesVisual visual = Select(width: false, height: false, showOrigin: true, locked: false, out layer);

        visual.ShowOrigin = false;

        IsOriginVisible(layer).ShouldBeFalse();
    }

    private ResizeHandlesVisual Select(bool width, bool height, bool showOrigin, bool locked, out Layer layer)
    {
        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.SetupGet(item => item.SelectedInstance)
            .Returns(new InstanceSave { Locked = locked });
        EditorContext context = EditorContextTestHelper.Create(selectedState: selectedState.Object);
        context.IsWidthChangeEnabled = width;
        context.IsHeightChangeEnabled = height;
        layer = context.OverlayLayer;

        ResizeHandlesVisual visual = new ResizeHandlesVisual(context, System.Drawing.Color.White);
        visual.ShowOrigin = showOrigin;

        GraphicalUiElement element = new GraphicalUiElement();
        context.SelectedObjects.Add(element);
        visual.UpdateToSelection(new List<GraphicalUiElement> { element });
        return visual;
    }

    private static bool IsOriginVisible(Layer layer)
    {
        List<Line> originLines = layer.Renderables.OfType<Line>()
            .Where(line => line.Name == "Origin X Line" || line.Name == "Origin Y Line")
            .ToList();
        return originLines.Any(line => line.Visible);
    }

    private static int VisibleHandleRectangleCount(Layer layer)
    {
        return layer.Renderables.OfType<LineRectangle>().Count(rectangle => rectangle.Visible);
    }
}
