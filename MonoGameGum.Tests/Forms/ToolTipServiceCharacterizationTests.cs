using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Pins how <see cref="ToolTipService"/> behaves for Forms controls (issue #7058). These were
/// written and run green against the service before it learned about plain visuals, so any red
/// here after that change means Forms tooltips changed.
/// </summary>
public class ToolTipServiceCharacterizationTests : BaseTestClass
{
    public ToolTipServiceCharacterizationTests()
    {
        ToolTipService.ResetForTesting();
        ToolTipService.InitialShowDelay = TimeSpan.FromMilliseconds(500);
        ToolTipService.ShowDuration = TimeSpan.FromMilliseconds(5000);
        ToolTipService.BetweenShowDelay = TimeSpan.FromMilliseconds(100);
    }

    public override void Dispose()
    {
        ToolTipService.ResetForTesting();
        ToolTipService.InitialShowDelay = TimeSpan.FromMilliseconds(500);
        ToolTipService.ShowDuration = TimeSpan.FromMilliseconds(5000);
        ToolTipService.BetweenShowDelay = TimeSpan.FromMilliseconds(100);
        base.Dispose();
    }

    private static int OpenCount => FrameworkElement.PopupRoot.Children.Count;

    private static Button CreateHost(string tooltip, float x = 0)
    {
        Button button = new Button();
        button.Visual.X = x;
        button.AddToRoot();
        button.ToolTip = tooltip;
        return button;
    }

    #region Delays

    [Fact]
    public void InitialShowDelay_JustBeforeDelay_StaysClosed_AndAtDelay_Opens()
    {
        Button button = CreateHost("hi");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.Over(cursor, button.Visual);

        ToolTipTestHarness.TickTo(1.0);
        ToolTipTestHarness.TickTo(1.49);
        OpenCount.ShouldBe(0);

        ToolTipTestHarness.TickTo(1.5);
        OpenCount.ShouldBe(1);
    }

    [Fact]
    public void BetweenShowDelay_ReHoverAfterTheWindow_WaitsForTheInitialDelayAgain()
    {
        Button button = CreateHost("hi");
        Button other = CreateHost("other", x: 200);
        other.ToolTip = null;
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);
        OpenCount.ShouldBe(1);

        ToolTipTestHarness.Over(cursor, other.Visual);
        ToolTipTestHarness.TickTo(0.65);
        OpenCount.ShouldBe(0);

        // 0.5s after closing is well past the 100ms window, so the 500ms delay applies again.
        ToolTipTestHarness.Over(cursor, button.Visual);
        ToolTipTestHarness.TickTo(1.15);
        OpenCount.ShouldBe(0);
        ToolTipTestHarness.TickTo(1.7);
        OpenCount.ShouldBe(1);
    }

    [Fact]
    public void ShowDuration_StaysOpenUntilTheDurationElapses()
    {
        ToolTipService.ShowDuration = TimeSpan.FromMilliseconds(1000);
        Button button = CreateHost("hi");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual); // opens on the tick at 0.6
        ToolTipTestHarness.TickTo(1.5);
        OpenCount.ShouldBe(1);

        ToolTipTestHarness.TickTo(1.6);
        OpenCount.ShouldBe(0);
    }

    [Fact]
    public void ShowDuration_WhileStillHoveringAfterTimeout_ReopensOnTheNextTick()
    {
        // Pinned as observed: the timeout records a close time, so the BetweenShowDelay window
        // lets the very next tick reopen the tooltip for a cursor that never left.
        ToolTipService.ShowDuration = TimeSpan.FromMilliseconds(1000);
        Button button = CreateHost("hi");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);
        ToolTipTestHarness.TickTo(1.6);
        OpenCount.ShouldBe(0);

        ToolTipTestHarness.TickTo(1.62);
        OpenCount.ShouldBe(1);
    }

    #endregion

    #region Placement

    [Fact]
    public void Open_PlacesTheTooltipBelowAndRightOfTheCursor()
    {
        Button button = CreateHost("hi");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);

        Tooltip tooltip = ToolTipService.GetTooltip(button)!;
        tooltip.Visual.X.ShouldBe(ToolTipTestHarness.CursorX + 12);
        tooltip.Visual.Y.ShouldBe(ToolTipTestHarness.CursorY + 20);
    }

    [Fact]
    public void Open_NearTheBottomRightCorner_KeepsTheTooltipOnScreen()
    {
        GraphicalUiElement.CanvasWidth = 800;
        GraphicalUiElement.CanvasHeight = 600;
        Button button = CreateHost("The quick brown fox jumps over the lazy dog");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(790f);
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(590f);

        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);

        Tooltip tooltip = ToolTipService.GetTooltip(button)!;
        tooltip.Visual.X.ShouldBeLessThanOrEqualTo(800 - tooltip.Visual.AbsoluteWidth);
        tooltip.Visual.Y.ShouldBeLessThanOrEqualTo(600 - tooltip.Visual.AbsoluteHeight);
    }

    #endregion

    #region Leaving, disabling, removing

    [Fact]
    public void Leave_ToAHostWithoutATooltip_ClosesAndStaysClosed()
    {
        Button button = CreateHost("hi");
        Button plain = new Button();
        plain.AddToRoot();
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);
        OpenCount.ShouldBe(1);

        ToolTipTestHarness.Over(cursor, plain.Visual);
        ToolTipTestHarness.TickTo(0.7);
        ToolTipTestHarness.TickTo(2.0);

        OpenCount.ShouldBe(0);
    }

    [Fact]
    public void DisabledHost_StillShowsItsTooltip()
    {
        // Pinned as observed: the service never looks at IsEnabled.
        Button button = CreateHost("hi");
        button.IsEnabled = false;
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);

        OpenCount.ShouldBe(1);
    }

    [Fact]
    public void HostRemovedWhileOpen_AndCursorNoLongerOverIt_ClosesTheTooltip()
    {
        Button button = CreateHost("hi");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);
        OpenCount.ShouldBe(1);

        button.RemoveFromRoot();
        ToolTipTestHarness.Over(cursor, null);
        ToolTipTestHarness.TickTo(0.7);

        OpenCount.ShouldBe(0);
    }

    [Fact]
    public void HostRemovedWhileOpen_ButCursorStillReportsIt_TheTooltipStaysUntilShowDuration()
    {
        // Pinned as observed: the service does not check that the host is still attached.
        Button button = CreateHost("hi");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);

        button.RemoveFromRoot();
        ToolTipTestHarness.TickTo(0.7);

        OpenCount.ShouldBe(1);
    }

    [Fact]
    public void SettingToolTipToNull_WhileOpen_ClosesItWithoutRaisingToolTipClosing()
    {
        Button button = CreateHost("hi");
        int closingCount = 0;
        button.ToolTipClosing += (_, _) => closingCount++;
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);

        button.ToolTip = null;

        OpenCount.ShouldBe(0);
        closingCount.ShouldBe(0);
    }

    #endregion

    #region Nesting

    [Fact]
    public void NestedHosts_HoveringTheInnerOne_ShowsTheInnerTooltip()
    {
        StackPanel outer = new StackPanel();
        outer.AddToRoot();
        outer.ToolTip = "outer";
        Button inner = new Button();
        outer.AddChild(inner);
        inner.ToolTip = "inner";
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, inner.Visual);

        ToolTipService.GetTooltip(inner)!.IsOpen.ShouldBeTrue();
        ToolTipService.GetTooltip(outer)!.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void NestedHosts_HoveringAChildWithoutATooltip_ShowsTheAncestorsTooltip()
    {
        StackPanel outer = new StackPanel();
        outer.AddToRoot();
        outer.ToolTip = "outer";
        Button inner = new Button();
        outer.AddChild(inner);
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, inner.Visual);

        ToolTipService.GetTooltip(outer)!.IsOpen.ShouldBeTrue();
    }

    [Fact]
    public void NestedHosts_MovingFromInnerToOuter_ClosesTheInnerAndOpensTheOuterOnTheSameTick()
    {
        StackPanel outer = new StackPanel();
        outer.AddToRoot();
        outer.ToolTip = "outer";
        Button inner = new Button();
        outer.AddChild(inner);
        inner.ToolTip = "inner";
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, inner.Visual);

        ToolTipTestHarness.Over(cursor, outer.Visual);
        // The close records a close time, so the BetweenShowDelay window skips the initial delay
        // and the outer host's tooltip opens on the very tick that closed the inner one.
        ToolTipTestHarness.TickTo(0.65);

        ToolTipService.GetTooltip(inner)!.IsOpen.ShouldBeFalse();
        ToolTipService.GetTooltip(outer)!.IsOpen.ShouldBeTrue();
    }

    [Fact]
    public void WindowPushed_TakesPrecedenceOverVisualOver()
    {
        Button pushed = CreateHost("pushed");
        Button over = CreateHost("over", x: 200);
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.Over(cursor, over.Visual);
        ToolTipTestHarness.Pushed(cursor, pushed.Visual);

        ToolTipTestHarness.TickTo(0.0);
        ToolTipTestHarness.TickTo(0.6);

        ToolTipService.GetTooltip(pushed)!.IsOpen.ShouldBeTrue();
        ToolTipService.GetTooltip(over)!.IsOpen.ShouldBeFalse();
    }

    #endregion

    #region Events

    [Fact]
    public void Events_OpenThenClose_AreRaisedInOrder()
    {
        Button button = CreateHost("hi");
        List<string> log = new();
        button.ToolTipOpening += (_, _) => log.Add("ToolTipOpening");
        button.ToolTipClosing += (_, _) => log.Add("ToolTipClosing");
        Tooltip tooltip = ToolTipService.GetTooltip(button)!;
        tooltip.Opened += (_, _) => log.Add("Opened");
        tooltip.Closed += (_, _) => log.Add("Closed");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);
        ToolTipTestHarness.Over(cursor, null);
        ToolTipTestHarness.TickTo(0.7);

        log.ShouldBe(new[] { "ToolTipOpening", "Opened", "ToolTipClosing", "Closed" });
    }

    [Fact]
    public void Events_ShowDurationTimeout_RaisesClosingBeforeClosed()
    {
        ToolTipService.ShowDuration = TimeSpan.FromMilliseconds(1000);
        Button button = CreateHost("hi");
        List<string> log = new();
        button.ToolTipClosing += (_, _) => log.Add("ToolTipClosing");
        ToolTipService.GetTooltip(button)!.Closed += (_, _) => log.Add("Closed");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);
        ToolTipTestHarness.TickTo(1.6);

        log.ShouldBe(new[] { "ToolTipClosing", "Closed" });
    }

    #endregion

    #region Content and registration

    [Fact]
    public void ToolTip_String_IsWrappedInATooltipWhosePlacementTargetIsTheHost()
    {
        Button button = new Button();

        button.ToolTip = "hello";

        Tooltip tooltip = ToolTipService.GetTooltip(button)!;
        tooltip.Content.ShouldBe("hello");
        tooltip.PlacementTarget.ShouldBeSameAs(button);
    }

    [Fact]
    public void ToolTip_TooltipInstance_IsUsedAsIs()
    {
        Button button = CreateHost("unused");
        Tooltip custom = new Tooltip();
        custom.Content = "custom";

        button.ToolTip = custom;

        button.ToolTip.ShouldBeSameAs(custom);
        ToolTipService.GetTooltip(button).ShouldBeSameAs(custom);
        custom.PlacementTarget.ShouldBeSameAs(button);

        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);
        custom.IsOpen.ShouldBeTrue();
    }

    [Fact]
    public void ToolTip_AssignedAfterHoverStarted_OpensAfterTheDelayCountedFromTheAssignment()
    {
        Button button = new Button();
        button.AddToRoot();
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.Over(cursor, button.Visual);
        ToolTipTestHarness.TickTo(0.0);
        ToolTipTestHarness.TickTo(1.0);

        button.ToolTip = "late";
        ToolTipTestHarness.TickTo(1.1); // first tick that sees the host starts the hover timer
        ToolTipTestHarness.TickTo(1.5);
        OpenCount.ShouldBe(0);

        ToolTipTestHarness.TickTo(1.7);
        OpenCount.ShouldBe(1);
    }

    [Fact]
    public void ToolTip_ReplacedWithAnotherString_OpensWithTheNewContent()
    {
        Button button = CreateHost("first");
        button.ToolTip = "second";
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);

        ToolTipService.GetTooltip(button)!.Content.ShouldBe("second");
        OpenCount.ShouldBe(1);
    }

    [Fact]
    public void SwappedVisual_KeepsTheControlsTooltip()
    {
        Button button = CreateHost("kept");
        ButtonVisual newVisual = new ButtonVisual(tryCreateFormsObject: false);
        button.Visual = newVisual;
        button.AddToRoot();
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, newVisual);

        ToolTipService.GetTooltip(button)!.IsOpen.ShouldBeTrue();
    }

    [Fact]
    public void ResetForTesting_ClosesTheOpenTooltip()
    {
        Button button = CreateHost("hi");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);

        ToolTipService.ResetForTesting();

        OpenCount.ShouldBe(0);
    }

    #endregion
}
