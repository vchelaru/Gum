using System.Linq;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Showing a popup that is already shown (a MenuItem placing its open submenu again, a ComboBox
/// re-placing its open drop-down) leaves it on top of every other popup, once, in the right root,
/// and clamped to the screen at its new position (#6757).
/// </summary>
public class ShowPopupListBoxRepeatTests : BaseTestClass
{
    private static ScrollViewer CreatePopup(float x, float y)
    {
        ScrollViewer popup = new();
        popup.Width = 100;
        popup.Height = 50;
        popup.X = x;
        popup.Y = y;
        return popup;
    }

    [Fact]
    public void ShowPopupListBox_ShouldClampAndKeepOneEntry_WhenShownAgainAfterMovingOffScreen()
    {
        GraphicalUiElement.CanvasWidth = 800;
        GraphicalUiElement.CanvasHeight = 600;
        ContainerRuntime parent = new();
        parent.AddToRoot();
        ScrollViewer popup = CreatePopup(10, 10);
        ListBox.ShowPopupListBox(popup, parent);

        popup.X = 790;
        popup.Y = 20;
        ListBox.ShowPopupListBox(popup, parent);

        FrameworkElement.PopupRoot.Children!.Count(c => c == popup.Visual).ShouldBe(1);
        FrameworkElement.PopupRoot.Children![^1].ShouldBeSameAs(popup.Visual);
        popup.Visual.AbsoluteRight.ShouldBe(800, 0.01f);
        popup.Visual.AbsoluteTop.ShouldBe(20, 0.01f);
        popup.Visual.AbsoluteWidth.ShouldBe(100, 0.01f);
        popup.Visual.AbsoluteHeight.ShouldBe(50, 0.01f);
        popup.Visual.Z.ShouldBe(float.PositiveInfinity);
        popup.IsVisible.ShouldBeTrue();
    }

    [Fact]
    public void ShowPopupListBox_ShouldMovePopupOverOthers_WhenAnotherPopupWasShownAfterIt()
    {
        ContainerRuntime parent = new();
        parent.AddToRoot();
        ScrollViewer first = CreatePopup(10, 10);
        ScrollViewer second = CreatePopup(30, 30);
        ListBox.ShowPopupListBox(first, parent);
        ListBox.ShowPopupListBox(second, parent);

        ListBox.ShowPopupListBox(first, parent);

        var children = FrameworkElement.PopupRoot.Children!;
        children.Count(c => c == first.Visual).ShouldBe(1);
        children[^1].ShouldBeSameAs(first.Visual);
        children.IndexOf(second.Visual).ShouldBeLessThan(children.IndexOf(first.Visual));
    }

    [Fact]
    public void ShowPopupListBox_ShouldMoveToModalRoot_WhenShownAgainAfterParentMovedIntoModalRoot()
    {
        ContainerRuntime parent = new();
        parent.AddToRoot();
        ScrollViewer popup = CreatePopup(10, 10);
        ListBox.ShowPopupListBox(popup, parent);
        FrameworkElement.PopupRoot.Children![^1].ShouldBeSameAs(popup.Visual);

        parent.RemoveFromRoot();
        FrameworkElement.ModalRoot.Children!.Add(parent);
        ListBox.ShowPopupListBox(popup, parent);

        popup.Visual.Parent.ShouldBeSameAs(FrameworkElement.ModalRoot);
        FrameworkElement.PopupRoot.Children!.ShouldNotContain(popup.Visual);
        FrameworkElement.ModalRoot.Children![^1].ShouldBeSameAs(popup.Visual);
    }

    [Fact]
    public void ItemsAdd_ShouldPutSubmenuBackOverComboBoxDropDown_WhenDropDownOpenedAfterSubmenu()
    {
        Menu menu = new();
        menu.AddToRoot();
        MenuItem file = new() { Header = "File" };
        menu.Items!.Add(file);
        file.Items!.Add("New");
        file.IsSelected = true;
        ComboBox comboBox = new();
        comboBox.Items.Add("a");
        comboBox.AddToRoot();

        comboBox.IsDropDownOpen = true;
        var children = FrameworkElement.PopupRoot.Children!;
        children[^1].ShouldBeSameAs(comboBox.ListBox!.Visual);

        // Placing a popup that is not the last one moves it over the others.
        file.Items.Add("Open");

        file.IsPopupVisible.ShouldBeTrue();
        children[^1].ShouldNotBeSameAs(comboBox.ListBox!.Visual);
        ((InteractiveGue)children[^1]).FormsControlAsObject.ShouldBeOfType<ScrollViewer>();
        children.Count(c => c == comboBox.ListBox!.Visual).ShouldBe(1);
    }
}
