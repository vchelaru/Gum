using Gum.Forms.Controls;
using Gum.Input;

using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Tests for the real (non-spike) integration of issue #4129: <see cref="FrameworkElement.GamepadNavigationMode"/>
/// lets a container opt a subtree into spatial navigation declaratively, so stock controls (plain
/// <see cref="Button"/> here, no subclass) get it automatically through the existing
/// <see cref="FrameworkElement.HandleGamepadNavigation(GamePadForNavigation)"/> call every control's
/// own <c>OnFocusUpdate</c> already makes — unlike PR #4132's spike, which required a custom
/// subclass and a manually-driven gamepad kept out of <see cref="FrameworkElement.GamePadsForUiControl"/>.
/// </summary>
public class GamepadNavigationModeTests : BaseTestClass
{
    [Fact]
    public void HandleGamepadNavigation_DiagonalPress_IgnoresSingleDirectionOverride()
    {
        Panel panel = new Panel();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;

        Button origin = new Button();
        panel.AddChild(origin);
        origin.X = 0;
        origin.Y = 0;

        Button overrideRightTarget = new Button();
        panel.AddChild(overrideRightTarget);
        overrideRightTarget.X = 500;
        overrideRightTarget.Y = 0;

        Button diagonalCandidate = new Button();
        panel.AddChild(diagonalCandidate);
        diagonalCandidate.X = 100;
        diagonalCandidate.Y = 100;

        // Set, but the press below is diagonal (Right+Down), not a clean single direction, so the
        // override must be ignored and scoring must run instead.
        origin.SpatialNavigationRight = overrideRightTarget;
        origin.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadRight, true);
        gamepad.SetButtonState(GamepadButton.DPadDown, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        origin.OnFocusUpdate();

        diagonalCandidate.IsFocused.ShouldBeTrue();
        overrideRightTarget.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleGamepadNavigation_ExplicitOverrideSet_TakesPrecedenceOverScoring()
    {
        Panel panel = new Panel();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;

        Button origin = new Button();
        panel.AddChild(origin);
        origin.X = 0;
        origin.Y = 0;

        Button nearestByScore = new Button();
        panel.AddChild(nearestByScore);
        nearestByScore.X = 50;
        nearestByScore.Y = 0;

        Button overrideTarget = new Button();
        panel.AddChild(overrideTarget);
        overrideTarget.X = 500;
        overrideTarget.Y = 0;

        origin.SpatialNavigationRight = overrideTarget;
        origin.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadRight, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        origin.OnFocusUpdate();

        overrideTarget.IsFocused.ShouldBeTrue();
        nearestByScore.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleGamepadNavigation_NestedPanelExplicitTabOrder_OptsOutOfOuterSpatialZone()
    {
        Panel outer = new Panel();
        outer.AddToRoot();
        outer.GamepadNavigationMode = GamepadNavigationMode.Spatial;

        Panel inner = new Panel();
        outer.AddChild(inner);
        inner.GamepadNavigationMode = GamepadNavigationMode.TabOrder;

        Button first = new Button();
        inner.AddChild(first);
        first.X = 0;
        first.Y = 0;

        // Directly opposite ("up") of the Down press below — if spatial scoring wrongly ran here,
        // this candidate would be excluded by the direction cone and nothing would be focused,
        // proving second.IsFocused below can only be true via tab order.
        Button second = new Button();
        inner.AddChild(second);
        second.X = 0;
        second.Y = -150;

        first.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadDown, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        first.OnFocusUpdate();

        first.IsFocused.ShouldBeFalse();
        second.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleGamepadNavigation_NoModeSet_UsesExistingIndexBasedNavigation()
    {
        Panel panel = new Panel();
        panel.AddToRoot();
        // GamepadNavigationMode left unset (null) on every ancestor — must default to TabOrder,
        // exactly matching every existing consumer's behavior before this feature existed.

        Button first = new Button();
        panel.AddChild(first);
        first.X = 0;
        first.Y = 0;

        Button second = new Button();
        panel.AddChild(second);
        second.X = -150;
        second.Y = 0;

        first.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadDown, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        first.OnFocusUpdate();

        first.IsFocused.ShouldBeFalse();
        second.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleGamepadNavigation_PanelMarkedSpatial_StockButtonNavigatesSpatially()
    {
        Panel panel = new Panel();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;

        Button origin = new Button();
        panel.AddChild(origin);
        origin.X = 0;
        origin.Y = 0;

        Button right = new Button();
        panel.AddChild(right);
        right.X = 150;
        right.Y = 0;

        Button above = new Button();
        panel.AddChild(above);
        above.X = 0;
        above.Y = -150;

        origin.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadRight, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        origin.OnFocusUpdate();

        origin.IsFocused.ShouldBeFalse();
        right.IsFocused.ShouldBeTrue();
        above.IsFocused.ShouldBeFalse();
    }
}
