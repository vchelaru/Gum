using System;
using Gum.Automation;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Tests.Input;
using Shouldly;
using Xunit;
using GamePad = Gum.Input.GamePad;
using GumKeys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// <see cref="InteractiveGue.IsInputBlocked"/> stops cursor, keyboard and gamepad input for an element and
/// its subtree, keeps the rest of Update running, and never changes which control has focus.
/// </summary>
public class IsInputBlockedTests : BaseTestClass
{
    private readonly ICursor? _originalCursor = FormsUtilities.Cursor;
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;
    private readonly GumDriver _driver;
    private readonly GamePad _pad = new();
    private Buttons _padButtons;

    public IsInputBlockedTests()
    {
        // Each frame applies the scripted pad state before the service update, as a game's input pump does.
        Gum.GumService service = Gum.GumService.Default;
        TimeSpan step = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
        TimeSpan total = TimeSpan.Zero;
        _driver = new GumDriver(
            () =>
            {
                total += step;
                _pad.Activity(new GamePadState(Vector2.Zero, Vector2.Zero, 0, 0, _padButtons), total.TotalSeconds);
                service.Update(new GameTime(total, step));
            },
            () => GumDriver.RootsOf(service));
        _driver.Install();
        FrameworkElement.KeyboardsForUiControl.Add(_driver.Keyboard);
        FrameworkElement.GamePadsForUiControl.Add(_pad);
    }

    public override void Dispose()
    {
        Gum.GumService.Default.Root.IsInputBlocked = false;
        if (_originalCursor != null)
        {
            FormsUtilities.SetCursor(_originalCursor);
        }
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }
        base.Dispose();
    }

    private InteractiveGue Root => Gum.GumService.Default.Root;

    private void Pad(Buttons buttons, int frames = 1)
    {
        _padButtons = buttons;
        _driver.Step(frames);
    }

    private static Button PlaceButton(string name, float y, GraphicalUiElement? parent = null)
    {
        Button button = new() { Name = name };
        if (parent == null)
        {
            button.AddToRoot();
        }
        else
        {
            parent.AddChild(button);
        }
        button.Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        button.Visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        button.Visual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Left;
        button.Visual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Top;
        button.X = 10;
        button.Y = y;
        return button;
    }

    #region Focus

    [Fact]
    public void BlockAndUnblock_FocusedButton_StaysFocusedWithFocusedLookAndNoFocusEvents()
    {
        Button button = PlaceButton("A", 10);
        StateLog log = new(button);
        button.IsFocused = true;
        _driver.Step();
        int focusEvents = 0;
        button.GotFocus += (_, _) => focusEvents++;
        button.LostFocus += (_, _) => focusEvents++;

        Root.IsInputBlocked = true;
        _driver.Step(3);
        Root.IsInputBlocked = false;
        _driver.Step(3);

        button.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBeSameAs(button);
        focusEvents.ShouldBe(0);
        log.Last.ShouldBe(FrameworkElement.FocusedStateName);
    }

    [Theory]
    [InlineData("TextBox")]
    [InlineData("ListBox")]
    [InlineData("Slider")]
    public void BlockAndUnblock_FocusedControl_StaysFocused(string controlType)
    {
        FrameworkElement control = controlType switch
        {
            "TextBox" => new TextBox(),
            "ListBox" => new ListBox(),
            _ => new Slider(),
        };
        if (control is ListBox listBox)
        {
            listBox.Items.Add("One");
            listBox.Items.Add("Two");
        }
        control.AddToRoot();
        control.IsFocused = true;
        _driver.Step();
        int focusEvents = 0;
        control.GotFocus += (_, _) => focusEvents++;
        control.LostFocus += (_, _) => focusEvents++;

        Root.IsInputBlocked = true;
        _driver.Step(3);
        Root.IsInputBlocked = false;
        _driver.Step(3);

        control.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBeSameAs(control);
        focusEvents.ShouldBe(0);
    }

    #endregion

    #region Ignored while blocked

    [Fact]
    public void Blocked_CursorClickOnButton_DoesNotClick()
    {
        Button button = PlaceButton("A", 10);
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        button.Visual.IsInputBlocked = true;
        _driver.Click("A");

        clicks.ShouldBe(0);
    }

    [Fact]
    public void BlockedAncestor_CursorClickOnChildButton_DoesNotClick()
    {
        ContainerRuntime screen = new() { Width = 400, Height = 400, WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute, HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute };
        screen.AddToRoot();
        Button button = PlaceButton("A", 10, screen);
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        screen.IsInputBlocked = true;
        _driver.Click("A");

        clicks.ShouldBe(0);
    }

    [Fact]
    public void BlockedSibling_OtherButtonStillClicks()
    {
        ContainerRuntime blocked = new() { IsInputBlocked = true };
        blocked.AddToRoot();
        PlaceButton("Blocked", 10, blocked);
        Button other = PlaceButton("Other", 60);
        int clicks = 0;
        other.Click += (_, _) => clicks++;

        _driver.Click("Other");

        clicks.ShouldBe(1);
    }

    [Fact]
    public void Blocked_ButtonInModalRoot_DoesNotClick()
    {
        Button button = new() { Name = "Modal" };
        Gum.GumService.Default.ModalRoot.AddChild(button);
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        button.Visual.IsInputBlocked = true;
        _driver.Click("Modal");

        clicks.ShouldBe(0);
    }

    [Fact]
    public void Blocked_TabDPadAndA_DoNothing()
    {
        Button first = PlaceButton("A", 10);
        PlaceButton("B", 60);
        int clicks = 0;
        first.Click += (_, _) => clicks++;
        first.IsFocused = true;
        _driver.Step();

        Root.IsInputBlocked = true;
        _driver.PressKey(GumKeys.Tab);
        Pad(Buttons.DPadDown);
        Pad(0);
        Pad(Buttons.A);
        Pad(0);

        first.IsFocused.ShouldBeTrue();
        clicks.ShouldBe(0);
    }

    [Fact]
    public void Blocked_TypedText_DoesNotReachFocusedTextBox()
    {
        TextBox textBox = new();
        textBox.AddToRoot();
        textBox.IsFocused = true;
        _driver.Step();

        Root.IsInputBlocked = true;
        _driver.Type("abc");
        Root.IsInputBlocked = false;
        _driver.Step();

        textBox.Text.ShouldBe("");
    }

    #endregion

    #region Presses that span the block

    [Fact]
    public void PressWhileBlocked_ReleaseAfterUnblock_DoesNotClick()
    {
        Button button = PlaceButton("A", 10);
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        _driver.MoveTo("A");

        Root.IsInputBlocked = true;
        _driver.SetButton(MouseButton.Primary, held: true);
        Root.IsInputBlocked = false;
        _driver.SetButton(MouseButton.Primary, held: true);
        _driver.SetButton(MouseButton.Primary, held: false);

        clicks.ShouldBe(0);
    }

    [Fact]
    public void PressBeforeBlock_ReleaseWhileBlocked_DoesNotClickAndLeavesPushedState()
    {
        Button button = PlaceButton("A", 10);
        StateLog log = new(button);
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        _driver.MoveTo("A");
        _driver.SetButton(MouseButton.Primary, held: true);
        log.Last.ShouldBe(FrameworkElement.PushedStateName);

        int drags = 0;
        button.Visual.Dragging += (_, _) => drags++;

        Root.IsInputBlocked = true;
        _driver.Step();
        log.Last.ShouldBe(FrameworkElement.EnabledStateName);
        _driver.Cursor.WindowPushed.ShouldBeNull();
        _driver.Step();
        _driver.SetButton(MouseButton.Primary, held: false);

        clicks.ShouldBe(0);
        drags.ShouldBe(0);
    }

    [Fact]
    public void RightPressBeforeBlock_ReleaseAfterUnblock_DoesNotRightClick()
    {
        Button button = PlaceButton("A", 10);
        int rightClicks = 0;
        button.Visual.RightClick += (_, _) => rightClicks++;
        _driver.MoveTo("A");
        _driver.SetButton(MouseButton.Secondary, held: true);

        Root.IsInputBlocked = true;
        _driver.Step();
        Root.IsInputBlocked = false;
        _driver.SetButton(MouseButton.Secondary, held: false);

        rightClicks.ShouldBe(0);
    }

    [Fact]
    public void Unblock_CursorStillOverButton_HoverReturnsNextFrame()
    {
        Button button = PlaceButton("A", 10);
        _driver.MoveTo("A");

        Root.IsInputBlocked = true;
        _driver.Step();
        _driver.Cursor.VisualOver.ShouldBeNull();
        Root.IsInputBlocked = false;
        _driver.Step();

        _driver.Cursor.VisualOver.ShouldBeSameAs(button.Visual);
    }

    [Fact]
    public void TabPushedWhileBlockedAndHeldPastIt_DoesNotRepeatAfterUnblock()
    {
        Button first = PlaceButton("A", 10);
        PlaceButton("B", 60);
        first.IsFocused = true;
        _driver.Step();

        Root.IsInputBlocked = true;
        _driver.SetKey(GumKeys.Tab, held: true);
        Root.IsInputBlocked = false;
        // Two seconds held: well past any key repeat delay.
        _driver.Step(120);

        first.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void DPadPushedWhileBlockedAndHeldPastIt_DoesNotRepeatAfterUnblock()
    {
        Button first = PlaceButton("A", 10);
        PlaceButton("B", 60);
        first.IsFocused = true;
        _driver.Step();

        Root.IsInputBlocked = true;
        Pad(Buttons.DPadDown);
        Root.IsInputBlocked = false;
        Pad(Buttons.DPadDown, frames: 120);

        first.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void KeyHeldPastBlock_AfterRelease_NextTabMovesFocus()
    {
        Button first = PlaceButton("A", 10);
        Button second = PlaceButton("B", 60);
        first.IsFocused = true;
        _driver.Step();

        Root.IsInputBlocked = true;
        _driver.SetKey(GumKeys.Tab, held: true);
        Root.IsInputBlocked = false;
        _driver.Step(10);
        _driver.SetKey(GumKeys.Tab, held: false);
        _driver.PressKey(GumKeys.Tab);

        second.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Unblocked_ClickOnButton_Clicks()
    {
        Button button = PlaceButton("A", 10);
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        button.Visual.IsInputBlocked = true;
        button.Visual.IsInputBlocked = false;
        _driver.Click("A");

        clicks.ShouldBe(1);
    }

    #endregion

    #region Update keeps running

    [Fact]
    public void Blocked_AnimationAndDeferredQueue_StillAdvance()
    {
        Button button = PlaceButton("A", 10);
        Gum.DataTypes.Variables.StateSaveCategory category = new() { Name = "Move" };
        Gum.DataTypes.Variables.StateSave from = new() { Name = "From" };
        from.Variables.Add(new Gum.DataTypes.Variables.VariableSave { Name = "X", Value = 0f });
        category.States.Add(from);
        Gum.DataTypes.Variables.StateSave to = new() { Name = "To" };
        to.Variables.Add(new Gum.DataTypes.Variables.VariableSave { Name = "X", Value = 100f });
        category.States.Add(to);
        button.Visual.Categories["Move"] = category;
        Gum.StateAnimation.Runtime.AnimationRuntime animation = new() { Name = "Move" };
        animation.Keyframes.Add(new Gum.StateAnimation.Runtime.KeyframeRuntime
        {
            Time = 0, StateName = "Move/From",
            InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear
        });
        animation.Keyframes.Add(new Gum.StateAnimation.Runtime.KeyframeRuntime { Time = 1, StateName = "Move/To" });
        bool deferredRan = false;

        Root.IsInputBlocked = true;
        button.Visual.PlayAnimation(animation);
        Gum.GumService.Default.DeferredQueue.Enqueue(() => deferredRan = true);
        _driver.Step(30);

        button.Visual.X.ShouldBeGreaterThan(0);
        deferredRan.ShouldBeTrue();
    }

    #endregion
}
