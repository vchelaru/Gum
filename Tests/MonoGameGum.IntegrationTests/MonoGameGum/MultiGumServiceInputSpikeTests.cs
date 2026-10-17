#pragma warning disable CS0618 // WindowOver / WindowPushed are the obsolete aliases of VisualOver / VisualPushed; the spike checks both
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Converters;
using Gum.Input;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGameAndGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ToolsUtilities;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;
using GumKeys = Gum.Forms.Input.Keys;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Third spike for the epic on running several GumService instances in one process (#6066):
/// input and interaction. Two services (Bubblegum = A, Hazard = B) are built the same way as in the
/// themes spike and driven through <see cref="Gum.GumService.Update(GameTime)"/>, each with its own
/// scripted cursor and keyboard. The tests assert the behavior we want, so a red test marks a collision
/// between the two services (its message says which static or queue is shared). Each test names what it
/// expects in its summary.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class MultiGumServiceInputSpikeTests : BaseTestClass, IClassFixture<ThemeProjectFolders>
{
    private const int CaptureWidth = 1024;
    private const int CaptureHeight = 768;
    private const double FrameSeconds = 1.0 / 60.0;

    private static readonly XnaColor ClearColor = XnaColor.CornflowerBlue;

    private readonly ThemeProjectFolders _themeFolders;

    public MultiGumServiceInputSpikeTests(ThemeProjectFolders themeFolders)
    {
        _themeFolders = themeFolders;
    }

    public override void Dispose()
    {
        base.Dispose();
        GraphicalUiElement.IsAllLayoutSuspended = false;
    }

    #region Update drives each service's own cursor and keyboard

    // Each Update advances only its own service's cursor and polls only its own keyboard.
    [Fact]
    public void Update_ShouldDriveOnlyThatServicesCursorAndKeyboard()
    {
        using InputGame game = CreateGame();

        // CreateGame already ticked both services once with their cursors parked at (-50,-50).
        int aPollsBefore = game.A.Keyboard.PollCount;
        int bPollsBefore = game.B.Keyboard.PollCount;
        game.A.Cursor.Place(100, 120, leftDown: false);
        game.B.Cursor.Place(500, 400, leftDown: false);

        Tick(game, Side.A);
        game.A.Cursor.X.ShouldBe(100, "A's Update should advance A's cursor");
        game.A.Cursor.Y.ShouldBe(120);
        game.B.Cursor.X.ShouldBe(-50, "A's Update must not advance B's cursor");
        game.A.Keyboard.PollCount.ShouldBe(aPollsBefore + 1, "A's Update should poll A's keyboard once");
        game.B.Keyboard.PollCount.ShouldBe(bPollsBefore, "A's Update must not poll B's keyboard");

        Tick(game, Side.B);
        game.B.Cursor.X.ShouldBe(500, "B's Update should advance B's cursor");
        game.B.Cursor.Y.ShouldBe(400);
        game.A.Cursor.X.ShouldBe(100, "B's Update must not move A's cursor");
        game.A.Keyboard.PollCount.ShouldBe(aPollsBefore + 1, "B's Update must not poll A's keyboard");
        game.B.Keyboard.PollCount.ShouldBe(bPollsBefore + 1, "B's Update should poll B's keyboard once");
    }

    // B is the default when A updates, so Update has to switch to A itself.
    [Fact]
    public void Update_WhenAnotherServiceIsTheDefault_ShouldDriveTheUpdatedServicesOwnCursor()
    {
        using InputGame game = CreateGame();

        // B is the default now (CreateGame ended on B's setup).
        game.A.Cursor.Place(100, 120, leftDown: false);
        game.B.Cursor.Place(500, 400, leftDown: false);

        TickWithoutActivating(game, Side.A);

        game.A.Cursor.X.ShouldBe(100,
            "serviceA.Update(...) should drive A's cursor even though B is the default: " +
            $"A's cursor X={game.A.Cursor.X}, B's cursor X={game.B.Cursor.X} (B's was driven instead).");
    }

    // GumService.Cursor/Keyboard/PopupRoot/ModalRoot must answer for the instance they are called on.
    [Fact]
    public void ServiceInstanceProperties_ShouldDescribeThatInstance_WhileAnotherServiceIsTheDefault()
    {
        using InputGame game = CreateGame();
        Activate(game, Side.B);

        List<string> leaks = new List<string>();
        if (!ReferenceEquals(game.A.Service.Cursor, game.A.Cursor)) { leaks.Add("serviceA.Cursor returned " + Describe(game.A.Service.Cursor, game)); }
        if (!ReferenceEquals(game.A.Service.Keyboard, null) && !ReferenceEquals(game.A.Service.Keyboard, game.A.Keyboard)) { leaks.Add("serviceA.Keyboard returned another keyboard"); }
        if (!ReferenceEquals(game.A.Service.PopupRoot, game.A.PopupRoot)) { leaks.Add("serviceA.PopupRoot returned B's popup root"); }
        if (!ReferenceEquals(game.A.Service.ModalRoot, game.A.ModalRoot)) { leaks.Add("serviceA.ModalRoot returned B's modal root"); }

        leaks.ShouldBeEmpty("serviceA's per-instance properties answer for whichever service is the default: " + string.Join("; ", leaks));
    }

    // One GamePad[4] per service.
    [Fact]
    public void Gamepads_ShouldBeSeparateArraysPerService()
    {
        using InputGame game = CreateGame();

        Activate(game, Side.A);
        Gum.Input.GamePad[] gamepadsSeenByA = FormsUtilities.Gamepads;
        Activate(game, Side.B);
        Gum.Input.GamePad[] gamepadsSeenByB = FormsUtilities.Gamepads;

        gamepadsSeenByA.ShouldNotBeSameAs(gamepadsSeenByB,
            "FormsUtilities.Gamepads is one array for the process, so both services advance the same GamePad objects " +
            "(and B's InitializeDefaults replaced A's elements in place).");
    }

    #endregion

    #region Clicks and hover with overlapping screens

    // Green: each service has its own cursor instance, so a click by A's cursor reaches only A's tree even
    // though B's identical button sits under the same pixels.
    [Theory]
    [InlineData(Side.A)]
    [InlineData(Side.B)]
    public void Click_OverCoincidentButtons_ShouldFireOnlyTheClickedServicesButton(Side clicker)
    {
        using InputGame game = CreateGame(coincidentConfirmButtons: true);
        Point target = game.ConfirmCenter(clicker);
        InputService other = game.Get(Other(clicker));
        InputService clicking = game.Get(clicker);

        game.ConfirmCenter(Side.A).ShouldBe(game.ConfirmCenter(Side.B), "setup: the two Confirm buttons should sit on the same pixels");

        ClickWithBothServicesUpdating(game, clicker, target);

        clicking.ConfirmClicks.ShouldBe(1, $"{clicker}'s own button should have been clicked once");
        other.ConfirmClicks.ShouldBe(0, $"{Other(clicker)}'s button sits under the same pixels but its cursor was elsewhere, so it must not fire");

        // and the other one afterwards:
        ClickWithBothServicesUpdating(game, Other(clicker), target);
        clicking.ConfirmClicks.ShouldBe(1);
        other.ConfirmClicks.ShouldBe(1);
    }

    // A click handler runs inside its service's Update, which makes that service the default for the call.
    [Theory]
    [InlineData(Side.A)]
    [InlineData(Side.B)]
    public void Click_HandlerFiredInsideUpdate_ShouldSeeItsOwnServiceAsDefault(Side clicker)
    {
        using InputGame game = CreateGame(coincidentConfirmButtons: true);
        Point target = game.ConfirmCenter(clicker);
        InputService clicking = game.Get(clicker);
        List<IGumService?> seenByHandler = new List<IGumService?>();
        clicking.ConfirmButton.Click += (_, _) => seenByHandler.Add(IGumService.Default);

        // The default is the other service whenever this one's Update starts.
        for (int frame = 0; frame < 3; frame++)
        {
            clicking.Cursor.Place(target.X, target.Y, leftDown: frame == 1);
            game.Get(Other(clicker)).Cursor.Place(-50, -50, leftDown: false);
            IGumService.Default = game.Get(Other(clicker)).Service;
            TickWithoutActivating(game, clicker);
            IGumService.Default.ShouldBeSameAs(game.Get(Other(clicker)).Service, "Update puts the previous default back");
        }

        seenByHandler.Count.ShouldBe(1);
        seenByHandler[0].ShouldBeSameAs(clicking.Service);
    }

    // The same click, but B is the default when A updates.
    [Fact]
    public void Click_InA_WhenBIsTheDefault_ShouldStillFireA()
    {
        using InputGame game = CreateGame(coincidentConfirmButtons: true);
        Point target = game.ConfirmCenter(Side.A);

        // B is the default. A's cursor is at the button, B's is parked.
        game.B.Cursor.Place(-50, -50, leftDown: false);
        for (int frame = 0; frame < 3; frame++)
        {
            game.A.Cursor.Place(target.X, target.Y, leftDown: frame == 1);
            TickWithoutActivating(game, Side.A);
        }

        game.A.ConfirmClicks.ShouldBe(1,
            "A's cursor clicked A's button, but A's Update used FormsUtilities.Cursor (B's, parked off screen) and so saw no click.");
    }

    // Green: VisualOver / WindowOver are per cursor instance, so each service's cursor reports a visual from its own tree.
    [Theory]
    [InlineData(Side.A)]
    [InlineData(Side.B)]
    public void VisualOver_WhenScreensOverlap_ShouldBeFromEachServicesOwnTree(Side updatesFirst)
    {
        using InputGame game = CreateGame(coincidentConfirmButtons: true);
        Point target = game.ConfirmCenter(Side.A);

        game.A.Cursor.Place(target.X, target.Y, leftDown: false);
        game.B.Cursor.Place(target.X, target.Y, leftDown: false);

        // one frame to settle, then the frame we assert on:
        TickBoth(game, updatesFirst);
        TickBoth(game, updatesFirst);

        InteractiveGue? overA = game.A.Cursor.VisualOver;
        InteractiveGue? overB = game.B.Cursor.VisualOver;

        overA.ShouldNotBeNull("A's cursor should be over something in A's tree");
        overB.ShouldNotBeNull("B's cursor should be over something in B's tree");
        IsInTree(overA!, game.A.Screen).ShouldBeTrue("A's cursor.VisualOver should belong to A's screen");
        IsInTree(overB!, game.B.Screen).ShouldBeTrue("B's cursor.VisualOver should belong to B's screen");
        IsInTree(overA!, game.B.Screen).ShouldBeFalse("A's cursor must never report a visual from B's tree");
        IsInTree(overB!, game.A.Screen).ShouldBeFalse("B's cursor must never report a visual from A's tree");
        game.A.Cursor.WindowOver.ShouldBeSameAs(overA, "the obsolete WindowOver alias should match VisualOver");
        game.B.Cursor.WindowOver.ShouldBeSameAs(overB);
    }

    #endregion

    #region Typing and focus

    // Green: A's TextBox is clicked and typed into with A's keyboard while B has nothing focused.
    [Fact]
    public void TypingInA_WhileBHasNothingFocused_ShouldChangeOnlyATextBox()
    {
        using InputGame game = CreateGame();
        string textBefore = game.A.TextBox.Text ?? "";
        string textBBefore = game.B.TextBox.Text ?? "";

        FocusByClick(game, Side.A);
        game.A.TextBox.IsFocused.ShouldBeTrue("clicking A's TextBox with A's cursor should focus it");
        game.B.TextBox.IsFocused.ShouldBeFalse();

        game.A.Keyboard.Type("hi");
        Tick(game, Side.A);
        Tick(game, Side.B);

        game.A.TextBox.Text.ShouldBe(textBefore + "hi", "A's keyboard text should reach A's focused TextBox");
        game.B.TextBox.Text.ShouldBe(textBBefore, "B's TextBox must be untouched");
        game.B.TextBox.IsFocused.ShouldBeFalse();
    }

    // Green: the mirror of the above, so the result is not a quirk of A being first.
    [Fact]
    public void TypingInB_WhileAHasNothingFocused_ShouldChangeOnlyBTextBox()
    {
        using InputGame game = CreateGame();
        string textBefore = game.B.TextBox.Text ?? "";
        string textABefore = game.A.TextBox.Text ?? "";

        FocusByClick(game, Side.B);
        game.B.TextBox.IsFocused.ShouldBeTrue("clicking B's TextBox with B's cursor should focus it");

        game.B.Keyboard.Type("yo");
        Tick(game, Side.A);
        Tick(game, Side.B);

        game.B.TextBox.Text.ShouldBe(textBefore + "yo", "B's keyboard text should reach B's focused TextBox");
        game.A.TextBox.Text.ShouldBe(textABefore, "A's TextBox must be untouched");
    }

    // Keyboard text from B's Update must not reach a control focused in A (the focus slot is per service).
    [Fact]
    public void KeyboardOfB_ShouldNotTypeIntoTextBoxFocusedInA()
    {
        using InputGame game = CreateGame();
        string textBefore = game.A.TextBox.Text ?? "";

        FocusByClick(game, Side.A);
        game.A.TextBox.IsFocused.ShouldBeTrue("setup: A's TextBox is focused");

        game.B.Keyboard.Type("zz");
        Tick(game, Side.B);

        game.A.TextBox.Text.ShouldBe(textBefore,
            "B's Update dispatched B's keyboard to InteractiveGue.CurrentInputReceiver, which is A's focused TextBox (one focus slot for the process).");
    }

    // Focusing a control in B must not take focus away from the focused control in A.
    [Fact]
    public void FocusingTextBoxInB_ShouldNotUnfocusTextBoxInA()
    {
        using InputGame game = CreateGame();

        Activate(game, Side.A);
        game.A.TextBox.IsFocused = true;
        Activate(game, Side.B);
        game.B.TextBox.IsFocused = true;

        game.A.TextBox.IsFocused.ShouldBeTrue(
            "each service has its own ActiveInputReceiver, so B focusing its TextBox should leave A's focus alone; " +
            "InteractiveGue.CurrentInputReceiver is one static, so setting it called OnLoseFocus on A's TextBox.");
        game.B.TextBox.IsFocused.ShouldBeTrue();
    }

    // Making another service the default is a context switch, not a user focus change.
    [Fact]
    public void SwitchingActiveService_ShouldNotRaiseFocusEventsOnTheOtherServicesControls()
    {
        using InputGame game = CreateGame();
        int aGot = 0;
        int aLost = 0;

        Activate(game, Side.A);
        game.A.TextBox.GotFocus += (_, _) => aGot++;
        game.A.TextBox.LostFocus += (_, _) => aLost++;
        game.A.TextBox.IsFocused = true;
        aGot.ShouldBe(1, "setup: focusing A's TextBox raises GotFocus once");

        Activate(game, Side.B);
        Activate(game, Side.A);

        aLost.ShouldBe(0, "switching which service is the default is a context switch, not a user focus change, but it " +
            "raised LostFocus on A's TextBox when B became the default");
        aGot.ShouldBe(1, "and GotFocus again when A became current");
    }

    #endregion

    #region Popup root and modal root

    // Green: with A the default, the ComboBox dropdown lands in A's PopupRoot and draws only in A's render.
    [Fact]
    public void ComboBoxDropDown_OpenedInA_ShouldLandInAPopupRootAndDrawOnlyInA()
    {
        using InputGame game = CreateGame();

        Activate(game, Side.A);
        ComboBox combo = CreateComboBox(game, Side.A);
        InteractiveGue popupRootA = game.A.PopupRoot!;
        InteractiveGue popupRootB = game.B.PopupRoot!;
        popupRootA.ShouldNotBeSameAs(popupRootB, "setup: each service has its own PopupRoot");
        Tick(game, Side.A);

        Pixels beforeA = DrawSolo(game, Side.A);
        Pixels beforeB = DrawSolo(game, Side.B);

        Activate(game, Side.A);
        combo.IsDropDownOpen = true;
        Tick(game, Side.A);
        Tick(game, Side.B);

        popupRootA.Children.Count.ShouldBe(1, "the dropdown should be in A's PopupRoot");
        popupRootB.Children.Count.ShouldBe(0, "B's PopupRoot must stay empty");

        GraphicalUiElement popup = popupRootA.Children[0];
        Rectangle box = Box(popup);
        Pixels afterA = DrawSolo(game, Side.A);
        Pixels afterB = DrawSolo(game, Side.B);

        beforeA.CountDifferences(afterA, box).ShouldBeGreaterThan(100, $"the open dropdown should draw over A's content within {box}");
        beforeB.CountDifferences(afterB, null).ShouldBe(0, "B's render must be identical with A's dropdown open");
    }

    // With A made the default first, as the rule says, A's dropdown lands in A's PopupRoot.
    [Fact]
    public void ComboBoxDropDown_OpenedAfterMakingItsServiceTheDefault_ShouldLandInItsPopupRoot()
    {
        using InputGame game = CreateGame();

        Activate(game, Side.A);
        ComboBox combo = CreateComboBox(game, Side.A);
        Activate(game, Side.B);
        Activate(game, Side.A);

        combo.IsDropDownOpen = true;

        game.A.PopupRoot!.Children.Count.ShouldBe(1);
        game.B.PopupRoot!.Children.Count.ShouldBe(0);
    }

    // The 'close on next push' action must not sit in a queue that B's Update also drains.
    [Fact]
    public void ClickInB_ShouldNotCloseDropDownOpenInA()
    {
        using InputGame game = CreateGame();

        Activate(game, Side.A);
        ComboBox combo = CreateComboBox(game, Side.A);
        combo.IsDropDownOpen = true;
        combo.IsDropDownOpen.ShouldBeTrue("setup: A's dropdown is open");
        Tick(game, Side.A);

        // A's cursor stays parked off screen; the user clicks somewhere in B.
        Point inB = game.ConfirmCenter(Side.B);
        game.A.Cursor.Place(-50, -50, leftDown: false);

        // control: B's cursor hovering (no push) for a few frames leaves the dropdown alone
        for (int frame = 0; frame < 3; frame++)
        {
            game.B.Cursor.Place(inB.X, inB.Y, leftDown: false);
            Tick(game, Side.A);
            Tick(game, Side.B);
        }
        combo.IsDropDownOpen.ShouldBeTrue("control: hovering in B without a push should not close A's dropdown");

        for (int frame = 0; frame < 3; frame++)
        {
            game.B.Cursor.Place(inB.X, inB.Y, leftDown: frame == 1);
            Tick(game, Side.A);
            Tick(game, Side.B);
        }

        combo.IsDropDownOpen.ShouldBeTrue(
            "a click inside service B should not dismiss the dropdown of service A; ComboBox queued TryHideFromPush with " +
            "InteractiveGue.AddNextPushAction (one process-wide queue) and B's push ran it with B's cursor.");
    }

    // Green: a Window put in A's ModalRoot is exclusive to A; B keeps receiving input and draws unchanged.
    [Fact]
    public void ModalWindow_AddedInA_ShouldLandInAModalRootBlockOnlyAAndDrawOnlyInA()
    {
        using InputGame game = CreateGame(coincidentConfirmButtons: true);
        Point target = game.ConfirmCenter(Side.A);

        Pixels beforeA = DrawSolo(game, Side.A);
        Pixels beforeB = DrawSolo(game, Side.B);

        Activate(game, Side.A);
        Gum.Forms.Window window = new Gum.Forms.Window();
        window.Visual.XUnits = GeneralUnitType.PixelsFromSmall;
        window.Visual.YUnits = GeneralUnitType.PixelsFromSmall;
        window.Visual.X = 20;
        window.Visual.Y = 600;
        window.Visual.Width = 200;
        window.Visual.Height = 100;
        FrameworkElement.ModalRoot.Children.Add(window.Visual);

        InteractiveGue modalRootA = game.A.ModalRoot!;
        InteractiveGue modalRootB = game.B.ModalRoot!;
        modalRootA.ShouldNotBeSameAs(modalRootB, "setup: each service has its own ModalRoot");
        modalRootA.Children.ShouldContain(window.Visual, "the window should be in A's ModalRoot");
        modalRootB.Children.Count.ShouldBe(0, "B's ModalRoot must stay empty");

        Tick(game, Side.A);
        Tick(game, Side.B);
        Pixels afterA = DrawSolo(game, Side.A);
        Pixels afterB = DrawSolo(game, Side.B);
        beforeA.CountDifferences(afterA, Box(window.Visual)).ShouldBeGreaterThan(100, "the modal window should draw on A");
        beforeB.CountDifferences(afterB, null).ShouldBe(0, "B's render must be identical with A's modal open");

        // A's modal blocks A's own button even though A's cursor is right over it; B's button still works.
        ClickWithBothServicesUpdating(game, Side.A, target);
        game.A.ConfirmClicks.ShouldBe(0, "a modal in A should block clicks on A's other controls");
        ClickWithBothServicesUpdating(game, Side.B, target);
        game.B.ConfirmClicks.ShouldBe(1, "A's modal must not block B");
    }

    #endregion

    #region Helpers: rig

    public enum Side { A, B }

    private static Side Other(Side side) => side == Side.A ? Side.B : Side.A;

    private InputGame CreateGame(bool coincidentConfirmButtons = false)
    {
        string pathA = _themeFolders.GetGumxPath("Bubblegum");
        string pathB = _themeFolders.GetGumxPath("Hazard");
        InputGame game = new InputGame(pathA, pathB);
        game.Initialize();

        // Each screen is built with its own service's statics live, like the themes spike.
        BuildScreen(game, Side.A, "Bubblegum");
        BuildScreen(game, Side.B, "Hazard");

        if (coincidentConfirmButtons)
        {
            // Move B's screen so its Confirm button lands exactly on A's. Then only the cursor's identity
            // decides who gets an event at that spot.
            Point a = game.ConfirmCenter(Side.A);
            Point b = game.ConfirmCenter(Side.B);
            Activate(game, Side.B);
            game.B.Screen.X += a.X - b.X;
            game.B.Screen.Y += a.Y - b.Y;
            game.B.Screen.UpdateLayout();
        }

        // Settle both services (first-frame work), leaving every cursor parked off screen.
        game.A.Cursor.Place(-50, -50, leftDown: false);
        game.B.Cursor.Place(-50, -50, leftDown: false);
        Tick(game, Side.A);
        Tick(game, Side.B);
        return game;
    }

    private static void BuildScreen(InputGame game, Side side, string theme)
    {
        Activate(game, side);
        InputService service = game.Get(side);
        ScreenSave screenSave = service.Project.Screens.First(item => item.Name == theme + "/DemoScreenGum");
        GraphicalUiElement screen = screenSave.ToGraphicalUiElement(service.Service.SystemManagers, addToManagers: false);
        service.Service.Root.Children.Add(screen);
        screen.UpdateLayout();

        service.Screen = screen;
        service.ConfirmButton = ((screen.GetGraphicalUiElementByName("ButtonConfirmInstance") as InteractiveGue)?.FormsControlAsObject as Button)
            .ShouldNotBeNull($"{theme} demo screen should contain a real Forms Button named ButtonConfirmInstance");
        service.TextBox = ((screen.GetGraphicalUiElementByName("TextBoxInstance") as InteractiveGue)?.FormsControlAsObject as TextBox)
            .ShouldNotBeNull($"{theme} demo screen should contain a real Forms TextBox named TextBoxInstance");
        service.ConfirmButton.Click += (_, _) => service.ConfirmClicks++;
    }

    /// <summary>Makes the side's service the default; it stays the default until the next call.</summary>
    private static void Activate(InputGame game, Side side)
    {
        global::RenderingLibrary.IGumService.Default = game.Get(side).Service;
    }

    /// <summary>Makes the side's service the default, then updates it.</summary>
    private static void Tick(InputGame game, Side side)
    {
        Activate(game, side);
        TickWithoutActivating(game, side);
    }

    /// <summary>Updates the side's service and leaves the default alone, so Update has to switch itself.</summary>
    private static void TickWithoutActivating(InputGame game, Side side)
    {
        game.Seconds += FrameSeconds;
        GameTime gameTime = new GameTime(TimeSpan.FromSeconds(game.Seconds), TimeSpan.FromSeconds(FrameSeconds));
        game.Get(side).Service.Update(gameTime);
    }

    private static void TickBoth(InputGame game, Side first)
    {
        Tick(game, first);
        Tick(game, Other(first));
    }

    /// <summary>
    /// Three frames of a left click at <paramref name="target"/> by <paramref name="clicker"/>'s cursor while the
    /// other service's cursor stays off screen. Both services Update every frame, A first.
    /// </summary>
    private static void ClickWithBothServicesUpdating(InputGame game, Side clicker, Point target)
    {
        InputService other = game.Get(Other(clicker));
        other.Cursor.Place(-50, -50, leftDown: false);
        for (int frame = 0; frame < 3; frame++)
        {
            game.Get(clicker).Cursor.Place(target.X, target.Y, leftDown: frame == 1);
            Tick(game, Side.A);
            Tick(game, Side.B);
        }
        // park it again
        game.Get(clicker).Cursor.Place(-50, -50, leftDown: false);
        Tick(game, Side.A);
        Tick(game, Side.B);
    }

    private static void FocusByClick(InputGame game, Side side)
    {
        InputService service = game.Get(side);
        Point center = Center(service.TextBox.Visual);
        game.Get(Other(side)).Cursor.Place(-50, -50, leftDown: false);
        for (int frame = 0; frame < 3; frame++)
        {
            service.Cursor.Place(center.X, center.Y, leftDown: frame == 1);
            Tick(game, side);
        }
    }

    private static ComboBox CreateComboBox(InputGame game, Side side)
    {
        ComboBox combo = new ComboBox();
        combo.Items!.Add("One");
        combo.Items!.Add("Two");
        combo.Items!.Add("Three");
        combo.Visual.XUnits = GeneralUnitType.PixelsFromSmall;
        combo.Visual.YUnits = GeneralUnitType.PixelsFromSmall;
        combo.Visual.X = 300;
        combo.Visual.Y = 300;
        combo.Width = 160;
        combo.AddToRoot();
        game.Get(side).Service.Root.UpdateLayout();
        return combo;
    }

    private static Point Center(GraphicalUiElement element) =>
        new Point((int)(element.AbsoluteLeft + element.GetAbsoluteWidth() / 2), (int)(element.AbsoluteTop + element.GetAbsoluteHeight() / 2));

    private static Rectangle Box(GraphicalUiElement element) =>
        new Rectangle((int)element.AbsoluteLeft, (int)element.AbsoluteTop, (int)element.GetAbsoluteWidth(), (int)element.GetAbsoluteHeight());

    private static bool IsInTree(GraphicalUiElement element, GraphicalUiElement root)
    {
        for (IRenderableIpso? current = element; current != null; current = current.Parent)
        {
            if (current == root) { return true; }
        }
        return false;
    }

    private static string Describe(object? cursor, InputGame game) =>
        ReferenceEquals(cursor, game.A.Cursor) ? "A's cursor" : ReferenceEquals(cursor, game.B.Cursor) ? "B's cursor" : "another cursor";

    #endregion

    #region Helpers: drawing

    private sealed class Pixels
    {
        private readonly XnaColor[] _data;
        public Pixels(XnaColor[] data) { _data = data; }

        public int CountDifferences(Pixels other, Rectangle? within)
        {
            int count = 0;
            int x0 = within?.Left ?? 0;
            int y0 = within?.Top ?? 0;
            int x1 = Math.Min(CaptureWidth, within?.Right ?? CaptureWidth);
            int y1 = Math.Min(CaptureHeight, within?.Bottom ?? CaptureHeight);
            for (int y = Math.Max(0, y0); y < y1; y++)
            {
                for (int x = Math.Max(0, x0); x < x1; x++)
                {
                    int i = (y * CaptureWidth) + x;
                    if (_data[i] != other._data[i]) { count++; }
                }
            }
            return count;
        }
    }

    /// <summary>Draws only the side's service into a clear render target and reads the pixels back.</summary>
    private static Pixels DrawSolo(InputGame game, Side side)
    {
        GraphicsDevice gd = game.GraphicsDevice;
        InputService service = game.Get(side);
        using RenderTarget2D capture = new RenderTarget2D(gd, CaptureWidth, CaptureHeight, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        for (int pass = 0; pass < 2; pass++)
        {
            Activate(game, side);
            gd.SetRenderTarget(capture);
            gd.Clear(ClearColor);
            service.Service.SystemManagers.Renderer.Draw(service.Service.SystemManagers);
        }

        gd.SetRenderTarget(null);
        XnaColor[] data = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(data);
        return new Pixels(data);
    }

    #endregion

    #region Scripted input

    /// <summary>
    /// A real <see cref="Cursor"/> whose per-frame mouse state is scripted instead of read from the OS.
    /// Hides Activity(double) so that FormsUtilities.Update, which calls it through ICursor, feeds the script in.
    /// </summary>
    private sealed class ScriptedCursor : Cursor, ICursor
    {
        private int _x;
        private int _y;
        private bool _leftDown;

        public ScriptedCursor() : base((GameWindow?)null) { }

        public void Place(int x, int y, bool leftDown)
        {
            _x = x;
            _y = y;
            _leftDown = leftDown;
        }

        public new void Activity(double gameTime)
        {
            MouseState state = new MouseState(_x, _y, 0,
                _leftDown ? ButtonState.Pressed : ButtonState.Released,
                ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            Activity(gameTime, state, new TouchCollection(), isMobile: false);
        }
    }

    /// <summary>A keyboard whose pressed keys and typed text come from the test. Counts how often it is polled.</summary>
    private sealed class ScriptedKeyboard : PolledKeyboard
    {
        private static readonly GumKeys[] AllKeys = Enum.GetValues<GumKeys>();

        public int PollCount { get; private set; }

        protected override IEnumerable<GumKeys> SupportedKeys => AllKeys;

        protected override bool IsDeviceKeyDown(GumKeys key)
        {
            if (key == AllKeys[0]) { PollCount++; }
            return false;
        }

        public void Type(string text) => AppendTypedText(text);
    }

    #endregion

    #region Rig types

    private sealed class InputService
    {
        public Gum.GumService Service { get; } = new Gum.GumService();
        public GumProjectSave Project { get; set; } = null!;
        /// <summary>This service's Forms popup root, read while it is the default.</summary>
        public InteractiveGue? PopupRoot { get; set; }
        /// <summary>This service's Forms modal root, read while it is the default.</summary>
        public InteractiveGue? ModalRoot { get; set; }
        public ScriptedCursor Cursor { get; } = new ScriptedCursor();
        public ScriptedKeyboard Keyboard { get; } = new ScriptedKeyboard();
        public GraphicalUiElement Screen { get; set; } = null!;
        public Button ConfirmButton { get; set; } = null!;
        public TextBox TextBox { get; set; } = null!;
        public int ConfirmClicks { get; set; }
    }

    private sealed class InputGame : IDisposable
    {
        private readonly MultiServiceGameLease _lease;
        private readonly string _pathA;
        private readonly string _pathB;

        public InputService A { get; } = new InputService();
        public InputService B { get; } = new InputService();
        public double Seconds { get; set; }

        public InputService Get(Side side) => side == Side.A ? A : B;

        public Point ConfirmCenter(Side side) => Center(Get(side).ConfirmButton.Visual);

        public GraphicsDevice GraphicsDevice => _lease.GraphicsDevice;

        public InputGame(string pathA, string pathB)
        {
            _lease = new MultiServiceGameLease();
            _pathA = pathA;
            _pathB = pathB;
        }

        public void Initialize()
        {
            ShapeRenderer.Self.Initialize(GraphicsDevice, _lease.Game.Content);

            Setup(A, _pathA);
            Setup(B, _pathB);
        }

        private void Setup(InputService service, string path)
        {
            service.Project = service.Service.Initialize(_lease.Game, path)!;

            // Install the scripted input in this service's own scope and note its popup and modal roots.
            global::RenderingLibrary.IGumService.Default = service.Service;
            {
                FormsUtilities.SetCursor(service.Cursor);
                FormsUtilities.SetKeyboard(service.Keyboard);
                service.PopupRoot = FrameworkElement.PopupRoot;
                service.ModalRoot = FrameworkElement.ModalRoot;
            }
        }

        public void Dispose()
        {
            // Drop shared input state first so no control gets a focus callback after teardown.
            try { InteractiveGue.CurrentInputReceiver = null; } catch { /* teardown after a red test */ }
            InteractiveGue.LastVisualPushed = null;
            InteractiveGue.ClearNextPushActions();
            InteractiveGue.ClearNextClickActions();
            ToolTipService.ResetForTesting();
            FrameworkElement.AdditionalPopupRootPairs.Clear();
            GraphicalUiElement.ResolvePopupRoots = null;

            foreach (InputService service in new[] { B, A })
            {
                if (service.Service.IsInitialized)
                {
                    try { service.Service.Uninitialize(); } catch { /* teardown after a red test */ }
                }
            }
            ShapeRenderer.Self.Uninitialize();
            LoaderManager.Self?.DisposeAndClear();

            SystemManagers.Default = null!;
            ISystemManagers.Default = null!;
            IGumService.Default = null;
            FrameworkElement.PopupRoot = null!;
            FrameworkElement.ModalRoot = null!;
            Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle = null!;
            _lease.Dispose();
        }
    }

    #endregion
}
