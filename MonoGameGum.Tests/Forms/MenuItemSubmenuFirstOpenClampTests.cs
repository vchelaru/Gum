using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A submenu whose size comes from its items is clamped to the canvas using that final size on its
/// very first open, not the size it had before its items sized it (#6686).
/// </summary>
public class MenuItemSubmenuFirstOpenClampTests : BaseTestClass
{
    private static (ContainerRuntime Container, MenuItem File) CreateMenu()
    {
        ContainerRuntime container = new();
        container.Width = 400;
        container.WidthUnits = DimensionUnitType.Absolute;
        container.Height = 40;
        container.HeightUnits = DimensionUnitType.Absolute;
        Gum.GumService.Default.Root.Children!.Add(container);

        Menu menu = new();
        container.Children!.Add(menu.Visual);

        MenuItem file = new() { Header = "File" };
        menu.Items!.Add(file);
        for (int i = 0; i < 6; i++)
        {
            file.Items!.Add(new MenuItem { Header = "Item number " + i });
        }

        container.UpdateLayout();
        return (container, file);
    }

    private static GraphicalUiElement PopupOf(MenuItem owner)
    {
        GraphicalUiElement visual = ((MenuItem)owner.Items![0]!).Visual;
        while (visual.Parent != null && visual.Parent != FrameworkElement.PopupRoot)
        {
            visual = (GraphicalUiElement)visual.Parent;
        }
        return visual;
    }

    // With no ScrollViewerVisualTemplate the popup is a plain ScrollViewer that MenuItem itself
    // switches to RelativeToChildren while opening.
    private static (ContainerRuntime Container, MenuItem File) CreateMenuWithoutPopupTemplate()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();
        file.ScrollViewerVisualTemplate = null;
        return (container, file);
    }

    [Fact]
    public void FirstOpen_NoTemplateNearBottomEdge_PopupEndsExactlyAtCanvasBottom()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenuWithoutPopupTemplate();
        container.Anchor(Anchor.BottomLeft);
        container.UpdateLayout();

        file.IsSelected = true;

        GraphicalUiElement popup = PopupOf(file);
        popup.Parent.ShouldBe(FrameworkElement.PopupRoot);
        popup.AbsoluteHeight.ShouldBeGreaterThan(0);
        popup.AbsoluteTop.ShouldBeGreaterThanOrEqualTo(-0.5f);
        (popup.AbsoluteTop + popup.AbsoluteHeight).ShouldBe(GraphicalUiElement.CanvasHeight, 0.5f);
    }

    [Fact]
    public void FirstOpen_NoTemplateNearRightEdge_PopupEndsExactlyAtCanvasRight()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenuWithoutPopupTemplate();
        container.X = GraphicalUiElement.CanvasWidth - 20;
        container.UpdateLayout();

        file.IsSelected = true;

        GraphicalUiElement popup = PopupOf(file);
        popup.Parent.ShouldBe(FrameworkElement.PopupRoot);
        popup.AbsoluteWidth.ShouldBeGreaterThan(20);
        (popup.AbsoluteLeft + popup.AbsoluteWidth).ShouldBe(GraphicalUiElement.CanvasWidth, 0.5f);
    }

    [Fact]
    public void FirstOpen_DefaultTemplateNearBottomEdge_PopupEndsExactlyAtCanvasBottom()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();
        container.Anchor(Anchor.BottomLeft);
        container.UpdateLayout();

        file.IsSelected = true;

        GraphicalUiElement popup = PopupOf(file);
        (popup.AbsoluteTop + popup.AbsoluteHeight).ShouldBe(GraphicalUiElement.CanvasHeight, 0.5f);
    }

    [Fact]
    public void FirstOpen_NoTemplateFarFromEdges_PopupIsPlacedBelowItemUnclamped()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenuWithoutPopupTemplate();

        file.IsSelected = true;

        GraphicalUiElement popup = PopupOf(file);
        popup.AbsoluteLeft.ShouldBe(file.Visual.AbsoluteLeft, 0.5f);
        popup.AbsoluteTop.ShouldBe(file.Visual.GetAbsoluteBottom(), 0.5f);
        popup.WidthUnits.ShouldBe(DimensionUnitType.RelativeToChildren);
    }

    [Fact]
    public void FirstOpen_FarFromEdges_PopupIsPlacedBelowItemUnclamped()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();

        file.IsSelected = true;

        GraphicalUiElement popup = PopupOf(file);
        popup.AbsoluteLeft.ShouldBe(file.Visual.AbsoluteLeft, 0.5f);
        popup.AbsoluteTop.ShouldBe(file.Visual.GetAbsoluteBottom(), 0.5f);
    }
}
