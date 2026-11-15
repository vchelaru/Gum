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

    [Fact]
    public void UpdateToSelection_OriginHiddenButXAndYShown_DrawsOffsetLinesWithoutMarker()
    {
        Layer layer;
        Select(width: true, height: true, showOrigin: false, locked: false, showXLine: true, showYLine: true, out layer);

        IsOriginVisible(layer).ShouldBeFalse();
        IsLineVisible(layer, "Origin X Line").ShouldBeTrue();
        IsLineVisible(layer, "Origin Y Line").ShouldBeTrue();
        IsLineVisible(layer, "Origin Top Connector").ShouldBeTrue();
        IsLineVisible(layer, "Origin Left Connector").ShouldBeTrue();
    }

    [Fact]
    public void UpdateToSelection_XHiddenYShown_DrawsOnlyYLineAndItsConnector()
    {
        Layer layer;
        Select(width: true, height: true, showOrigin: true, locked: false, showXLine: false, showYLine: true, out layer);

        IsLineVisible(layer, "Origin X Line").ShouldBeFalse();
        IsLineVisible(layer, "Origin Top Connector").ShouldBeFalse();
        IsLineVisible(layer, "Origin Y Line").ShouldBeTrue();
        IsLineVisible(layer, "Origin Left Connector").ShouldBeTrue();
        IsOriginVisible(layer).ShouldBeTrue();
    }

    [Fact]
    public void UpdateToSelection_YHiddenXShown_DrawsOnlyXLineAndItsConnector()
    {
        Layer layer;
        Select(width: true, height: true, showOrigin: true, locked: false, showXLine: true, showYLine: false, out layer);

        IsLineVisible(layer, "Origin X Line").ShouldBeTrue();
        IsLineVisible(layer, "Origin Top Connector").ShouldBeTrue();
        IsLineVisible(layer, "Origin Y Line").ShouldBeFalse();
        IsLineVisible(layer, "Origin Left Connector").ShouldBeFalse();
    }

    [Fact]
    public void UpdateToSelection_XAndYHiddenOriginShown_DrawsMarkerOnly()
    {
        Layer layer;
        Select(width: true, height: true, showOrigin: true, locked: false, showXLine: false, showYLine: false, out layer);

        IsOriginVisible(layer).ShouldBeTrue();
        IsLineVisible(layer, "Origin X Line").ShouldBeFalse();
        IsLineVisible(layer, "Origin Y Line").ShouldBeFalse();
        IsLineVisible(layer, "Origin Top Connector").ShouldBeFalse();
        IsLineVisible(layer, "Origin Left Connector").ShouldBeFalse();
    }

    [Fact]
    public void UpdateToSelection_OnlyOffsetLinesShown_VisualStaysUp()
    {
        Layer layer;
        Select(width: false, height: false, showOrigin: false, locked: false, showXLine: true, showYLine: false, out layer);

        IsLineVisible(layer, "Origin X Line").ShouldBeTrue();
    }

    [Fact]
    public void UpdateToSelection_Locked_HidesOffsetLines()
    {
        Layer layer;
        Select(width: true, height: true, showOrigin: true, locked: true, showXLine: true, showYLine: true, out layer);

        IsLineVisible(layer, "Origin X Line").ShouldBeFalse();
        IsLineVisible(layer, "Origin Y Line").ShouldBeFalse();
    }

    private ResizeHandlesVisual Select(bool width, bool height, bool showOrigin, bool locked, out Layer layer)
    {
        return Select(width, height, showOrigin, locked, showXLine: true, showYLine: true, out layer);
    }

    private ResizeHandlesVisual Select(bool width, bool height, bool showOrigin, bool locked, bool showXLine, bool showYLine, out Layer layer)
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
        visual.ShowXOffsetLine = showXLine;
        visual.ShowYOffsetLine = showYLine;

        GraphicalUiElement element = new GraphicalUiElement();
        context.SelectedObjects.Add(element);
        visual.UpdateToSelection(new List<GraphicalUiElement> { element });
        return visual;
    }

    private static bool IsOriginVisible(Layer layer)
    {
        return IsLineVisible(layer, "Resize Handle X Line 1") || IsLineVisible(layer, "Resize Handle X Line 2");
    }

    private static bool IsLineVisible(Layer layer, string name)
    {
        return layer.Renderables.OfType<Line>().Any(line => line.Name == name && line.Visible);
    }

    private static int VisibleHandleRectangleCount(Layer layer)
    {
        return layer.Renderables.OfType<LineRectangle>().Count(rectangle => rectangle.Visible);
    }
}
