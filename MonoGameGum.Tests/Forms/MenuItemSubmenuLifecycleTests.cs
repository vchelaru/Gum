using System;
using System.Reflection;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// An open MenuItem submenu lives in the popup layer, so it closes when its item or an ancestor is
/// hidden or removed from the tree, and nested submenus stay drawn above their parent (#6688).
/// </summary>
public class MenuItemSubmenuLifecycleTests : BaseTestClass
{
    #region Helpers

    private static (ContainerRuntime Container, Menu Menu, MenuItem File) CreateMenu()
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
        return (container, menu, file);
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

    private static MenuItem Recent(MenuItem file) => (MenuItem)file.Items![2]!;

    private static void ShouldBeBelow(MenuItem owner)
    {
        GraphicalUiElement popup = PopupOf(owner);
        popup.Parent.ShouldBe(FrameworkElement.PopupRoot);
        popup.AbsoluteLeft.ShouldBe(owner.Visual.AbsoluteLeft, 0.5f);
        popup.AbsoluteTop.ShouldBe(owner.Visual.GetAbsoluteBottom(), 0.5f);
    }

    private static int PopupRootCount() => FrameworkElement.PopupRoot.Children!.Count;

    private static int HandlerCount(GraphicalUiElement element, string eventName)
    {
        FieldInfo field = typeof(GraphicalUiElement).GetField(eventName, BindingFlags.NonPublic | BindingFlags.Instance)!;
        Delegate? handlers = (Delegate?)field.GetValue(element);
        return handlers?.GetInvocationList().Length ?? 0;
    }

    // Subscriptions on the item's Visual and every ancestor, for both events a watcher would use.
    private static int WatcherSubscriptions(MenuItem item)
    {
        int total = 0;
        GraphicalUiElement? current = item.Visual;
        while (current != null)
        {
            total += HandlerCount(current, "VisibleChanged") + HandlerCount(current, "ParentChanged");
            current = current.Parent;
        }
        return total;
    }

    private static void ShouldBeClosed(MenuItem file, GraphicalUiElement popup, int baselinePopupRootCount)
    {
        file.IsPopupVisible.ShouldBeFalse();
        FrameworkElement.PopupRoot.Children!.ShouldNotContain(popup);
        PopupRootCount().ShouldBe(baselinePopupRootCount);
    }

    private static int PopupIndex(MenuItem owner) => FrameworkElement.PopupRoot.Children!.IndexOf(PopupOf(owner));

    #endregion

    #region Closing when hidden or removed

    [Fact]
    public void ItemRemovedFromParent_WhileOpen_ClosesPopup()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        int baseline = PopupRootCount();
        file.IsSelected = true;
        GraphicalUiElement popup = PopupOf(file);
        FrameworkElement.PopupRoot.Children!.ShouldContain(popup);

        file.Visual.Parent = null;

        ShouldBeClosed(file, popup, baseline);
    }

    [Fact]
    public void ItemRemovedFromMenuItems_WhileOpen_ClosesPopup()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        int baseline = PopupRootCount();
        file.IsSelected = true;
        GraphicalUiElement popup = PopupOf(file);

        menu.Items!.Remove(file);

        ShouldBeClosed(file, popup, baseline);
    }

    [Fact]
    public void AncestorRemovedFromTree_WhileOpen_ClosesPopup()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        int baseline = PopupRootCount();
        file.IsSelected = true;
        GraphicalUiElement popup = PopupOf(file);

        Gum.GumService.Default.Root.Children!.Remove(container);

        ShouldBeClosed(file, popup, baseline);
    }

    [Fact]
    public void AncestorHidden_WhileOpen_ClosesPopup()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        int baseline = PopupRootCount();
        file.IsSelected = true;
        GraphicalUiElement popup = PopupOf(file);

        container.Visible = false;

        ShouldBeClosed(file, popup, baseline);
    }

    [Fact]
    public void ItemHidden_WhileOpen_ClosesPopup()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        int baseline = PopupRootCount();
        file.IsSelected = true;
        GraphicalUiElement popup = PopupOf(file);

        file.Visual.Visible = false;

        ShouldBeClosed(file, popup, baseline);
    }

    [Fact]
    public void NestedSubmenuOpen_AncestorHidden_ClosesBothPopups()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        int baseline = PopupRootCount();
        file.IsSelected = true;
        MenuItem recent = Recent(file);
        recent.IsSelected = true;
        recent.IsPopupVisible.ShouldBeTrue();
        GraphicalUiElement filePopup = PopupOf(file);
        GraphicalUiElement recentPopup = PopupOf(recent);

        container.Visible = false;

        recent.IsPopupVisible.ShouldBeFalse();
        FrameworkElement.PopupRoot.Children!.ShouldNotContain(recentPopup);
        ShouldBeClosed(file, filePopup, baseline);
    }

    [Fact]
    public void NestedSubmenuOpen_ItemRemoved_ClosesBothPopups()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        int baseline = PopupRootCount();
        file.IsSelected = true;
        MenuItem recent = Recent(file);
        recent.IsSelected = true;
        GraphicalUiElement filePopup = PopupOf(file);
        GraphicalUiElement recentPopup = PopupOf(recent);

        file.Visual.Parent = null;

        recent.IsPopupVisible.ShouldBeFalse();
        FrameworkElement.PopupRoot.Children!.ShouldNotContain(recentPopup);
        ShouldBeClosed(file, filePopup, baseline);
    }

    [Fact]
    public void ClosedByHiding_ShownAgain_OpensAFreshPopupBelowTheItem()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        file.IsSelected = true;
        container.Visible = false;
        file.IsPopupVisible.ShouldBeFalse("precondition");

        container.Visible = true;
        container.UpdateLayout();
        file.IsSelected.ShouldBeFalse();
        file.IsSelected = true;

        file.IsPopupVisible.ShouldBeTrue();
        ShouldBeBelow(file);
    }

    #endregion

    #region Reparenting

    [Fact]
    public void Reparent_ToAnotherVisibleParent_WhileOpen_KeepsPopupOpenAndFollows()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        ContainerRuntime other = new();
        other.X = 150;
        other.Y = 60;
        other.Width = 400;
        other.WidthUnits = DimensionUnitType.Absolute;
        other.Height = 40;
        other.HeightUnits = DimensionUnitType.Absolute;
        Gum.GumService.Default.Root.Children!.Add(other);
        file.IsSelected = true;

        other.Children!.Add(menu.Visual);
        other.UpdateLayout();
        Tick();

        file.IsPopupVisible.ShouldBeTrue();
        ShouldBeBelow(file);
    }

    [Fact]
    public void Reparent_ToAHiddenParent_WhileOpen_ClosesPopup()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        int baseline = PopupRootCount();
        ContainerRuntime hidden = new();
        hidden.Visible = false;
        Gum.GumService.Default.Root.Children!.Add(hidden);
        file.IsSelected = true;
        GraphicalUiElement popup = PopupOf(file);

        hidden.Children!.Add(menu.Visual);

        ShouldBeClosed(file, popup, baseline);
    }

    [Fact]
    public void Reparent_ThenHideNewAncestor_WhileOpen_ClosesPopup()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        int baseline = PopupRootCount();
        ContainerRuntime other = new();
        Gum.GumService.Default.Root.Children!.Add(other);
        file.IsSelected = true;
        GraphicalUiElement popup = PopupOf(file);
        other.Children!.Add(menu.Visual);
        file.IsPopupVisible.ShouldBeTrue("precondition: reparenting to a visible parent keeps it open");

        other.Visible = false;

        ShouldBeClosed(file, popup, baseline);
    }

    [Fact]
    public void Reparent_ThenHideOldAncestor_WhileOpen_KeepsPopupOpen()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        ContainerRuntime other = new();
        Gum.GumService.Default.Root.Children!.Add(other);
        file.IsSelected = true;
        other.Children!.Add(menu.Visual);

        container.Visible = false;

        file.IsPopupVisible.ShouldBeTrue();
    }

    #endregion

    #region Closed menu untouched

    [Fact]
    public void PopupClosed_HideThenShowMenu_DoesNothingAndStillOpens()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        int baseline = PopupRootCount();

        container.Visible = false;
        container.Visible = true;
        container.UpdateLayout();

        file.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);

        file.IsSelected = true;
        file.IsPopupVisible.ShouldBeTrue();
        ShouldBeBelow(file);
    }

    [Fact]
    public void PopupClosed_ItemReparented_DoesNothing()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        int baseline = PopupRootCount();

        GraphicalUiElement menuVisual = (GraphicalUiElement)file.Visual.Parent!;
        file.Visual.Parent = null;
        menuVisual.Children!.Add(file.Visual);

        file.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    #endregion

    #region Subscriptions

    [Fact]
    public void PopupClosed_AddsNoSubscriptionsToTheTree()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        int baseline = WatcherSubscriptions(file);

        file.IsSelected = true;
        int whileOpen = WatcherSubscriptions(file);
        file.IsSelected = false;

        whileOpen.ShouldBeGreaterThan(baseline, "precondition: an open submenu listens for hide and detach");
        WatcherSubscriptions(file).ShouldBe(baseline);
    }

    [Fact]
    public void RepeatedOpenClose_DoesNotLeakSubscriptions()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        MenuItem recent = Recent(file);
        int baseline = WatcherSubscriptions(file);

        for (int i = 0; i < 5; i++)
        {
            file.IsSelected = true;
            recent.IsSelected = true;
            recent.IsSelected = false;
            file.IsSelected = false;
        }
        WatcherSubscriptions(file).ShouldBe(baseline);

        file.IsSelected = true;
        int openOnce = WatcherSubscriptions(file);
        file.IsSelected = false;
        file.IsSelected = true;
        WatcherSubscriptions(file).ShouldBe(openOnce);
    }

    [Fact]
    public void ClosedByHiding_RemovesItsSubscriptions()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        int baseline = WatcherSubscriptions(file);
        file.IsSelected = true;
        Recent(file).IsSelected = true;

        container.Visible = false;

        WatcherSubscriptions(file).ShouldBe(baseline);
    }

    [Fact]
    public void ReparentWhileOpen_DoesNotLeakSubscriptionsOnTheOldAncestors()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        ContainerRuntime other = new();
        Gum.GumService.Default.Root.Children!.Add(other);
        int baseline = HandlerCount(container, "VisibleChanged") + HandlerCount(container, "ParentChanged");
        file.IsSelected = true;
        other.Children!.Add(menu.Visual);

        (HandlerCount(container, "VisibleChanged") + HandlerCount(container, "ParentChanged")).ShouldBe(baseline);
    }

    #endregion

    #region Draw order

    [Fact]
    public void ParentReplacedWhileNestedOpen_NestedPopupStaysAboveParentPopup()
    {
        // At the bottom edge the File popup is clamped, so moving the menu up a little re-places it
        // at the same spot and the nested item's rectangle does not change.
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        container.Anchor(Anchor.BottomLeft);
        container.UpdateLayout();
        file.IsSelected = true;
        MenuItem recent = Recent(file);
        recent.IsSelected = true;
        GraphicalUiElement recentPopup = PopupOf(recent);
        float recentPopupY = recentPopup.AbsoluteTop;

        container.Y -= 5;
        container.UpdateLayout();
        Tick();

        recentPopup.AbsoluteTop.ShouldBe(recentPopupY, 0.5f, "precondition: the nested popup did not move");
        PopupIndex(file).ShouldBeGreaterThanOrEqualTo(0);
        PopupIndex(recent).ShouldBeGreaterThan(PopupIndex(file));
    }

    [Fact]
    public void ParentReplacedWhileTwoLevelsOpen_EveryLevelStaysAboveItsParent()
    {
        (ContainerRuntime container, Menu menu, MenuItem file) = CreateMenu();
        MenuItem recent = Recent(file);
        recent.Items!.Add(new MenuItem { Header = "Deep" });
        MenuItem deep = (MenuItem)recent.Items[2]!;
        deep.Items!.Add(new MenuItem { Header = "Deeper" });
        container.Anchor(Anchor.BottomLeft);
        container.UpdateLayout();
        file.IsSelected = true;
        recent.IsSelected = true;
        deep.IsSelected = true;
        deep.IsPopupVisible.ShouldBeTrue("precondition");

        container.Y -= 5;
        container.UpdateLayout();
        Tick();

        PopupIndex(file).ShouldBeGreaterThanOrEqualTo(0);
        PopupIndex(recent).ShouldBeGreaterThan(PopupIndex(file));
        PopupIndex(deep).ShouldBeGreaterThan(PopupIndex(recent));
    }

    #endregion

    [Fact]
    public void MenuItemsReplace_OfOpenTopLevelItem_ClosesItsSubmenu()
    {
        Menu menu = new();
        Gum.GumService.Default.Root.Children!.Add(menu.Visual);
        MenuItem file = new() { Header = "File" };
        menu.Items!.Add(file);
        file.Items!.Add(new MenuItem { Header = "A" });
        file.IsSelected = true;
        file.IsPopupVisible.ShouldBeTrue();

        menu.Items[0] = new MenuItem { Header = "Edit" };

        file.IsPopupVisible.ShouldBeFalse();
        file.Visual.Parent.ShouldBeNull();
    }
}
