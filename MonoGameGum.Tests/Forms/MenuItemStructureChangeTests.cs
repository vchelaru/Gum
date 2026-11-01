using System.Collections.ObjectModel;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// The MenuItem/Menu popup: the menu's structure changing
/// while it is open or closed (items added, removed, hidden, replaced, rebuilt from a collection) and
/// the Visual being replaced or the whole menu leaving and re-entering the tree.
/// </summary>
public class MenuItemStructureChangeTests : BaseTestClass
{
    #region Helpers

    private class Bar
    {
        public ContainerRuntime Container = null!;
        public Menu Menu = null!;
        public MenuItem File = null!;
        public MenuItem Recent = null!;
        public MenuItem NewItem = null!;
        public MenuItem OpenItem = null!;
    }

    // File (New, Open, Recent (One, Two)).
    private static Bar CreateBar()
    {
        Bar bar = new();
        bar.Container = new ContainerRuntime();
        bar.Container.X = 20;
        bar.Container.Y = 20;
        bar.Container.Width = 400;
        bar.Container.WidthUnits = DimensionUnitType.Absolute;
        bar.Container.Height = 40;
        bar.Container.HeightUnits = DimensionUnitType.Absolute;
        Gum.GumService.Default.Root.Children!.Add(bar.Container);

        bar.Menu = new Menu();
        bar.Container.Children!.Add(bar.Menu.Visual);

        bar.File = new MenuItem { Header = "File" };
        bar.Menu.Items!.Add(bar.File);
        bar.NewItem = new MenuItem { Header = "New" };
        bar.File.Items!.Add(bar.NewItem);
        bar.OpenItem = new MenuItem { Header = "Open" };
        bar.File.Items.Add(bar.OpenItem);
        bar.Recent = new MenuItem { Header = "Recent" };
        bar.File.Items.Add(bar.Recent);
        bar.Recent.Items!.Add(new MenuItem { Header = "One" });
        bar.Recent.Items.Add(new MenuItem { Header = "Two" });

        bar.Container.UpdateLayout();
        return bar;
    }

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

    private static int PopupRootCount() => FrameworkElement.PopupRoot.Children!.Count;

    private static bool IsInPopupRoot(GraphicalUiElement visual)
    {
        GraphicalUiElement? current = visual;
        while (current != null)
        {
            if (current == FrameworkElement.PopupRoot)
            {
                return true;
            }
            current = current.Parent as GraphicalUiElement;
        }
        return false;
    }

    private static void ShouldBeBelow(MenuItem owner)
    {
        GraphicalUiElement popup = PopupOf(owner);
        popup.Parent.ShouldBe(FrameworkElement.PopupRoot);
        popup.AbsoluteLeft.ShouldBe(owner.Visual.AbsoluteLeft, 0.5f);
        popup.AbsoluteTop.ShouldBe(owner.Visual.GetAbsoluteBottom(), 0.5f);
    }

    #endregion

    #region Items changing while open

    [Fact]
    public void ItemAddedToSubmenuWhileClosed_AppearsOnTheNextOpen()
    {
        Bar bar = CreateBar();
        MenuItem added = new() { Header = "Added" };

        bar.File.Items!.Add(added);
        bar.File.IsSelected = true;

        IsInPopupRoot(added.Visual).ShouldBeTrue();
    }

    [Fact]
    public void ItemRemovedFromSubmenuWhileClosed_IsNotInThePopupOnTheNextOpen()
    {
        Bar bar = CreateBar();

        bar.File.Items!.Remove(bar.OpenItem);
        bar.File.IsSelected = true;

        IsInPopupRoot(bar.OpenItem.Visual).ShouldBeFalse();
        IsInPopupRoot(bar.NewItem.Visual).ShouldBeTrue();
    }

    [Fact]
    public void AllItemsClearedWhileClosed_SelectingDoesNotOpenAPopup()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        bar.File.Items!.Clear();

        bar.File.IsSelected = true;

        bar.File.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void ItemHiddenInOpenSubmenu_PopupShrinks()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        float before = PopupOf(bar.File).GetAbsoluteHeight();

        bar.OpenItem.Visual.Visible = false;
        PopupOf(bar.File).UpdateLayout();

        PopupOf(bar.File).GetAbsoluteHeight().ShouldBeLessThan(before);
    }

    [Fact]
    public void ItemHiddenInSubmenuWhileClosed_IsNotVisibleOnTheNextOpen()
    {
        Bar bar = CreateBar();
        bar.OpenItem.Visual.Visible = false;

        bar.File.IsSelected = true;

        bar.OpenItem.Visual.AbsoluteVisible.ShouldBeFalse();
        bar.NewItem.Visual.AbsoluteVisible.ShouldBeTrue();
    }

    [Fact]
    public void ItemDisabledInOpenSubmenu_StaysInThePopupAndDoesNotCloseIt()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;

        bar.OpenItem.IsEnabled = false;

        bar.File.IsPopupVisible.ShouldBeTrue();
        IsInPopupRoot(bar.OpenItem.Visual).ShouldBeTrue();
    }

    [Fact]
    public void TopLevelItemRemovedWhileAnotherIsOpen_LeavesTheOpenOneAlone()
    {
        Bar bar = CreateBar();
        MenuItem edit = new() { Header = "Edit" };
        bar.Menu.Items!.Add(edit);
        edit.Items!.Add(new MenuItem { Header = "Cut" });
        bar.Container.UpdateLayout();
        bar.File.IsSelected = true;

        bar.Menu.Items.Remove(edit);

        bar.File.IsPopupVisible.ShouldBeTrue();
        ShouldBeBelow(bar.File);
    }

    [Fact]
    public void TopLevelItemRemovedAndAddedAgain_OpensBelowItsNewPosition()
    {
        Bar bar = CreateBar();
        MenuItem first = new() { Header = "First" };
        bar.Menu.Items!.Insert(0, first);
        first.Items!.Add(new MenuItem { Header = "Thing" });
        bar.Container.UpdateLayout();
        bar.Menu.Items.Remove(bar.File);
        bar.Menu.Items.Add(bar.File);
        bar.Container.UpdateLayout();

        bar.File.IsSelected = true;

        bar.File.IsPopupVisible.ShouldBeTrue();
        ShouldBeBelow(bar.File);
    }

    [Fact]
    public void MenuItemsCleared_WhileAnItemIsOpen_ClosesItsPopup()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        bar.File.IsSelected = true;

        bar.Menu.Items!.Clear();

        bar.File.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void MenuItemInsertedAtTheFront_IsFirstInMenuItems()
    {
        Bar bar = CreateBar();
        MenuItem inserted = new() { Header = "Inserted" };

        bar.Menu.Items!.Insert(0, inserted);

        bar.Menu.MenuItems[0].ShouldBe(inserted);
        bar.Menu.MenuItems[1].ShouldBe(bar.File);
    }

    #endregion

    #region Items built from data

    [Fact]
    public void StringItems_OpenAsOnePopupWithAnEntryPerString()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        MenuItem exports = new() { Header = "Exports" };
        bar.Menu.Items!.Add(exports);
        exports.Items!.Add("PNG");
        exports.Items.Add("SVG");
        bar.Container.UpdateLayout();

        exports.IsSelected = true;

        exports.IsPopupVisible.ShouldBeTrue();
        PopupRootCount().ShouldBe(baseline + 1);
        FrameworkElement.PopupRoot.Children![baseline].Children!.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ObservableCollectionAssignedToMenu_CreatesAMenuItemPerEntryAndFollowsAdds()
    {
        Menu menu = new();
        ObservableCollection<string> source = new() { "File", "Edit" };

        menu.Items = source;

        menu.MenuItems.Count.ShouldBe(2);
        source.Add("View");
        menu.MenuItems.Count.ShouldBe(3);
    }

    [Fact]
    public void ObservableCollectionAssignedToMenu_RemovalRemovesTheMenuItem()
    {
        Menu menu = new();
        ObservableCollection<string> source = new() { "File", "Edit", "View" };
        menu.Items = source;

        source.RemoveAt(1);

        menu.MenuItems.Count.ShouldBe(2);
    }

    [Fact]
    public void SubmenuIndicator_FollowsWhetherANestedItemHasSubItems()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        GraphicalUiElement indicator = bar.Recent.GetVisual("SubmenuIndicatorInstance")!;
        indicator.Visible.ShouldBeTrue("precondition");

        bar.Recent.Items!.Clear();

        indicator.Visible.ShouldBeFalse();

        bar.Recent.Items.Add(new MenuItem { Header = "Again" });

        indicator.Visible.ShouldBeTrue();
    }

    #endregion

    #region Visual replaced

    [Fact]
    public void VisualReplacedOnAnOpenItem_ClosesItsPopup()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        bar.File.IsSelected = true;
        GraphicalUiElement popup = PopupOf(bar.File);

        bar.File.Visual = new MenuItemVisual(tryCreateFormsObject: false);

        bar.File.IsPopupVisible.ShouldBeFalse();
        FrameworkElement.PopupRoot.Children!.ShouldNotContain(popup);
        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void VisualReplacedOnAnOpenItem_ItCanOpenAgainBelowTheNewVisual()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        bar.File.Visual = new MenuItemVisual(tryCreateFormsObject: false);
        bar.Container.UpdateLayout();

        bar.File.IsSelected = false;
        bar.File.IsSelected = true;

        bar.File.IsPopupVisible.ShouldBeTrue();
    }

    [Fact]
    public void VisualReplacedOnANestedOpenItem_ClosesItsPopupAndKeepsTheParentOpen()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        bar.Recent.IsSelected = true;
        GraphicalUiElement recentPopup = PopupOf(bar.Recent);

        bar.Recent.Visual = new MenuItemVisual(tryCreateFormsObject: false);

        bar.Recent.IsPopupVisible.ShouldBeFalse();
        FrameworkElement.PopupRoot.Children!.ShouldNotContain(recentPopup);
        bar.File.IsPopupVisible.ShouldBeTrue();
    }

    [Fact]
    public void MenuVisualReplacedWhileClosed_ItemsStillOpen()
    {
        Bar bar = CreateBar();
        MenuVisual replacement = new(tryCreateFormsObject: false);
        bar.Container.Children!.Add(replacement);
        bar.Menu.Visual = replacement;
        bar.Container.UpdateLayout();

        bar.File.IsSelected = true;

        bar.File.IsPopupVisible.ShouldBeTrue();
    }

    #endregion

    #region Menu leaving and re-entering the tree

    [Fact]
    public void MenuRemovedFromTreeAndAddedBack_OpensBelowItsItem()
    {
        Bar bar = CreateBar();
        Gum.GumService.Default.Root.Children!.Remove(bar.Container);
        Gum.GumService.Default.Root.Children.Add(bar.Container);
        bar.Container.X = 90;
        bar.Container.UpdateLayout();

        bar.File.IsSelected = true;

        ShouldBeBelow(bar.File);
    }

    [Fact]
    public void MenuRemovedWhileThreeLevelsOpen_LeavesNoPopupBehind()
    {
        Bar bar = CreateBar();
        MenuItem deep = new() { Header = "Deep" };
        bar.Recent.Items!.Add(deep);
        deep.Items!.Add(new MenuItem { Header = "Alpha" });
        int baseline = PopupRootCount();
        bar.File.IsSelected = true;
        bar.Recent.IsSelected = true;
        deep.IsSelected = true;
        deep.IsPopupVisible.ShouldBeTrue("precondition");

        Gum.GumService.Default.Root.Children!.Remove(bar.Container);

        bar.File.IsPopupVisible.ShouldBeFalse();
        bar.Recent.IsPopupVisible.ShouldBeFalse();
        deep.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void MenuRemovedAndAddedBack_ThenNestedOpen_PlacesTheNestedPopupBesideItsItem()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        bar.Recent.IsSelected = true;
        Gum.GumService.Default.Root.Children!.Remove(bar.Container);
        Gum.GumService.Default.Root.Children.Add(bar.Container);
        bar.Container.UpdateLayout();

        bar.File.IsSelected = true;
        bar.Recent.IsSelected = true;
        Tick();

        GraphicalUiElement popup = PopupOf(bar.Recent);
        popup.AbsoluteLeft.ShouldBe(bar.Recent.Visual.GetAbsoluteRight(), 0.5f);
        popup.AbsoluteTop.ShouldBe(bar.Recent.Visual.AbsoluteTop, 0.5f);
    }

    [Fact]
    public void MenuDisabledAndEnabledAgain_ItsItemsStillOpen()
    {
        Bar bar = CreateBar();
        bar.File.IsEnabled = false;
        bar.File.IsEnabled = true;

        bar.File.IsSelected = true;

        bar.File.IsPopupVisible.ShouldBeTrue();
        ShouldBeBelow(bar.File);
    }

    #endregion
}
