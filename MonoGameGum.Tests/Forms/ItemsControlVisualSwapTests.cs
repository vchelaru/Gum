using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using RenderingLibrary;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Items authored on a Visual in the Gum tool come over to the control, and items added in code
/// survive a Visual swap (issue #6227).
/// </summary>
public class ItemsControlVisualSwapTests : BaseTestClass
{
    #region Authored items come over (characterization)

    [Fact]
    public void ListBox_ShouldAdoptAuthoredListBoxItems_WhenVisualAssignedToFreshListBox()
    {
        ListBoxVisual authoredVisual = new(tryCreateFormsObject: false);
        ListBoxItem authoredItem = new();
        authoredVisual.InnerPanelInstance.Children.Add(authoredItem.Visual);

        ListBox sut = new(authoredVisual);

        sut.Items!.Count.ShouldBe(1);
        sut.Items[0].ShouldBe(authoredItem);
        sut.InnerPanel.Children.Count.ShouldBe(1);
    }

    [Fact]
    public void ListBox_ShouldAdoptAuthoredListBoxItems_WhenVisualAssignedToListBoxWithoutItems()
    {
        ListBox sut = new();
        ListBoxVisual authoredVisual = new(tryCreateFormsObject: false);
        ListBoxItem authoredItem = new();
        authoredVisual.InnerPanelInstance.Children.Add(authoredItem.Visual);

        sut.Visual = authoredVisual;

        sut.Items!.Count.ShouldBe(1);
        sut.Items[0].ShouldBe(authoredItem);
    }

    [Fact]
    public void ItemsControl_ShouldKeepAuthoredChildrenInPanel_WhenVisualAssigned()
    {
        ListBoxVisual authoredVisual = new(tryCreateFormsObject: false);
        ListBoxItem authoredItem = new();
        authoredVisual.InnerPanelInstance.Children.Add(authoredItem.Visual);

        ItemsControl sut = new(authoredVisual);

        sut.InnerPanel.Children.ShouldContain(authoredItem.Visual);
        authoredItem.Visual.Parent.ShouldBe(authoredVisual.InnerPanelInstance);
    }

    [Fact]
    public void Menu_ShouldKeepAuthoredMenuItemsInPanel_WhenVisualAssigned()
    {
        MenuVisual authoredVisual = new(tryCreateFormsObject: false);
        MenuItem authoredItem = new();
        authoredVisual.InnerPanelInstance.Children.Add(authoredItem.Visual);

        Menu sut = new(authoredVisual);

        sut.InnerPanel.Children.ShouldContain(authoredItem.Visual);
    }

    [Fact]
    public void MenuItem_ShouldShowAuthoredSubItemsInPopup_WhenSelected()
    {
        ContainerRuntime container = new();
        container.AttachManagersOnly(SystemManagers.Default);
        ContainerRuntime authoredVisual = new();
        ContainerRuntime subItemContainer = new() { Name = "SubItemContainerInstance" };
        authoredVisual.AddChild(subItemContainer);
        MenuItem authoredSubItem = new();
        subItemContainer.Children.Add(authoredSubItem.Visual);
        MenuItem sut = new(authoredVisual);
        container.Children.Add(sut.Visual);

        sut.IsSelected = true;

        sut.IsPopupVisible.ShouldBeTrue();
        sut.Items![0].ShouldBe(authoredSubItem);
    }

    #endregion

    #region Authored items are fully registered

    [Fact]
    public void ListBox_ShouldRegisterAuthoredRows_WhenVisualAssignedToFreshListBox()
    {
        ListBoxVisual authoredVisual = new(tryCreateFormsObject: false);
        ListBoxItem authoredItem = new();
        authoredVisual.InnerPanelInstance.Children.Add(authoredItem.Visual);

        ListBox sut = new(authoredVisual);
        authoredItem.IsSelected = true;

        sut.ListBoxItems.Count.ShouldBe(1);
        sut.SelectedObject.ShouldBe(authoredItem);
    }

    #endregion

    #region ListBox swap

    [Fact]
    public void ListBox_ShouldShowCodeAddedItemsInNewPanel_WhenVisualSwapped()
    {
        ListBox sut = new();
        sut.Items!.Add("a");
        sut.Items.Add("b");
        ListBoxVisual newVisual = new(tryCreateFormsObject: false);

        sut.Visual = newVisual;

        newVisual.InnerPanelInstance.Children.Count.ShouldBe(2);
        sut.Items.Count.ShouldBe(2);
        sut.ListBoxItems.Count.ShouldBe(2);
        sut.ListBoxItems[0].DataObject.ShouldBe("a");
        sut.ListBoxItems[1].DataObject.ShouldBe("b");
    }

    [Fact]
    public void ListBox_ShouldReparentCodeAddedListBoxItems_WhenVisualSwapped()
    {
        ListBox sut = new();
        ListBoxItem codeItem = new();
        sut.Items!.Add(codeItem);
        ListBoxVisual newVisual = new(tryCreateFormsObject: false);

        sut.Visual = newVisual;

        codeItem.Visual.Parent.ShouldBe(newVisual.InnerPanelInstance);
        sut.Items.Count.ShouldBe(1);
        sut.ListBoxItems.Count.ShouldBe(1);
        sut.ListBoxItems[0].ShouldBe(codeItem);
    }

    [Fact]
    public void ListBox_ShouldAddNewItemsToNewPanel_AfterVisualSwapped()
    {
        ListBox sut = new();
        sut.Items!.Add("a");
        InteractiveGue oldVisual = sut.Visual;
        ListBoxVisual newVisual = new(tryCreateFormsObject: false);
        sut.Visual = newVisual;

        sut.Items.Add("b");

        newVisual.InnerPanelInstance.Children.Count.ShouldBe(2);
        sut.ListBoxItems.Count.ShouldBe(2);
        ((ListBoxVisual)oldVisual).InnerPanelInstance.Children.Count.ShouldBe(1);
    }

    [Fact]
    public void ListBox_ShouldKeepAuthoredItemsFirst_WhenSwappedToVisualWithAuthoredItems()
    {
        ListBox sut = new();
        sut.Items!.Add("code");
        ListBoxVisual newVisual = new(tryCreateFormsObject: false);
        ListBoxItem authoredItem = new();
        newVisual.InnerPanelInstance.Children.Add(authoredItem.Visual);

        sut.Visual = newVisual;

        sut.Items.Count.ShouldBe(2);
        sut.Items[0].ShouldBe(authoredItem);
        sut.Items[1].ShouldBe("code");
        newVisual.InnerPanelInstance.Children.Count.ShouldBe(2);
        newVisual.InnerPanelInstance.Children[0].ShouldBe(authoredItem.Visual);
        sut.ListBoxItems.Count.ShouldBe(2);
        sut.ListBoxItems[0].ShouldBe(authoredItem);
        sut.ListBoxItems[1].DataObject.ShouldBe("code");
    }

    [Fact]
    public void ListBox_ShouldDropOldAuthoredItems_WhenVisualSwapped()
    {
        ListBoxVisual oldVisual = new(tryCreateFormsObject: false);
        ListBoxItem oldAuthoredItem = new();
        oldVisual.InnerPanelInstance.Children.Add(oldAuthoredItem.Visual);
        ListBox sut = new(oldVisual);
        sut.Items!.Add("code");
        ListBoxVisual newVisual = new(tryCreateFormsObject: false);

        sut.Visual = newVisual;

        sut.Items.Count.ShouldBe(1);
        sut.Items[0].ShouldBe("code");
        sut.ListBoxItems.Count.ShouldBe(1);
        newVisual.InnerPanelInstance.Children.Count.ShouldBe(1);
        oldVisual.InnerPanelInstance.Children.ShouldContain(oldAuthoredItem.Visual);
    }

    [Fact]
    public void ListBox_ShouldKeepSelection_WhenVisualSwapped()
    {
        ListBox sut = new();
        sut.Items!.Add("a");
        sut.Items.Add("b");
        sut.SelectedIndex = 1;

        sut.Visual = new ListBoxVisual(tryCreateFormsObject: false);

        sut.SelectedObject.ShouldBe("b");
        sut.SelectedIndex.ShouldBe(1);
        sut.ListBoxItems[1].IsSelected.ShouldBeTrue();
        sut.ListBoxItems[0].IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void ListBox_ShouldSelectCarriedRows_AfterVisualSwapped()
    {
        ListBox sut = new();
        sut.Items!.Add("a");
        sut.Items.Add("b");
        sut.Visual = new ListBoxVisual(tryCreateFormsObject: false);

        sut.ListBoxItems[0].IsSelected = true;

        sut.SelectedObject.ShouldBe("a");
    }

    #endregion

    #region ItemsControl and Menu swap

    [Fact]
    public void ItemsControl_ShouldShowCodeAddedItemsInNewPanel_WhenVisualSwapped()
    {
        ItemsControl sut = new();
        sut.Items!.Add("a");
        sut.Items.Add("b");
        ListBoxVisual newVisual = new(tryCreateFormsObject: false);

        sut.Visual = newVisual;

        newVisual.InnerPanelInstance.Children.Count.ShouldBe(2);
        sut.Items.Count.ShouldBe(2);
    }

    [Fact]
    public void Menu_ShouldShowCodeAddedItemsInNewPanel_WhenVisualSwapped()
    {
        Menu sut = new();
        MenuItem fileItem = new();
        sut.Items!.Add(fileItem);
        sut.Items.Add("Edit");
        MenuVisual newVisual = new(tryCreateFormsObject: false);

        sut.Visual = newVisual;

        newVisual.InnerPanelInstance.Children.Count.ShouldBe(2);
        sut.Items.Count.ShouldBe(2);
        sut.MenuItems.Count.ShouldBe(2);
        sut.MenuItems[0].ShouldBe(fileItem);
        fileItem.Visual.Parent.ShouldBe(newVisual.InnerPanelInstance);
    }

    [Fact]
    public void Menu_ShouldAdoptAuthoredMenuItems_WhenVisualAssigned()
    {
        MenuVisual authoredVisual = new(tryCreateFormsObject: false);
        MenuItem authoredItem = new();
        authoredVisual.InnerPanelInstance.Children.Add(authoredItem.Visual);

        Menu sut = new(authoredVisual);
        sut.Items!.Add("Edit");

        sut.Items.Count.ShouldBe(2);
        sut.Items[0].ShouldBe(authoredItem);
        sut.MenuItems.Count.ShouldBe(2);
        sut.MenuItems[0].ShouldBe(authoredItem);
    }

    [Fact]
    public void Menu_ShouldNotTouchAuthoredMenuItems_WhenVisualSwapped()
    {
        Menu sut = new();
        sut.Items!.Add("Edit");
        MenuVisual newVisual = new(tryCreateFormsObject: false);
        MenuItem authoredItem = new();
        newVisual.InnerPanelInstance.Children.Add(authoredItem.Visual);

        sut.Visual = newVisual;

        newVisual.InnerPanelInstance.Children.Count.ShouldBe(2);
        newVisual.InnerPanelInstance.Children[0].ShouldBe(authoredItem.Visual);
    }

    #endregion

    #region Menu push action

    [Fact]
    public void Menu_ShouldQueueOneNextPushAction_WhenCodeAddedItemSelectedAfterVisualSwapped()
    {
        Menu sut = new();
        MenuItem item = new();
        sut.Items!.Add(item);
        sut.Visual = new MenuVisual(tryCreateFormsObject: false);
        InteractiveGue.ClearNextPushActions();

        item.IsSelected = true;

        InteractiveGue.PendingNextPushActionCount.ShouldBe(1);
    }

    [Fact]
    public void Menu_ShouldQueueOneNextPushAction_WhenAuthoredItemSelectedAfterVisualSwappedBackIn()
    {
        MenuVisual visualA = new(tryCreateFormsObject: false);
        MenuItem authoredItem = new();
        visualA.InnerPanelInstance.Children.Add(authoredItem.Visual);
        Menu sut = new(visualA);
        sut.Visual = new MenuVisual(tryCreateFormsObject: false);
        sut.Visual = visualA;
        InteractiveGue.ClearNextPushActions();

        authoredItem.IsSelected = true;

        sut.Items!.Count.ShouldBe(1);
        InteractiveGue.PendingNextPushActionCount.ShouldBe(1);
    }

    #endregion

    #region Bound Items

    [Fact]
    public void ListBox_ShouldRebuildRowsWithoutTouchingBoundCollection_WhenVisualSwapped()
    {
        ObservableCollection<string> source = new() { "a", "b" };
        ListBox sut = new();
        sut.Items = source;
        int changeCount = 0;
        source.CollectionChanged += (_, _) => changeCount++;
        ListBoxVisual newVisual = new(tryCreateFormsObject: false);

        sut.Visual = newVisual;
        source.Add("c");

        changeCount.ShouldBe(1);
        source.ShouldBe(new[] { "a", "b", "c" });
        sut.Items.ShouldBeSameAs(source);
        newVisual.InnerPanelInstance.Children.Count.ShouldBe(3);
        sut.ListBoxItems.Count.ShouldBe(3);
        sut.ListBoxItems[2].DataObject.ShouldBe("c");
    }

    [Fact]
    public void ListBox_ShouldKeepSelectionAndRemoveFromNewPanel_WhenBoundCollectionChangesAfterVisualSwapped()
    {
        ObservableCollection<string> source = new() { "a", "b" };
        ListBox sut = new();
        sut.Items = source;
        sut.SelectedIndex = 1;
        ListBoxVisual newVisual = new(tryCreateFormsObject: false);
        sut.Visual = newVisual;

        source.RemoveAt(0);

        sut.SelectedObject.ShouldBe("b");
        newVisual.InnerPanelInstance.Children.Count.ShouldBe(1);
        sut.ListBoxItems.Count.ShouldBe(1);
    }

    #endregion

    #region RefreshStyles after swap

    [Fact]
    public void ListBox_ShouldKeepItemsAndSelection_WhenRefreshStylesAfterVisualSwapped()
    {
        ListBox sut = new();
        sut.Items!.Add("a");
        sut.Items.Add("b");
        sut.SelectedIndex = 1;
        ListBoxVisual newVisual = new(tryCreateFormsObject: false);
        sut.Visual = newVisual;

        newVisual.RefreshStyles();

        sut.Items.Count.ShouldBe(2);
        newVisual.InnerPanelInstance.Children.Count.ShouldBe(2);
        sut.ListBoxItems.Count.ShouldBe(2);
        sut.SelectedObject.ShouldBe("b");
        sut.ListBoxItems[1].IsSelected.ShouldBeTrue();
    }

    #endregion

    #region MenuItem swap

    private static ContainerRuntime CreateMenuItemVisualWithSubItem(MenuItem subItem)
    {
        ContainerRuntime visual = new();
        ContainerRuntime subItemContainer = new() { Name = "SubItemContainerInstance" };
        visual.AddChild(subItemContainer);
        subItemContainer.Children.Add(subItem.Visual);
        return visual;
    }

    [Fact]
    public void MenuItem_ShouldReplaceAuthoredSubItems_WhenVisualSwapped()
    {
        MenuItem oldAuthored = new();
        MenuItem sut = new(CreateMenuItemVisualWithSubItem(oldAuthored));
        sut.Items!.Add("code");
        MenuItem newAuthored = new();

        sut.Visual = CreateMenuItemVisualWithSubItem(newAuthored);

        sut.Items.Count.ShouldBe(2);
        sut.Items[0].ShouldBe("code");
        sut.Items[1].ShouldBe(newAuthored);
    }

    [Fact]
    public void MenuItem_ShouldRestoreAuthoredSubItems_WhenPreviousVisualSwappedBackIn()
    {
        MenuItem authored = new();
        ContainerRuntime visualA = CreateMenuItemVisualWithSubItem(authored);
        MenuItem sut = new(visualA);
        sut.Items!.Add("code");
        ContainerRuntime visualB = new();

        sut.Visual = visualB;
        sut.Visual = visualA;

        sut.Items.Count.ShouldBe(2);
        sut.Items[0].ShouldBe("code");
        sut.Items[1].ShouldBe(authored);
    }

    [Fact]
    public void MenuItem_ShouldLeaveOldVisualIntact_WhenVisualSwapped()
    {
        MenuItem authored = new();
        ContainerRuntime oldVisual = new();
        ContainerRuntime header = new();
        ContainerRuntime subItemContainer = new() { Name = "SubItemContainerInstance" };
        oldVisual.AddChild(header);
        oldVisual.AddChild(subItemContainer);
        subItemContainer.Children.Add(authored.Visual);
        MenuItem sut = new(oldVisual);

        sut.Visual = new ContainerRuntime();

        oldVisual.Children.IndexOf(subItemContainer).ShouldBe(1);
        subItemContainer.Children.ShouldContain(authored.Visual);
    }

    [Fact]
    public void MenuItem_ShouldKeepOnlyCodeAddedSubItems_WhenSwappedToVisualWithoutSubItems()
    {
        MenuItem oldAuthored = new();
        MenuItem sut = new(CreateMenuItemVisualWithSubItem(oldAuthored));
        sut.Items!.Add("code");

        sut.Visual = new ContainerRuntime();

        sut.Items.Count.ShouldBe(1);
        sut.Items[0].ShouldBe("code");
    }

    [Fact]
    public void MenuItem_ShouldDropAuthoredSubItems_WhenVisualSetToNull()
    {
        MenuItem oldAuthored = new();
        MenuItem sut = new(CreateMenuItemVisualWithSubItem(oldAuthored));
        sut.Items!.Add("code");

        sut.Visual = null!;

        sut.Items.Count.ShouldBe(1);
        sut.Items[0].ShouldBe("code");
    }

    [Fact]
    public void MenuItem_ShouldIgnoreQueuedOldSubItems_WhenVisualSwappedBeforeQueueProcessed()
    {
        ContainerRuntime oldVisual = new();
        ContainerRuntime oldSubItemContainer = new() { Name = "SubItemContainerInstance" };
        oldVisual.AddChild(oldSubItemContainer);
        MenuItem sut = new(oldVisual);
        MenuItem lateSubItem = new();
        oldSubItemContainer.Children.Add(lateSubItem.Visual);
        sut.Visual = new ContainerRuntime();

        Gum.GumService.Default.DeferredQueue.ProcessPending();

        sut.Items!.Count.ShouldBe(0);
    }

    [Fact]
    public void MenuItem_ShouldClosePopup_WhenVisualSwapped()
    {
        ContainerRuntime container = new();
        container.AttachManagersOnly(SystemManagers.Default);
        MenuItem sut = new();
        container.Children.Add(sut.Visual);
        sut.Items!.Add("SubItem");
        sut.IsSelected = true;
        sut.IsPopupVisible.ShouldBeTrue();

        sut.Visual = new MenuItemVisual(tryCreateFormsObject: false);

        sut.IsPopupVisible.ShouldBeFalse();
        sut.IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void MenuItem_ShouldClosePopup_WhenVisualSetToNull()
    {
        ContainerRuntime container = new();
        container.AttachManagersOnly(SystemManagers.Default);
        MenuItem sut = new();
        container.Children.Add(sut.Visual);
        sut.Items!.Add("SubItem");
        sut.IsSelected = true;
        sut.IsPopupVisible.ShouldBeTrue();

        sut.Visual = null!;

        sut.IsPopupVisible.ShouldBeFalse();
    }

    [Fact]
    public void MenuItem_ShouldClosePopup_WhenVisualSwappedWithoutManagers()
    {
        ContainerRuntime container = new();
        MenuItem sut = new();
        container.Children.Add(sut.Visual);
        sut.Items!.Add("SubItem");
        sut.IsSelected = true;

        Should.NotThrow(() => sut.Visual = new MenuItemVisual(tryCreateFormsObject: false));

        sut.IsPopupVisible.ShouldBeFalse();
    }

    #endregion
}
