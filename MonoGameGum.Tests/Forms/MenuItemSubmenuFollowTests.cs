using System.Collections.Specialized;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// An open MenuItem submenu stays attached to its MenuItem when the menu, an ancestor or the canvas
/// moves (#6680, same cause as #6644 for the ComboBox drop-down).
/// </summary>
public class MenuItemSubmenuFollowTests : BaseTestClass
{
    #region Helpers

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
        file.Items!.Add(new MenuItem { Header = "New" });
        file.Items.Add(new MenuItem { Header = "Open" });

        MenuItem recent = new() { Header = "Recent" };
        file.Items.Add(recent);
        recent.Items!.Add(new MenuItem { Header = "One" });
        recent.Items.Add(new MenuItem { Header = "Two" });

        container.UpdateLayout();
        return (container, file);
    }

    private static MenuItem AddTopLevelItem(MenuItem existingTopLevel, string header)
    {
        GraphicalUiElement? ancestor = existingTopLevel.Visual;
        while (ancestor != null && (ancestor as InteractiveGue)?.FormsControlAsObject is not Menu)
        {
            ancestor = ancestor.Parent as GraphicalUiElement;
        }
        Menu menu = (Menu)((InteractiveGue)ancestor!).FormsControlAsObject!;
        MenuItem item = new() { Header = header };
        menu.Items!.Add(item);
        item.Items!.Add(new MenuItem { Header = header + " 1" });
        item.Items.Add(new MenuItem { Header = header + " 2" });
        return item;
    }

    /// <summary>Ticks the roots in the order GumService.Update does: Root, then PopupRoot.</summary>
    private static void Tick(double seconds = 0.016)
    {
        Gum.GumService.Default.Root.AnimateSelf(seconds);
        FrameworkElement.PopupRoot.AnimateSelf(seconds);
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

    // A submenu of a top-level item opens below the item.
    private static void ShouldBeBelow(MenuItem owner)
    {
        GraphicalUiElement popup = PopupOf(owner);
        popup.Parent.ShouldBe(FrameworkElement.PopupRoot);
        popup.AbsoluteLeft.ShouldBe(owner.Visual.AbsoluteLeft, 0.5f);
        popup.AbsoluteTop.ShouldBe(owner.Visual.GetAbsoluteBottom(), 0.5f);
    }

    // A submenu of an item that is itself in a submenu opens to the right of the item.
    private static void ShouldBeBeside(MenuItem owner)
    {
        GraphicalUiElement popup = PopupOf(owner);
        popup.Parent.ShouldBe(FrameworkElement.PopupRoot);
        popup.AbsoluteLeft.ShouldBe(owner.Visual.GetAbsoluteRight(), 0.5f);
        popup.AbsoluteTop.ShouldBe(owner.Visual.AbsoluteTop, 0.5f);
    }

    private static void ResizeCanvas(float width, float height)
    {
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;
        Gum.GumService.Default.Root.UpdateLayout();
        // GumService.Update sizes the popup root to the canvas each frame, before the roots animate.
        Gum.Forms.FormsUtilities.SetDimensionsToCanvas(FrameworkElement.PopupRoot);
        FrameworkElement.PopupRoot.UpdateLayout();
    }

    private static MenuItem OpenRecent(MenuItem file)
    {
        MenuItem recent = (MenuItem)file.Items![2]!;
        recent.IsSelected = true;
        recent.IsPopupVisible.ShouldBeTrue();
        return recent;
    }

    #endregion

    #region Following

    [Fact]
    public void Follow_AncestorMoves_SubmenuStaysBelowItem()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();
        file.IsSelected = true;
        ShouldBeBelow(file);

        container.X += 120;
        container.Y += 40;
        container.UpdateLayout();
        Tick();

        ShouldBeBelow(file);
    }

    [Fact]
    public void Follow_CanvasResizes_SubmenuStaysBelowItem()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();
        container.Anchor(Anchor.TopRight);
        file.IsSelected = true;
        float itemXAtOpen = file.Visual.AbsoluteX;

        ResizeCanvas(1100, GraphicalUiElement.CanvasHeight);
        file.Visual.AbsoluteX.ShouldNotBe(itemXAtOpen, "precondition: the canvas resize moved the menu");
        Tick();

        ShouldBeBelow(file);
    }

    [Fact]
    public void Follow_MenuItselfMoves_SubmenuStaysBelowItem()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();
        file.IsSelected = true;

        file.Visual.Parent!.X += 60;
        container.UpdateLayout();
        Tick();

        ShouldBeBelow(file);
    }

    [Fact]
    public void Follow_NestedSubmenu_FollowsItsItemAndItsParentSubmenu()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();
        file.IsSelected = true;
        MenuItem recent = OpenRecent(file);
        ShouldBeBelow(file);
        ShouldBeBeside(recent);

        container.X += 90;
        container.Y += 30;
        container.UpdateLayout();
        Tick();

        ShouldBeBelow(file);
        ShouldBeBeside(recent);
    }

    [Fact]
    public void Follow_NestedSubmenuAfterCanvasResize_FollowsToo()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();
        container.Anchor(Anchor.TopRight);
        file.IsSelected = true;
        MenuItem recent = OpenRecent(file);

        ResizeCanvas(1000, GraphicalUiElement.CanvasHeight);
        Tick();

        ShouldBeBelow(file);
        ShouldBeBeside(recent);
    }

    [Fact]
    public void Follow_MenuMovesNearBottomEdge_SubmenuIsClampedToCanvas()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();
        container.Anchor(Anchor.BottomRight);
        file.IsSelected = true;
        GraphicalUiElement popup = PopupOf(file);

        ResizeCanvas(700, 500);
        Tick();

        (popup.AbsoluteY + popup.AbsoluteHeight).ShouldBe(GraphicalUiElement.CanvasHeight, 0.5f);
        (popup.AbsoluteX + popup.AbsoluteWidth).ShouldBeLessThanOrEqualTo(GraphicalUiElement.CanvasWidth + 0.5f);
        popup.AbsoluteX.ShouldBeGreaterThanOrEqualTo(-0.5f);
    }

    [Fact]
    public void Follow_ItemHoveredAway_SubmenuStaysOpenAndFollows()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();
        file.IsHighlighted = true;
        file.IsSelected = true;
        file.IsHighlighted = false;
        file.IsPopupVisible.ShouldBeTrue();

        container.X += 50;
        container.UpdateLayout();
        Tick();

        file.IsPopupVisible.ShouldBeTrue();
        ShouldBeBelow(file);
    }

    [Fact]
    public void Follow_SwitchingToAnotherTopLevelItem_OnlyTheOpenOneIsPlacedAndFollows()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();
        MenuItem edit = AddTopLevelItem(file, "Edit");
        container.UpdateLayout();
        file.IsSelected = true;
        edit.IsSelected = true;
        file.IsPopupVisible.ShouldBeFalse();
        edit.IsPopupVisible.ShouldBeTrue();

        container.X += 100;
        container.UpdateLayout();
        Tick();

        ShouldBeBelow(edit);
        file.IsPopupVisible.ShouldBeFalse();
        FrameworkElement.PopupRoot.Children!.ShouldNotContain(PopupOf(file));
    }

    #endregion

    #region Closed and unchanged

    [Fact]
    public void Follow_SubmenuClosed_IsNotPlacedAgainWhenMenuMoves()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();
        file.IsSelected = true;
        file.IsSelected = false;
        file.IsPopupVisible.ShouldBeFalse();

        container.X += 120;
        container.UpdateLayout();
        Tick();

        file.IsPopupVisible.ShouldBeFalse();
        FrameworkElement.PopupRoot.Children!.ShouldNotContain(PopupOf(file));
    }

    [Fact]
    public void Follow_NestedSubmenuClosedWithItsParentOpen_ParentStillFollows()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();
        file.IsSelected = true;
        MenuItem recent = OpenRecent(file);
        recent.IsSelected = false;
        recent.IsPopupVisible.ShouldBeFalse();

        container.X += 70;
        container.UpdateLayout();
        Tick();

        ShouldBeBelow(file);
        recent.IsPopupVisible.ShouldBeFalse();
    }

    [Fact]
    public void Follow_RectUnchanged_SubmenuIsNotPlacedAgain()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();
        file.IsSelected = true;
        MenuItem recent = OpenRecent(file);
        // Placing a popup sets its position, which lays it out; a popup already on top is no longer
        // taken out of the popup root and added back (#6762), so layouts are what show a placement.
        int before = GraphicalUiElement.UpdateLayoutCallCount;

        Tick();
        Tick();
        Tick();

        (GraphicalUiElement.UpdateLayoutCallCount - before).ShouldBe(0);

        container.X += 25;
        container.UpdateLayout();
        before = GraphicalUiElement.UpdateLayoutCallCount;
        Tick();
        (GraphicalUiElement.UpdateLayoutCallCount - before).ShouldBeGreaterThan(0);
        before = GraphicalUiElement.UpdateLayoutCallCount;

        Tick();
        Tick();

        (GraphicalUiElement.UpdateLayoutCallCount - before).ShouldBe(0);
        ShouldBeBeside(recent);
    }

    #endregion

    #region Idle

    [Fact]
    public void Idle_SubmenuClosed_MenuItemDoesNotKeepAncestorsAwake()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();

        Tick();
        Tick();
        Tick();

        container.IsIdle.ShouldBeTrue();
    }

    [Fact]
    public void Idle_SubmenuOpenedAfterGoingIdle_IsAwakeUntilClosed()
    {
        (ContainerRuntime container, MenuItem file) = CreateMenu();
        Tick();
        Tick();
        Tick();
        container.IsIdle.ShouldBeTrue("precondition: a closed menu lets the subtree go idle");

        file.IsSelected = true;
        container.X += 80;
        container.UpdateLayout();
        Tick();
        Tick();

        container.IsIdle.ShouldBeFalse();
        ShouldBeBelow(file);

        file.IsSelected = false;
        Tick();
        Tick();
        Tick();

        container.IsIdle.ShouldBeTrue();
    }

    #endregion
}
