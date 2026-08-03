using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;

using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Tests for direction-agnostic ("spatial") gamepad focus navigation — the issue #4129 spike.
/// Exercises <see cref="FrameworkElement.HandleGamepadSpatialNavigation"/>, driven by a real
/// <see cref="GamePad"/> data holder. Mirrors the harness approach in
/// <see cref="RaylibGum.Tests.Forms.ControllerTabNavigationTests"/> for the index-based counterpart.
/// </summary>
public class ControllerSpatialNavigationTests : BaseTestClass
{
    private sealed class SpatialNavHarness : FrameworkElement, IInputReceiver
    {
        public SpatialNavHarness(InteractiveGue visual) : base(visual) { }

        public void InvokeHandleGamepadSpatialNavigation(IGamePad gamepad, GraphicalUiElement navigationRoot) =>
            HandleGamepadSpatialNavigation(gamepad, navigationRoot);

        public IInputReceiver? ParentInputReceiver => null;

        public void OnGainFocus() { }

        public void OnLoseFocus() { }

        public void OnFocusUpdate() { }

        public void OnFocusUpdatePreview(RoutedEventArgs args) { }

        public void DoKeyboardAction(IInputReceiverKeyboard keyboard) { }
    }

    private static SpatialNavHarness CreateHarness(ContainerRuntime parent, float x, float y)
    {
        ContainerRuntime visual = new ContainerRuntime();
        visual.Parent = parent;
        SpatialNavHarness harness = new SpatialNavHarness(visual);
        harness.X = x;
        harness.Y = y;
        return harness;
    }

    private sealed class SpatialNavButton : Button
    {
        public void InvokeHandleGamepadSpatialNavigation(IGamePad gamepad, GraphicalUiElement navigationRoot) =>
            HandleGamepadSpatialNavigation(gamepad, navigationRoot);
    }

    private static SpatialNavButton CreateButton(string name, float x, float y)
    {
        SpatialNavButton button = new SpatialNavButton();
        button.Name = name;
        button.X = x;
        button.Y = y;
        button.AddToRoot();
        return button;
    }

    [Fact]
    public void HandleGamepadSpatialNavigation_DiagonalDPadHeld_MovesFocusToDiagonalCandidate()
    {
        ContainerRuntime root = new ContainerRuntime();

        // Deltas keep the default-sized harness rectangles from overlapping each other; distance is
        // measured rect-to-rect, so an overlap would flatten the angle between them to axis-aligned.
        SpatialNavHarness origin = CreateHarness(root, 0, 0);
        SpatialNavHarness right = CreateHarness(root, 400, 0);
        SpatialNavHarness downRight = CreateHarness(root, 200, 200);

        origin.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadRight, true);
        gamepad.SetButtonState(GamepadButton.DPadDown, true);
        gamepad.Activity(1);

        origin.InvokeHandleGamepadSpatialNavigation(gamepad, root);

        origin.IsFocused.ShouldBeFalse();
        downRight.IsFocused.ShouldBeTrue();
        right.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleGamepadSpatialNavigation_DiagonalDPadHeld_SelfReferencingOverridesOnBothAxesSuppressNavigation()
    {
        // Reproduces a real bug report: an element suppresses Up and Right individually by pointing
        // both overrides back at itself. Pressing Up and Right on the exact same frame must still be
        // suppressed -- previously the two-held-directions case skipped the override check entirely
        // and fell through to scoring, which found upRight and navigated there anyway.
        ContainerRuntime root = new ContainerRuntime();

        SpatialNavHarness origin = CreateHarness(root, 0, 0);
        SpatialNavHarness upRight = CreateHarness(root, 200, -200);

        origin.SpatialNavigationUp = origin;
        origin.SpatialNavigationRight = origin;
        origin.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadUp, true);
        gamepad.SetButtonState(GamepadButton.DPadRight, true);
        gamepad.Activity(1);

        origin.InvokeHandleGamepadSpatialNavigation(gamepad, root);

        origin.IsFocused.ShouldBeTrue();
        upRight.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleGamepadSpatialNavigation_DiagonalDPadPressedSequentially_MovesFocusToDiagonalCandidate()
    {
        // Realistic controller use: Up is held first, then Right is pressed afterward while Up is
        // still down — not both pushed on the exact same frame. ButtonRepeatRate(Up) only pulses
        // on its own push/repeat schedule, so it is false on the frame Right is first pressed;
        // direction must come from which buttons are currently held (ButtonDown), not from which
        // ones' own repeat-rate happened to fire this frame. pureRight sits exactly where a
        // (buggy) Up-dropped, Right-only angle of 0 degrees would send focus; diagonal sits
        // exactly where the correct -45 degree (up-right) angle should send it instead.
        ContainerRuntime root = new ContainerRuntime();

        SpatialNavHarness origin = CreateHarness(root, 0, 0);
        SpatialNavHarness pureRight = CreateHarness(root, 400, 0);
        SpatialNavHarness diagonal = CreateHarness(root, 200, -200);

        origin.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadUp, true);
        gamepad.Activity(0);

        gamepad.SetButtonState(GamepadButton.DPadUp, true);
        gamepad.SetButtonState(GamepadButton.DPadRight, true);
        gamepad.Activity(0.1);

        origin.InvokeHandleGamepadSpatialNavigation(gamepad, root);

        origin.IsFocused.ShouldBeFalse();
        diagonal.IsFocused.ShouldBeTrue();
        pureRight.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleGamepadSpatialNavigation_DPadRightPushed_MovesFocusToCandidateOnRight()
    {
        ContainerRuntime root = new ContainerRuntime();

        SpatialNavHarness origin = CreateHarness(root, 0, 0);
        SpatialNavHarness right = CreateHarness(root, 400, 0);
        SpatialNavHarness above = CreateHarness(root, 0, -400);

        origin.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadRight, true);
        gamepad.Activity(1);

        origin.InvokeHandleGamepadSpatialNavigation(gamepad, root);

        origin.IsFocused.ShouldBeFalse();
        right.IsFocused.ShouldBeTrue();
        above.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleGamepadSpatialNavigation_LeftRightNavigationDisabled_StickPushedRight_DoesNotMoveFocus()
    {
        // Reproduces issue #4273: a Slider sets IsUsingLeftAndRightGamepadDirectionsForNavigation to
        // false so left/right stick input adjusts its own Value instead of navigating focus. Under
        // spatial navigation, a pure left/right stick push must not steal focus to a candidate on
        // that axis either.
        ContainerRuntime root = new ContainerRuntime();

        SpatialNavHarness origin = CreateHarness(root, 0, 0);
        origin.IsUsingLeftAndRightGamepadDirectionsForNavigation = false;
        SpatialNavHarness right = CreateHarness(root, 150, 0);

        origin.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetLeftStickPosition(1f, 0f);
        gamepad.Activity(1);

        origin.InvokeHandleGamepadSpatialNavigation(gamepad, root);

        origin.IsFocused.ShouldBeTrue();
        right.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleGamepadSpatialNavigation_LeftRightNavigationDisabledWithExplicitOverride_StickPushedRight_DoesNotUseOverride()
    {
        // Same root cause as above, via the explicit-override branch: an override assigned to the
        // right cardinal must not fire from a horizontal stick push when left/right navigation is
        // disabled (e.g. a Slider, which reserves left/right stick input for adjusting its own Value).
        ContainerRuntime root = new ContainerRuntime();

        SpatialNavHarness origin = CreateHarness(root, 0, 0);
        origin.IsUsingLeftAndRightGamepadDirectionsForNavigation = false;
        SpatialNavHarness overrideTarget = CreateHarness(root, 150, 0);
        origin.SpatialNavigationRight = overrideTarget;

        origin.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetLeftStickPosition(1f, 0f);
        gamepad.Activity(1);

        origin.InvokeHandleGamepadSpatialNavigation(gamepad, root);

        origin.IsFocused.ShouldBeTrue();
        overrideTarget.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleGamepadSpatialNavigation_NoInput_DoesNotChangeFocus()
    {
        ContainerRuntime root = new ContainerRuntime();

        SpatialNavHarness origin = CreateHarness(root, 0, 0);
        SpatialNavHarness other = CreateHarness(root, 150, 0);

        origin.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.Activity(1);

        origin.InvokeHandleGamepadSpatialNavigation(gamepad, root);

        origin.IsFocused.ShouldBeTrue();
        other.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleGamepadSpatialNavigation_SelfReferencingExplicitOverride_BlocksDirectionWithoutLosingFocus()
    {
        // Issue #4298: assigning a control to its own SpatialNavigationRight is the documented way to
        // block rightward navigation entirely. It must consume the press without moving focus anywhere,
        // including off of itself.
        ContainerRuntime root = new ContainerRuntime();

        SpatialNavHarness origin = CreateHarness(root, 0, 0);
        SpatialNavHarness nearestByScore = CreateHarness(root, 150, 0);
        origin.SpatialNavigationRight = origin;

        origin.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadRight, true);
        gamepad.Activity(1);

        origin.InvokeHandleGamepadSpatialNavigation(gamepad, root);

        origin.IsFocused.ShouldBeTrue();
        nearestByScore.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleGamepadSpatialNavigation_SkillTreeLayoutDPadDown_MovesFocusToNearestDownwardCandidate()
    {
        // Reproduces the manual-test demo's scattered 6-button layout reported in issue #4129:
        // pressing Down from TopLeft was observed to jump straight to FarRightMid instead of
        // BottomLeft/Middle. Real Button (not the bare-visual harness) via AddToRoot, matching
        // the demo's own setup exactly, to see whether this test path agrees with the app.
        SpatialNavButton topLeft = CreateButton("TopLeft", 80, 60);
        CreateButton("TopRight", 500, 90);
        SpatialNavButton middle = CreateButton("Middle", 280, 260);
        SpatialNavButton bottomLeft = CreateButton("BottomLeft", 60, 420);
        CreateButton("BottomRight", 560, 440);
        SpatialNavButton farRightMid = CreateButton("FarRightMid", 650, 250);

        topLeft.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadDown, true);
        gamepad.Activity(1);

        topLeft.InvokeHandleGamepadSpatialNavigation(gamepad, GumService.Default.Root);

        farRightMid.IsFocused.ShouldBeFalse();
        (bottomLeft.IsFocused || middle.IsFocused).ShouldBeTrue();
    }

    [Fact]
    public void HandleGamepadSpatialNavigation_StickPushedNearLeft_HonorsSpatialNavigationLeftOverride()
    {
        // Issue #4274: explicit per-direction overrides only ever consulted the DPad, so a
        // developer-set SpatialNavigationLeft was silently ignored when the player used the left
        // stick instead -- scoring ran and could pick a different (wrong) candidate even with an
        // override in place. A near-left stick push should snap to the Left cardinal and honor it.
        ContainerRuntime root = new ContainerRuntime();

        SpatialNavHarness origin = CreateHarness(root, 0, 0);
        SpatialNavHarness nearestByScore = CreateHarness(root, -90, -20);
        SpatialNavHarness overrideTarget = CreateHarness(root, -400, 0);

        origin.SpatialNavigationLeft = overrideTarget;
        origin.IsFocused = true;

        GamePad gamepad = new GamePad();
        // AnalogStick.Y is +1 = up; a small upward wobble keeps this short of straight-left.
        gamepad.SetLeftStickPosition(-0.98f, 0.05f);
        gamepad.Activity(1);

        origin.InvokeHandleGamepadSpatialNavigation(gamepad, root);

        origin.IsFocused.ShouldBeFalse();
        overrideTarget.IsFocused.ShouldBeTrue();
        nearestByScore.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleGamepadSpatialNavigation_StickPushedOffAxis_MovesFocusUsingContinuousAngle()
    {
        ContainerRuntime root = new ContainerRuntime();

        SpatialNavHarness origin = CreateHarness(root, 0, 0);
        SpatialNavHarness downRight = CreateHarness(root, 200, 200);
        SpatialNavHarness right = CreateHarness(root, 400, 0);

        origin.IsFocused = true;

        GamePad gamepad = new GamePad();
        // AnalogStick.Y is +1 = up, so pushing down-right on screen is (positive X, negative Y).
        gamepad.SetLeftStickPosition(0.7f, -0.7f);
        gamepad.Activity(1);

        origin.InvokeHandleGamepadSpatialNavigation(gamepad, root);

        origin.IsFocused.ShouldBeFalse();
        downRight.IsFocused.ShouldBeTrue();
        right.IsFocused.ShouldBeFalse();
    }
}
