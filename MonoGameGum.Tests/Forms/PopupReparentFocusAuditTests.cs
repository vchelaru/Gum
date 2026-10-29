using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.Controls.Games;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using Moq;
using MonoGameGum.Tests.Input;
using RenderingLibrary;
using Shouldly;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Audit of library operations that detach and re-attach a Visual (Grid layout, popup placement, sticky
/// headers, menu submenus, windows). A focused control the library only moves keeps its focus; hiding
/// still clears it, and showing never gives it back (#6934, #6956).
/// </summary>
public class PopupReparentFocusAuditTests : BaseTestClass
{
    public PopupReparentFocusAuditTests()
    {
        // A still cursor away from every control, so only the keyboard and gamepad act.
        Mock<ICursor> cursor = new();
        cursor.SetupProperty(c => c.VisualOver);
        cursor.SetupProperty(c => c.WindowPushed);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(c => c.X).Returns(-1000);
        cursor.Setup(c => c.Y).Returns(-1000);
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(-1000);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(-1000);
        Gum.Forms.FormsUtilities.SetCursor(cursor.Object);
    }

    private static void RunFrame() => Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

    private static void Press(Keys key)
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyTyped(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys> { key });
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);
        try
        {
            RunFrame();
        }
        finally
        {
            FrameworkElement.KeyboardsForUiControl.Remove(keyboard.Object);
        }
    }

    #region Grid

    private static (Grid grid, Button button) CreateGridWithButton()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.AddToRoot();
        Button button = new Button();
        grid.AddChild(button, row: 0, column: 0);
        return (grid, button);
    }

    [Fact]
    public void Grid_RowAdded_FocusedChildKeepsFocus()
    {
        (Grid grid, Button button) = CreateGridWithButton();
        button.IsFocused = true;
        GraphicalUiElement cellBefore = button.Visual.Parent!;

        grid.RowDefinitions.Add(new RowDefinition());

        button.Visual.Parent.ShouldNotBe(cellBefore, "precondition: the refresh rebuilt the cell the button is in");
        button.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(button);
    }

    [Fact]
    public void Grid_SiblingAdded_FocusedChildKeepsFocus()
    {
        (Grid grid, Button button) = CreateGridWithButton();
        button.IsFocused = true;

        grid.AddChild(new Button(), row: 0, column: 0);

        button.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Grid_LayoutRefreshed_GamepadStillClicksFocusedChild()
    {
        (Grid grid, Button button) = CreateGridWithButton();
        button.IsFocused = true;
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        grid.RowDefinitions.Add(new RowDefinition());
        PadHarness pad = new();

        pad.Tap(Buttons.A);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void Grid_LayoutRefreshed_UnfocusedChildDoesNotTakeFocus()
    {
        (Grid grid, Button button) = CreateGridWithButton();

        grid.RowDefinitions.Add(new RowDefinition());

        button.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void Grid_HideFocusedChild_ClearsFocus()
    {
        (_, Button button) = CreateGridWithButton();
        button.IsFocused = true;

        button.IsVisible = false;

        button.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void Grid_LayoutRefreshedWhileChildHidden_DoesNotRestoreFocus()
    {
        (Grid grid, Button button) = CreateGridWithButton();
        button.IsFocused = true;
        button.IsVisible = false;

        grid.RowDefinitions.Add(new RowDefinition());
        button.IsVisible = true;

        button.IsFocused.ShouldBeFalse();
    }

    #endregion

    #region ListBox.ShowPopupListBox

    private static (ListBox popup, Button owner) CreateOpenPopup()
    {
        Button owner = new Button();
        owner.AddToRoot();
        ListBox popup = new ListBox();
        popup.Items!.Add("A");
        popup.Items.Add("B");
        ListBox.ShowPopupListBox(popup, owner.Visual);
        return (popup, owner);
    }

    // Something added to the popup layer after the popup, so placing the popup again has to move it.
    private static void CoverPopup()
    {
        ContainerRuntime cover = new ContainerRuntime();
        Gum.GumService.Default.PopupRoot.Children!.Add(cover);
    }

    [Fact]
    public void ShowPopupListBox_FocusedPopupPlacedAgainBehindOtherPopup_KeepsFocus()
    {
        (ListBox popup, Button owner) = CreateOpenPopup();
        popup.IsFocused = true;
        popup.IsFocused.ShouldBeTrue("precondition: the popup is visible and takes focus");
        CoverPopup();

        ListBox.ShowPopupListBox(popup, owner.Visual);

        popup.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(popup);
    }

    [Fact]
    public void ShowPopupListBox_FocusedPopupPlacedAgain_GamepadStillSelectsItem()
    {
        (ListBox popup, Button owner) = CreateOpenPopup();
        popup.IsFocused = true;
        popup.DoListItemsHaveFocus = true;
        CoverPopup();
        ListBox.ShowPopupListBox(popup, owner.Visual);
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);

        popup.DoListItemsHaveFocus.ShouldBeTrue();
        popup.SelectedIndex.ShouldBeGreaterThan(-1);
    }

    [Fact]
    public void ShowPopupListBox_FocusedScrolledPopupPlacedAgain_KeepsScroll()
    {
        Button owner = new Button();
        owner.AddToRoot();
        ListBox popup = new ListBox();
        for (int i = 0; i < 40; i++)
        {
            popup.Items!.Add("Item " + i);
        }
        popup.Visual.Height = 100;
        popup.Visual.HeightUnits = DimensionUnitType.Absolute;
        ListBox.ShowPopupListBox(popup, owner.Visual);
        popup.SelectedIndex = 2;
        popup.VerticalScrollBarValue = 60;
        popup.IsFocused = true;
        popup.DoListItemsHaveFocus = true;
        popup.VerticalScrollBarValue = 60;
        double scrollBefore = popup.VerticalScrollBarValue;
        scrollBefore.ShouldBeGreaterThan(0);
        CoverPopup();

        ListBox.ShowPopupListBox(popup, owner.Visual);

        popup.IsFocused.ShouldBeTrue();
        popup.VerticalScrollBarValue.ShouldBe(scrollBefore);
    }

    [Fact]
    public void ShowPopupListBox_FocusedPopupAlreadyOnTop_KeepsFocus()
    {
        (ListBox popup, Button owner) = CreateOpenPopup();
        popup.IsFocused = true;

        ListBox.ShowPopupListBox(popup, owner.Visual);

        popup.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void ShowPopupListBox_UnfocusedPopupPlacedAgain_DoesNotTakeFocus()
    {
        (ListBox popup, Button owner) = CreateOpenPopup();
        CoverPopup();

        ListBox.ShowPopupListBox(popup, owner.Visual);

        popup.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void ShowPopupListBox_HiddenFocusedPopup_ClearsFocusAndStaysCleared()
    {
        (ListBox popup, Button owner) = CreateOpenPopup();
        popup.IsFocused = true;

        popup.IsVisible = false;
        popup.IsVisible = true;

        popup.IsFocused.ShouldBeFalse();
    }

    #endregion

    #region ScrollViewer sticky headers

    private static (ScrollViewer scrollViewer, Button header) CreateStickyHeaderButton()
    {
        ScrollViewer scrollViewer = new ScrollViewer();
        scrollViewer.AddToRoot();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;
        Button header = new Button();
        scrollViewer.AddChild(header);
        scrollViewer.AddChild(new Button());
        scrollViewer.RegisterStickyHeader(header);
        return (scrollViewer, header);
    }

    [Fact]
    public void StickyHeader_RegisterFocusedHeader_KeepsFocus()
    {
        ScrollViewer scrollViewer = new ScrollViewer();
        scrollViewer.AddToRoot();
        Button header = new Button();
        scrollViewer.AddChild(header);
        header.IsFocused = true;

        scrollViewer.RegisterStickyHeader(header);

        header.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void StickyHeader_UnregisterFocusedHeader_KeepsFocus()
    {
        (ScrollViewer scrollViewer, Button header) = CreateStickyHeaderButton();
        header.IsFocused = true;

        scrollViewer.UnregisterStickyHeader(header);

        header.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void StickyHeader_ClearFocusedHeaders_KeepsFocus()
    {
        (ScrollViewer scrollViewer, Button header) = CreateStickyHeaderButton();
        header.IsFocused = true;

        scrollViewer.ClearStickyHeaders();

        header.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void StickyHeader_HideFocusedHeader_ClearsFocus()
    {
        (_, Button header) = CreateStickyHeaderButton();
        header.IsFocused = true;

        header.IsVisible = false;

        header.IsFocused.ShouldBeFalse();
    }

    #endregion

    #region ListBox rows

    private static ListBox CreateFocusedRowListBox()
    {
        ListBox listBox = new ListBox();
        listBox.AddToRoot();
        listBox.Items!.Add("A");
        listBox.Items.Add("B");
        listBox.SelectedIndex = 1;
        listBox.IsFocused = true;
        listBox.DoListItemsHaveFocus = true;
        return listBox;
    }

    [Fact]
    public void ListBox_ItemAddedAtStart_FocusedRowKeepsFocus()
    {
        ListBox listBox = CreateFocusedRowListBox();

        listBox.Items!.Insert(0, "New");

        listBox.IsFocused.ShouldBeTrue();
        listBox.DoListItemsHaveFocus.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(listBox);
    }

    [Fact]
    public void ListBox_ItemAddedAtEnd_GamepadStillMovesSelection()
    {
        ListBox listBox = CreateFocusedRowListBox();
        listBox.Items!.Add("C");
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);

        listBox.SelectedIndex.ShouldBe(2);
    }

    #endregion

    #region Menu

    private static (ContainerRuntime container, Menu menu, MenuItem file, MenuItem recent) CreateMenu()
    {
        ContainerRuntime container = new ContainerRuntime();
        container.Width = 400;
        container.WidthUnits = DimensionUnitType.Absolute;
        container.Height = 300;
        container.HeightUnits = DimensionUnitType.Absolute;
        container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        Gum.GumService.Default.Root.Children!.Add(container);

        Menu menu = new Menu();
        container.Children!.Add(menu.Visual);
        MenuItem file = new MenuItem { Header = "File" };
        menu.Items!.Add(file);
        file.Items!.Add(new MenuItem { Header = "New" });
        file.Items.Add(new MenuItem { Header = "Open" });
        MenuItem recent = new MenuItem { Header = "Recent" };
        file.Items.Add(recent);
        recent.Items!.Add(new MenuItem { Header = "One" });
        container.UpdateLayout();
        return (container, menu, file, recent);
    }

    [Fact]
    public void Menu_SubmenuPlacedAgainAfterMove_MenuKeepsFocusAndKeyboardStillNavigates()
    {
        (ContainerRuntime container, Menu menu, MenuItem file, _) = CreateMenu();
        menu.IsFocused = true;
        Press(Keys.Down);
        file.IsPopupVisible.ShouldBeTrue("precondition: Down opens the submenu");

        MenuItem newItem = (MenuItem)file.Items![0]!;
        newItem.IsHighlighted.ShouldBeTrue("precondition: New highlighted on open");
        container.X = 25;
        container.UpdateLayout();
        RunFrame();

        menu.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(menu);
        MenuItem open = (MenuItem)file.Items![1]!;
        Press(Keys.Down);
        open.IsHighlighted.ShouldBeTrue("Down still moves the highlight inside the submenu");
    }

    [Fact]
    public void Menu_NestedSubmenuOpensWithKeyboardThenEscapeClosesOneLevel()
    {
        (_, Menu menu, MenuItem file, MenuItem recent) = CreateMenu();
        menu.IsFocused = true;
        Press(Keys.Down);
        Press(Keys.Down);
        Press(Keys.Down);
        recent.IsHighlighted.ShouldBeTrue("precondition: the keyboard reached Recent");
        Press(Keys.Right);
        recent.IsPopupVisible.ShouldBeTrue("precondition: Right opens the nested submenu");

        menu.IsFocused.ShouldBeTrue();
        Press(Keys.Escape);

        recent.IsPopupVisible.ShouldBeFalse();
        file.IsPopupVisible.ShouldBeTrue();
        menu.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Menu_HideFocusedMenu_ClearsFocus()
    {
        (_, Menu menu, _, _) = CreateMenu();
        menu.IsFocused = true;

        menu.IsVisible = false;

        menu.IsFocused.ShouldBeFalse();
    }

    #endregion

    #region Window

    [Fact]
    public void Window_DragWithFocusedChild_KeepsFocusAndGamepadStillClicksChild()
    {
        Mock<ICursor> cursor = new();
        FrameworkElement.MainCursor = cursor.Object;
        Window window = new Window();
        window.AddToRoot();
        Button child = new Button();
        window.AddChild(child);
        child.IsFocused = true;
        int clicks = 0;
        child.Click += (_, _) => clicks++;
        InteractiveGue titleBar = (InteractiveGue)window.GetVisual("TitleBarInstance")!;

        titleBar.TryCallPush();
        titleBar.TryCallDragging();
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(30);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(40);
        titleBar.TryCallDragging();
        PadHarness pad = new();
        pad.Tap(Buttons.A);

        window.X.ShouldBe(30);
        child.IsFocused.ShouldBeTrue();
        clicks.ShouldBe(1);
    }

    [Fact]
    public void Window_HideWithFocusedChild_ClearsFocus()
    {
        Window window = new Window();
        window.AddToRoot();
        Button child = new Button();
        window.AddChild(child);
        child.IsFocused = true;

        window.IsVisible = false;

        child.IsFocused.ShouldBeFalse();
    }

    #endregion

    #region DialogBox

    [Fact]
    public void DialogBox_DismissFocused_ClearsFocusAndShowingAgainDoesNotRestoreIt()
    {
        DialogBox dialogBox = new DialogBox();
        dialogBox.AddToRoot();
        dialogBox.IsFocused = true;
        dialogBox.IsFocused.ShouldBeTrue("precondition");

        dialogBox.Dismiss();
        dialogBox.IsVisible = true;

        dialogBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    #endregion

    #region Tooltip

    [Fact]
    public void Tooltip_ShowAndHide_DoNotChangeFocusOfOwner()
    {
        Button owner = new Button();
        owner.AddToRoot();
        owner.IsFocused = true;
        Tooltip tooltip = new Tooltip();

        tooltip.Show(10, 10);
        owner.IsFocused.ShouldBeTrue();
        tooltip.Hide();

        owner.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(owner);
    }

    [Fact]
    public void Tooltip_ShownTwiceAfterHide_DoNotChangeFocusOfOwner()
    {
        Button owner = new Button();
        owner.AddToRoot();
        owner.IsFocused = true;
        Tooltip tooltip = new Tooltip();

        tooltip.Show(10, 10);
        tooltip.Hide();
        tooltip.Show(20, 20);

        owner.IsFocused.ShouldBeTrue();
    }

    #endregion
}
