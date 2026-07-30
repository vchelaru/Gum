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
    public void HandleGamepadNavigation_SliderDPadDownPressed_NavigatesAwayWithoutChangingValue()
    {
        // Companion to the DPadRight test below: Up/Down aren't gated by
        // IsUsingLeftAndRightGamepadDirectionsForNavigation, and Slider has no competing Up/Down
        // handling of its own, so Down should navigate (now spatially) exactly like any other
        // control, leaving Value untouched.
        Panel panel = new Panel();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;

        Slider slider = new Slider();
        panel.AddChild(slider);
        slider.X = 0;
        slider.Y = 0;
        slider.Value = 50;

        Button below = new Button();
        panel.AddChild(below);
        below.X = 0;
        below.Y = 150;

        slider.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadDown, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        slider.OnFocusUpdate();

        slider.Value.ShouldBe(50);
        slider.IsFocused.ShouldBeFalse();
        below.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleGamepadNavigation_SliderDPadRightPressed_AdjustsValueAndDoesNotNavigate()
    {
        // Slider disables Left/Right-as-navigation in its own constructor (Slider.cs:101),
        // predating this feature — confirms that pre-existing guard also protects the new Spatial
        // path, since GamepadNavigationMode is resolved inside the same shared
        // HandleGamepadNavigation method Slider already calls. Without it, DPadRight would both
        // adjust Value and try to navigate away using the same press.
        Panel panel = new Panel();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;

        Slider slider = new Slider();
        panel.AddChild(slider);
        slider.X = 0;
        slider.Y = 0;
        slider.Value = 50;

        Button right = new Button();
        panel.AddChild(right);
        right.X = 150;
        right.Y = 0;

        slider.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadRight, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        slider.OnFocusUpdate();

        slider.Value.ShouldBeGreaterThan(50);
        slider.IsFocused.ShouldBeTrue();
        right.IsFocused.ShouldBeFalse();
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
    public void HandleGamepadNavigation_ListBoxItemsHaveInternalFocus_DoesNotTriggerTopLevelNavigation()
    {
        // ListBox.OnFocusUpdate branches: DoListItemsHaveFocus routes to DoListItemFocusUpdate
        // (moves selection between rows), which never calls HandleGamepadNavigation at all --
        // only the else branch (DoTopLevelFocusUpdate, top-level list focus) does. So a Down
        // press while an item has internal focus should stay entirely within the list, not also
        // trigger spatial navigation away to a sibling control.
        Panel panel = new Panel();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;

        ListBox listBox = new ListBox();
        panel.AddChild(listBox);
        listBox.X = 0;
        listBox.Y = 0;
        listBox.Items!.Add("A");
        listBox.Items!.Add("B");
        listBox.Items!.Add("C");
        listBox.SelectedIndex = 0;
        listBox.DoListItemsHaveFocus = true;

        Button below = new Button();
        panel.AddChild(below);
        below.X = 0;
        below.Y = 300;

        listBox.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadDown, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        listBox.OnFocusUpdate();

        listBox.IsFocused.ShouldBeTrue();
        below.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleGamepadNavigation_ScrollViewerItemHasInternalFocus_ChildNavigatesLikeAnyOtherFocusedControl()
    {
        // ScrollViewer.DoItemsHaveFocus is NOT the same shape as ListBox.DoListItemsHaveFocus --
        // ListBox's is a virtual/internal flag (the ListBox itself stays the real IsFocused /
        // CurrentInputReceiver throughout), but ScrollViewer.DoItemsHaveFocus hands REAL focus to
        // the child element (confirmed: scrollViewer.IsFocused goes false, item.IsFocused goes
        // true, matching the existing ScrollViewerTests precedent). Once that happens, it's the
        // child's own OnFocusUpdate that runs each frame (per InteractiveGue.CurrentInputReceiver
        // dispatch), not ScrollViewer.DoTopLevelFocusUpdate -- so a plain Button child under a
        // Spatial-marked ancestor navigates exactly like any other focused Button.
        //
        // This originally exposed a real bug: ScrollViewer itself is also a focusable candidate,
        // and (being a large container wrapping `item` near its top edge) its own center scored
        // better than the true sibling `below`, so Down navigated back to the ScrollViewer itself
        // instead of escaping to `below`. Fixed in SpatialNavigationService by excluding ancestors
        // of the origin, not just the origin, from the candidate set (see
        // SpatialNavigationServiceTests.FindBestCandidate_ExcludesAncestorsOfOrigin...).
        Panel panel = new Panel();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;

        ScrollViewer scrollViewer = new ScrollViewer();
        panel.AddChild(scrollViewer);
        scrollViewer.X = 0;
        scrollViewer.Y = 0;

        Button item = new Button();
        scrollViewer.AddChild(item);

        Button below = new Button();
        panel.AddChild(below);
        below.X = 0;
        below.Y = 300;

        scrollViewer.IsFocused = true;
        scrollViewer.DoItemsHaveFocus = true;

        item.IsFocused.ShouldBeTrue();
        scrollViewer.IsFocused.ShouldBeFalse();
        item.ParentFrameworkElement.ShouldBe(scrollViewer);

        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadDown, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        // The real per-frame dispatch would call OnFocusUpdate on whichever element is actually
        // CurrentInputReceiver -- that's now `item`, not scrollViewer.
        item.OnFocusUpdate();

        below.IsFocused.ShouldBeTrue();
        item.IsFocused.ShouldBeFalse();
        scrollViewer.IsFocused.ShouldBeFalse();
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
