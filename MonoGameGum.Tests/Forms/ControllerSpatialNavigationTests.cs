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

    [Fact]
    public void HandleGamepadSpatialNavigation_DiagonalDPadHeld_MovesFocusToDiagonalCandidate()
    {
        ContainerRuntime root = new ContainerRuntime();

        SpatialNavHarness origin = CreateHarness(root, 0, 0);
        SpatialNavHarness right = CreateHarness(root, 150, 0);
        SpatialNavHarness downRight = CreateHarness(root, 100, 100);

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
    public void HandleGamepadSpatialNavigation_DPadRightPushed_MovesFocusToCandidateOnRight()
    {
        ContainerRuntime root = new ContainerRuntime();

        SpatialNavHarness origin = CreateHarness(root, 0, 0);
        SpatialNavHarness right = CreateHarness(root, 150, 0);
        SpatialNavHarness above = CreateHarness(root, 0, -150);

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
    public void HandleGamepadSpatialNavigation_StickPushedOffAxis_MovesFocusUsingContinuousAngle()
    {
        ContainerRuntime root = new ContainerRuntime();

        SpatialNavHarness origin = CreateHarness(root, 0, 0);
        SpatialNavHarness downRight = CreateHarness(root, 100, 100);
        SpatialNavHarness right = CreateHarness(root, 150, 0);

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
