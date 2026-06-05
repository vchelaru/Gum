using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;

using Shouldly;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// Tests for controller / gamepad focus navigation (tabbing) on Raylib. These
/// exercise the shared <see cref="FrameworkElement.HandleGamepadNavigation"/> path
/// driven by a real <see cref="GamePad"/> data holder — the same holder the
/// <c>#if RAYLIB</c> branch of <c>FormsUtilities.UpdateGamepads</c> populates from
/// Raylib input each frame. Before issue #3046, the holder returned false for every
/// query so navigating the UI with a controller did nothing on Raylib.
///
/// Mirrors the harness approach in <see cref="FocusTabNavigationTests"/>: a thin
/// <see cref="FrameworkElement"/> wired to a <see cref="ContainerRuntime"/> visual,
/// avoiding the full <c>FormsUtilities.InitializeDefaults</c> setup.
/// </summary>
public class ControllerTabNavigationTests : BaseTestClass
{
    private sealed class GamepadNavHarness : FrameworkElement, IInputReceiver
    {
        public GamepadNavHarness(InteractiveGue visual) : base(visual) { }

        public void InvokeHandleGamepadNavigation(IGamePad gamepad) => HandleGamepadNavigation(gamepad);

        public IInputReceiver? ParentInputReceiver => null;

        public void OnGainFocus() { }

        public void OnLoseFocus() { }

        public void OnFocusUpdate() { }

        public void OnFocusUpdatePreview(RoutedEventArgs args) { }

        public void DoKeyboardAction(IInputReceiverKeyboard keyboard) { }
    }

    private static GamepadNavHarness CreateHarness(ContainerRuntime parent)
    {
        ContainerRuntime visual = new ContainerRuntime();
        visual.Parent = parent;
        GamepadNavHarness harness = new GamepadNavHarness(visual);
        return harness;
    }

    [Fact]
    public void HandleGamepadNavigation_DPadDownPushed_MovesFocusToNextFocusableSibling()
    {
        ContainerRuntime root = new ContainerRuntime();

        GamepadNavHarness harness1 = CreateHarness(root);
        GamepadNavHarness harness2 = CreateHarness(root);

        harness1.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadDown, true);
        gamepad.Activity(1);

        harness1.InvokeHandleGamepadNavigation(gamepad);

        harness1.IsFocused.ShouldBeFalse();
        harness2.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleGamepadNavigation_DPadUpPushed_MovesFocusToPreviousFocusableSibling()
    {
        ContainerRuntime root = new ContainerRuntime();

        GamepadNavHarness harness1 = CreateHarness(root);
        GamepadNavHarness harness2 = CreateHarness(root);

        harness2.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadUp, true);
        gamepad.Activity(1);

        harness2.InvokeHandleGamepadNavigation(gamepad);

        harness2.IsFocused.ShouldBeFalse();
        harness1.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleGamepadNavigation_LeftStickDown_MovesFocusToNextFocusableSibling()
    {
        ContainerRuntime root = new ContainerRuntime();

        GamepadNavHarness harness1 = CreateHarness(root);
        GamepadNavHarness harness2 = CreateHarness(root);

        harness1.IsFocused = true;

        GamePad gamepad = new GamePad();
        // XNA/Gum stick convention: down is negative Y.
        gamepad.SetLeftStickPosition(0, -1);
        gamepad.Activity(1);

        harness1.InvokeHandleGamepadNavigation(gamepad);

        harness1.IsFocused.ShouldBeFalse();
        harness2.IsFocused.ShouldBeTrue();
    }
}
