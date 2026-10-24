using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using MonoGameGum.Tests.Input;
using Moq;
using RenderingLibrary;
using Shouldly;
using Xunit;
using Keys = Gum.Forms.Input.Keys;
using XnaButtons = Microsoft.Xna.Framework.Input.Buttons;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Keyboard and gamepad navigation of Menu and MenuItem, and closing on focus loss (#6696).
/// </summary>
public class MenuKeyboardNavigationTests : BaseTestClass
{
    public MenuKeyboardNavigationTests()
    {
        // A still cursor away from every control, so only the keyboard and gamepad change highlights.
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

    #region Helpers

    private class Bar
    {
        public ContainerRuntime Container = null!;
        public Menu Menu = null!;
        public MenuItem File = null!;
        public MenuItem NewItem = null!;
        public MenuItem Open = null!;
        public MenuItem Recent = null!;
        public MenuItem One = null!;
        public MenuItem Exit = null!;
        public MenuItem Edit = null!;
        public MenuItem Cut = null!;
        public MenuItem Standalone = null!;
        public Button After = null!;
    }

    // File (New, Open [disabled], Recent (One, Two), Exit), Edit (Cut, Copy), Standalone (no sub-items),
    // then a Button after the menu in the tab order.
    private static Bar CreateBar()
    {
        Bar bar = new();
        bar.Container = new ContainerRuntime();
        bar.Container.Width = 400;
        bar.Container.WidthUnits = DimensionUnitType.Absolute;
        bar.Container.Height = 300;
        bar.Container.HeightUnits = DimensionUnitType.Absolute;
        bar.Container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        Gum.GumService.Default.Root.Children!.Add(bar.Container);

        bar.Menu = new Menu();
        bar.Container.Children!.Add(bar.Menu.Visual);

        bar.File = new MenuItem { Header = "File" };
        bar.Menu.Items!.Add(bar.File);
        bar.NewItem = new MenuItem { Header = "New" };
        bar.File.Items!.Add(bar.NewItem);
        bar.Open = new MenuItem { Header = "Open", IsEnabled = false };
        bar.File.Items.Add(bar.Open);
        bar.Recent = new MenuItem { Header = "Recent" };
        bar.File.Items.Add(bar.Recent);
        bar.One = new MenuItem { Header = "One" };
        bar.Recent.Items!.Add(bar.One);
        bar.Recent.Items.Add(new MenuItem { Header = "Two" });
        bar.Exit = new MenuItem { Header = "Exit" };
        bar.File.Items.Add(bar.Exit);

        bar.Edit = new MenuItem { Header = "Edit" };
        bar.Menu.Items.Add(bar.Edit);
        bar.Cut = new MenuItem { Header = "Cut" };
        bar.Edit.Items!.Add(bar.Cut);
        bar.Edit.Items.Add(new MenuItem { Header = "Copy" });

        bar.Standalone = new MenuItem { Header = "Standalone" };
        bar.Menu.Items.Add(bar.Standalone);

        bar.After = new Button();
        bar.Container.Children.Add(bar.After.Visual);

        bar.Container.UpdateLayout();
        return bar;
    }

    private static Bar CreateFocusedBar()
    {
        Bar bar = CreateBar();
        bar.Menu.IsFocused = true;
        bar.Menu.IsFocused.ShouldBeTrue("precondition: the menu takes focus");
        return bar;
    }

    // Runs one frame of the real focus dispatch with only this key down (pushed, typed and held).
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
            Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
        }
        finally
        {
            FrameworkElement.KeyboardsForUiControl.Remove(keyboard.Object);
        }
    }

    // Runs one frame of the real focus dispatch with a gamepad that has just pressed the given buttons
    // and holds the left stick at the given position.
    private static void PressGamepad(XnaButtons buttons, float leftStickX = 0, float leftStickY = 0)
    {
        Gum.Input.GamePad gamepad = new();
        gamepad.Activity(
            new Microsoft.Xna.Framework.Input.GamePadState(
                new Microsoft.Xna.Framework.Vector2(leftStickX, leftStickY),
                Microsoft.Xna.Framework.Vector2.Zero,
                0, 0,
                buttons),
            0);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);
        try
        {
            Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
        }
        finally
        {
            FrameworkElement.GamePadsForUiControl.Remove(gamepad);
        }
    }

    private static void ShouldBeOnlyHighlighted(MenuItem expected, params MenuItem[] siblings)
    {
        expected.IsHighlighted.ShouldBeTrue($"{expected.Header} should be highlighted");
        foreach (MenuItem sibling in siblings)
        {
            if (sibling != expected)
            {
                sibling.IsHighlighted.ShouldBeFalse($"{sibling.Header} should not be highlighted");
            }
        }
    }

    #endregion

    #region Focus

    [Fact]
    public void Tab_FromPrecedingControl_FocusesMenuAndHighlightsFirstItem()
    {
        Button before = new Button();
        Gum.GumService.Default.Root.Children!.Add(before.Visual);
        Bar bar = CreateBar();
        before.IsFocused = true;

        Press(Keys.Tab);

        bar.Menu.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(bar.Menu);
        bar.File.IsHighlighted.ShouldBeTrue();
    }

    #endregion

    #region Top level

    [Fact]
    public void Right_MovesHighlightAcrossTopLevelItemsAndWraps()
    {
        Bar bar = CreateFocusedBar();

        Press(Keys.Right);
        ShouldBeOnlyHighlighted(bar.Edit, bar.File, bar.Standalone);

        Press(Keys.Right);
        Press(Keys.Right);
        ShouldBeOnlyHighlighted(bar.File, bar.Edit, bar.Standalone);
        bar.File.IsPopupVisible.ShouldBeFalse("moving the highlight does not open anything");
    }

    [Fact]
    public void Left_FromFirstTopLevelItem_WrapsToLast()
    {
        Bar bar = CreateFocusedBar();

        Press(Keys.Left);

        ShouldBeOnlyHighlighted(bar.Standalone, bar.File, bar.Edit);
    }

    [Theory]
    [InlineData(Keys.Enter)]
    [InlineData(Keys.Down)]
    public void OpenKey_OnTopLevelItem_OpensSubmenuAndHighlightsFirstChild(Keys key)
    {
        Bar bar = CreateFocusedBar();

        Press(key);

        bar.File.IsPopupVisible.ShouldBeTrue();
        bar.NewItem.IsHighlighted.ShouldBeTrue();
        bar.Menu.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Enter_OnTopLevelLeaf_ActivatesIt()
    {
        Bar bar = CreateFocusedBar();
        int clicks = 0;
        bar.Standalone.Clicked += (_, _) => clicks++;
        Press(Keys.Left);

        Press(Keys.Enter);

        clicks.ShouldBe(1);
        bar.Standalone.IsSelected.ShouldBeFalse();
        bar.Menu.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Down_OnTopLevelLeaf_DoesNotActivateIt()
    {
        Bar bar = CreateFocusedBar();
        int clicks = 0;
        bar.Standalone.Clicked += (_, _) => clicks++;
        Press(Keys.Left);

        Press(Keys.Down);

        clicks.ShouldBe(0);
    }

    #endregion

    #region Submenu

    [Fact]
    public void UpDown_InSubmenu_SkipDisabledItemsAndWrap()
    {
        Bar bar = CreateFocusedBar();
        Press(Keys.Down);

        Press(Keys.Down);
        ShouldBeOnlyHighlighted(bar.Recent, bar.NewItem, bar.Open, bar.Exit);

        Press(Keys.Down);
        Press(Keys.Down);
        ShouldBeOnlyHighlighted(bar.NewItem, bar.Open, bar.Recent, bar.Exit);

        Press(Keys.Up);
        ShouldBeOnlyHighlighted(bar.Exit, bar.NewItem, bar.Open, bar.Recent);
        bar.File.IsPopupVisible.ShouldBeTrue();
    }

    [Fact]
    public void HomeEnd_InSubmenu_JumpToFirstAndLast()
    {
        Bar bar = CreateFocusedBar();
        Press(Keys.Down);

        Press(Keys.End);
        ShouldBeOnlyHighlighted(bar.Exit, bar.NewItem, bar.Recent);

        Press(Keys.Home);
        ShouldBeOnlyHighlighted(bar.NewItem, bar.Recent, bar.Exit);
    }

    [Fact]
    public void Right_OnItemWithChildren_OpensChildSubmenu_LeftClosesOneLevel()
    {
        Bar bar = CreateFocusedBar();
        Press(Keys.Down);
        Press(Keys.Down);

        Press(Keys.Right);
        bar.Recent.IsPopupVisible.ShouldBeTrue();
        bar.One.IsHighlighted.ShouldBeTrue();

        Press(Keys.Left);
        bar.Recent.IsPopupVisible.ShouldBeFalse();
        bar.File.IsPopupVisible.ShouldBeTrue();
        bar.Recent.IsHighlighted.ShouldBeTrue();
    }

    [Fact]
    public void LeftRight_InTopLevelSubmenu_MoveToNeighborTopLevelItemAndOpenIt()
    {
        Bar bar = CreateFocusedBar();
        Press(Keys.Down);

        Press(Keys.Right);
        bar.File.IsPopupVisible.ShouldBeFalse();
        bar.Edit.IsPopupVisible.ShouldBeTrue();
        bar.Cut.IsHighlighted.ShouldBeTrue();
        ShouldBeOnlyHighlighted(bar.Edit, bar.File, bar.Standalone);

        Press(Keys.Left);
        bar.Edit.IsPopupVisible.ShouldBeFalse();
        bar.File.IsPopupVisible.ShouldBeTrue();
        bar.NewItem.IsHighlighted.ShouldBeTrue();
    }

    [Fact]
    public void Enter_OnSubmenuLeaf_ActivatesClosesMenuAndKeepsFocus()
    {
        Bar bar = CreateFocusedBar();
        int clicks = 0;
        int selections = 0;
        bar.Exit.Clicked += (_, _) => clicks++;
        bar.Exit.Selected += (_, _) => selections++;
        Press(Keys.Down);
        Press(Keys.End);

        Press(Keys.Enter);

        clicks.ShouldBe(1);
        selections.ShouldBe(1);
        bar.File.IsPopupVisible.ShouldBeFalse();
        bar.File.IsSelected.ShouldBeFalse();
        bar.Menu.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(bar.Menu);
        bar.File.IsHighlighted.ShouldBeTrue();
    }

    [Fact]
    public void Escape_ClosesOneLevelAtATime()
    {
        Bar bar = CreateFocusedBar();
        Press(Keys.Down);
        Press(Keys.Down);
        Press(Keys.Right);

        Press(Keys.Escape);
        bar.Recent.IsPopupVisible.ShouldBeFalse();
        bar.File.IsPopupVisible.ShouldBeTrue();

        Press(Keys.Escape);
        bar.File.IsPopupVisible.ShouldBeFalse();
        bar.File.IsHighlighted.ShouldBeTrue();
        bar.Menu.IsFocused.ShouldBeTrue();
    }

    #endregion

    #region Gamepad

    [Fact]
    public void Gamepad_DPadAndLeftStickMirrorArrows_AOpens_BCloses()
    {
        Bar bar = CreateFocusedBar();

        PressGamepad(XnaButtons.DPadRight);
        ShouldBeOnlyHighlighted(bar.Edit, bar.File, bar.Standalone);

        PressGamepad(0, leftStickX: -1);
        ShouldBeOnlyHighlighted(bar.File, bar.Edit, bar.Standalone);

        PressGamepad(XnaButtons.A);
        bar.File.IsPopupVisible.ShouldBeTrue();
        bar.NewItem.IsHighlighted.ShouldBeTrue();

        PressGamepad(0, leftStickY: -1);
        bar.Recent.IsHighlighted.ShouldBeTrue();

        PressGamepad(XnaButtons.DPadDown);
        bar.Exit.IsHighlighted.ShouldBeTrue();

        PressGamepad(XnaButtons.B);
        bar.File.IsPopupVisible.ShouldBeFalse();
        bar.Menu.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Gamepad_AOnSubmenuLeaf_Activates()
    {
        Bar bar = CreateFocusedBar();
        int clicks = 0;
        bar.NewItem.Clicked += (_, _) => clicks++;
        PressGamepad(XnaButtons.A);

        PressGamepad(XnaButtons.A);

        clicks.ShouldBe(1);
        bar.File.IsPopupVisible.ShouldBeFalse();
    }

    [Fact]
    public void Gamepad_UpWithNothingOpen_MovesFocusOutOfTheMenu()
    {
        Bar bar = CreateFocusedBar();

        PressGamepad(XnaButtons.DPadUp);

        bar.Menu.IsFocused.ShouldBeFalse();
    }

    #endregion

    #region Focus loss

    [Fact]
    public void Tab_WithSubmenuOpen_MovesFocusAndClosesEverySubmenu()
    {
        Bar bar = CreateFocusedBar();
        Press(Keys.Down);
        Press(Keys.Down);
        Press(Keys.Right);

        Press(Keys.Tab);

        bar.After.IsFocused.ShouldBeTrue();
        bar.Menu.IsFocused.ShouldBeFalse();
        bar.File.IsPopupVisible.ShouldBeFalse();
        bar.Recent.IsPopupVisible.ShouldBeFalse();
        bar.File.IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void AnotherControlFocused_WithSubmenuOpen_ClosesEverySubmenu()
    {
        Bar bar = CreateFocusedBar();
        Press(Keys.Down);

        bar.After.IsFocused = true;

        bar.Menu.IsFocused.ShouldBeFalse();
        bar.File.IsPopupVisible.ShouldBeFalse();
        FrameworkElement.PopupRoot.Children!.Count.ShouldBe(0);
    }

    [Fact]
    public void ClickElsewhere_AfterKeyboardOpen_ClosesTheMenu()
    {
        Bar bar = CreateFocusedBar();
        Press(Keys.Down);
        bar.File.IsPopupVisible.ShouldBeTrue("precondition: Down opens the submenu");

        // The cursor rests in the window away from the menu for a frame, then pushes and releases there.
        // Like the mouse tests' PushOn(null), but with the idle frame first: a next-push action queued
        // this frame only becomes runnable on a later in-window frame, which the off-screen test cursor
        // never gives it.
        float x = GraphicalUiElement.CanvasWidth - 1;
        float y = GraphicalUiElement.CanvasHeight - 1;
        bool isPush = false;
        bool isDown = false;
        bool isClick = false;
        Mock<ICursor> cursor = new();
        cursor.SetupProperty(c => c.VisualOver);
        cursor.SetupProperty(c => c.WindowPushed);
        cursor.Setup(c => c.PrimaryPush).Returns(() => isPush);
        cursor.Setup(c => c.PrimaryDown).Returns(() => isDown);
        cursor.Setup(c => c.PrimaryClick).Returns(() => isClick);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(c => c.LastPrimaryPushTime).Returns(5);
        cursor.Setup(c => c.X).Returns((int)x);
        cursor.Setup(c => c.Y).Returns((int)y);
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(x);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(y);
        Gum.Forms.FormsUtilities.SetCursor(cursor.Object);
        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        isPush = true;
        isDown = true;
        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
        isPush = false;
        isDown = false;
        isClick = true;
        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        bar.File.IsPopupVisible.ShouldBeFalse();
        bar.File.IsSelected.ShouldBeFalse();
    }

    #endregion

    #region Nothing open

    [Fact]
    public void EscapeOrB_WithNothingOpen_KeepsFocusAndHighlight()
    {
        Bar bar = CreateFocusedBar();
        Press(Keys.Right);

        Press(Keys.Escape);
        PressGamepad(XnaButtons.B);

        bar.Menu.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(bar.Menu);
        ShouldBeOnlyHighlighted(bar.Edit, bar.File, bar.Standalone);
        bar.Edit.IsPopupVisible.ShouldBeFalse();
    }

    #endregion

    #region Default visual states

    // Like WPF, a focused Menu shows focus only through its highlighted top-level item (#6706).
    [Fact]
    public void MenuVisual_Focus_ChangesNothingOnTheMenuItself_AndHighlightsFirstItem()
    {
        Bar bar = CreateBar();
        Gum.Forms.DefaultVisuals.V3.MenuVisual visual = (Gum.Forms.DefaultVisuals.V3.MenuVisual)bar.Menu.Visual;
        visual.MenuCategory.States.Select(s => s.Name).ShouldBe(
            new[] { "Enabled", "Disabled", "DisabledFocused", "Focused" }, ignoreOrder: true);
        string unfocused = MenuChromeVisibility(visual);

        bar.Menu.IsFocused = true;

        MenuChromeVisibility(visual).ShouldBe(unfocused);
        ShouldBeOnlyHighlighted(bar.File, bar.Edit, bar.Standalone);

        bar.Menu.Visual.SetProperty(Menu.MenuCategoryStateName, FrameworkElement.DisabledFocusedStateName);
        MenuChromeVisibility(visual).ShouldBe(unfocused);
    }

    // The visibility of every element of the menu's own chrome, leaving out its items.
    private static string MenuChromeVisibility(GraphicalUiElement menuVisual)
    {
        List<string> parts = new();
        void Walk(GraphicalUiElement element)
        {
            foreach (GraphicalUiElement child in element.Children!)
            {
                if ((child as InteractiveGue)?.FormsControlAsObject is MenuItem)
                {
                    continue;
                }
                parts.Add($"{child.Name}={child.Visible}");
                Walk(child);
            }
        }
        Walk(menuVisual);
        return string.Join(", ", parts);
    }

    #endregion

    #region Shared highlight

    [Fact]
    public void MouseHover_TopLevelWithoutKeyboard_ClearsSiblingHighlight()
    {
        Bar bar = CreateBar();
        bar.File.IsHighlighted = true;

        bar.Edit.IsHighlighted = true;

        ShouldBeOnlyHighlighted(bar.Edit, bar.File, bar.Standalone);
        bar.Edit.IsPopupVisible.ShouldBeFalse();
    }

    [Fact]
    public void MouseHover_AfterKeyboardHighlight_LeavesOneHighlight_AndArrowsContinueFromIt()
    {
        Bar bar = CreateFocusedBar();
        bar.File.IsHighlighted.ShouldBeTrue("precondition");

        bar.Edit.IsHighlighted = true;
        ShouldBeOnlyHighlighted(bar.Edit, bar.File, bar.Standalone);

        Press(Keys.Right);
        ShouldBeOnlyHighlighted(bar.Standalone, bar.File, bar.Edit);
    }

    [Fact]
    public void MouseHover_InSubmenu_LeavesOneHighlight()
    {
        Bar bar = CreateFocusedBar();
        Press(Keys.Down);

        bar.Exit.IsHighlighted = true;

        ShouldBeOnlyHighlighted(bar.Exit, bar.NewItem, bar.Recent);
        Press(Keys.Up);
        ShouldBeOnlyHighlighted(bar.Recent, bar.NewItem, bar.Exit);
    }

    #endregion
}
