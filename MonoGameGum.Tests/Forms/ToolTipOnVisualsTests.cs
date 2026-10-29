using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// ToolTipService.SetToolTip on plain visuals (TextRuntime, ContainerRuntime, NineSliceRuntime...)
/// and tooltip content supplied by a callback when the tooltip opens (issue #7058).
/// </summary>
public class ToolTipOnVisualsTests : BaseTestClass
{
    public ToolTipOnVisualsTests()
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

    private static ContainerRuntime CreateContainerInRoot()
    {
        ContainerRuntime container = new ContainerRuntime();
        Gum.GumService.Default.Root.Children.Add(container);
        return container;
    }

    private static bool IsOpen(InteractiveGue visual) => ToolTipService.GetTooltip(visual)!.IsOpen;

    #region Registering plain visuals

    [Fact]
    public void SetToolTip_OnTextRuntime_OpensOnHover()
    {
        TextRuntime label = new TextRuntime();
        label.HasEvents = true;
        Gum.GumService.Default.Root.Children.Add(label);
        ToolTipService.SetToolTip(label, "a label");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, label);

        OpenCount.ShouldBe(1);
        ToolTipService.GetTooltip(label)!.Content.ShouldBe("a label");
    }

    [Fact]
    public void SetToolTip_OnContainerRuntime_OpensOnHover()
    {
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, "a row");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, row);

        OpenCount.ShouldBe(1);
    }

    [Fact]
    public void SetToolTip_OnNineSliceRuntime_OpensOnHover()
    {
        NineSliceRuntime window = new NineSliceRuntime();
        window.HasEvents = true;
        Gum.GumService.Default.Root.Children.Add(window);
        ToolTipService.SetToolTip(window, "a window");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, window);

        OpenCount.ShouldBe(1);
    }

    [Fact]
    public void SetToolTip_OnVisual_FollowsTheSameDelaysAsForms()
    {
        ToolTipService.ShowDuration = TimeSpan.FromMilliseconds(1000);
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, "a row");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.Over(cursor, row);

        ToolTipTestHarness.TickTo(0.0);
        ToolTipTestHarness.TickTo(0.49);
        OpenCount.ShouldBe(0);

        ToolTipTestHarness.TickTo(0.5);
        OpenCount.ShouldBe(1);

        ToolTipTestHarness.TickTo(1.4);
        OpenCount.ShouldBe(1);

        ToolTipTestHarness.TickTo(1.6);
        OpenCount.ShouldBe(0);
    }

    [Fact]
    public void SetToolTip_OnVisual_LeavingTheVisualClosesTheTooltip()
    {
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, "a row");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);

        ToolTipTestHarness.Over(cursor, null);
        ToolTipTestHarness.TickTo(0.7);

        OpenCount.ShouldBe(0);
    }

    [Fact]
    public void SetToolTip_WithATooltipInstance_UsesItAsIs()
    {
        ContainerRuntime row = CreateContainerInRoot();
        Tooltip custom = new Tooltip();
        custom.Content = "custom";

        ToolTipService.SetToolTip(row, custom);

        ToolTipService.GetTooltip(row).ShouldBeSameAs(custom);
    }

    [Fact]
    public void SetToolTip_WithUnsupportedContent_ThrowsAndKeepsThePreviousTooltip()
    {
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, "kept");

        Should.Throw<NotSupportedException>(() => ToolTipService.SetToolTip(row, 42));

        ToolTipService.GetTooltip(row)!.Content.ShouldBe("kept");
    }

    [Fact]
    public void SetToolTip_WithANullVisual_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ToolTipService.SetToolTip(null!, "x"));
    }

    [Fact]
    public void SetToolTip_Null_RemovesTheTooltip()
    {
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, "a row");

        ToolTipService.SetToolTip(row, (object?)null);

        ToolTipService.GetTooltip(row).ShouldBeNull();
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);
        OpenCount.ShouldBe(0);
    }

    [Fact]
    public void SetToolTip_NullCallback_RemovesTheTooltip()
    {
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () => "a row");

        ToolTipService.SetToolTip(row, (Func<object?>?)null);

        ToolTipService.GetTooltip(row).ShouldBeNull();
    }

    [Fact]
    public void SetToolTip_Null_WhileOpen_ClosesIt()
    {
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, "a row");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);
        OpenCount.ShouldBe(1);

        ToolTipService.SetToolTip(row, (object?)null);

        OpenCount.ShouldBe(0);
    }

    [Fact]
    public void SetToolTip_CalledAgain_ReplacesTheContent()
    {
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, "first");
        ToolTipService.SetToolTip(row, "second");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, row);

        ToolTipService.GetTooltip(row)!.Content.ShouldBe("second");
    }

    [Fact]
    public void Registration_IsWeak_SoACollectedVisualLeavesNoEntryBehind()
    {
        (WeakReference visual, WeakReference tooltip) = RegisterAndDrop();

        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        visual.IsAlive.ShouldBeFalse("because the registry must not keep the visual alive");
        tooltip.IsAlive.ShouldBeFalse("because the registry entry goes away with its host");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference, WeakReference) RegisterAndDrop()
    {
        ContainerRuntime row = new ContainerRuntime();
        ToolTipService.SetToolTip(row, () => "a row");
        return (new WeakReference(row), new WeakReference(ToolTipService.GetTooltip(row)));
    }

    #endregion

    #region Callback content

    [Fact]
    public void Callback_IsNotEvaluatedUntilTheTooltipOpens()
    {
        int calls = 0;
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () => { calls++; return "stock: 5"; });
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.Over(cursor, row);

        calls.ShouldBe(0, "because registering does not evaluate");
        ToolTipTestHarness.TickTo(0.0);
        ToolTipTestHarness.TickTo(0.4);
        calls.ShouldBe(0, "because the initial delay has not elapsed");

        ToolTipTestHarness.TickTo(0.6);
        calls.ShouldBe(1);
        ToolTipService.GetTooltip(row)!.Content.ShouldBe("stock: 5");
    }

    [Fact]
    public void Callback_IsNotReEvaluatedWhileTheTooltipIsOpen()
    {
        int calls = 0;
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () => { calls++; return "stock: " + calls; });
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);

        ToolTipTestHarness.TickTo(1.0);
        ToolTipTestHarness.TickTo(2.0);
        ToolTipTestHarness.TickTo(3.0);

        calls.ShouldBe(1);
        ToolTipService.GetTooltip(row)!.Content.ShouldBe("stock: 1");
    }

    [Fact]
    public void Callback_IsEvaluatedAgainOnTheNextOpen()
    {
        int stock = 5;
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () => "stock: " + stock);
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);
        ToolTipService.GetTooltip(row)!.Content.ShouldBe("stock: 5");

        ToolTipTestHarness.Over(cursor, null);
        ToolTipTestHarness.TickTo(0.7);
        OpenCount.ShouldBe(0);

        stock = 9;
        ToolTipTestHarness.Over(cursor, row);
        ToolTipTestHarness.TickTo(2.0);
        ToolTipTestHarness.TickTo(2.6);

        OpenCount.ShouldBe(1);
        ToolTipService.GetTooltip(row)!.Content.ShouldBe("stock: 9");
    }

    [Fact]
    public void Callback_ReturningNull_OpensNothingAndIsNotRetriedWhileHoveringTheSameHost()
    {
        int calls = 0;
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () => { calls++; return null; });
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, row);
        ToolTipTestHarness.TickTo(1.0);
        ToolTipTestHarness.TickTo(2.0);

        OpenCount.ShouldBe(0);
        calls.ShouldBe(1);
    }

    [Fact]
    public void Callback_ReturningNonString_ThrowsNotSupportedAndLeavesNothingOpen()
    {
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () => 42);
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.Over(cursor, row);
        ToolTipTestHarness.TickTo(0.0);

        Should.Throw<NotSupportedException>(() => ToolTipTestHarness.TickTo(0.6));

        OpenCount.ShouldBe(0);
    }

    [Fact]
    public void Callback_Throwing_PropagatesOnce_LeavesNothingOpen_AndRecoversOnTheNextHover()
    {
        bool shouldThrow = true;
        int calls = 0;
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () =>
        {
            calls++;
            if (shouldThrow) { throw new InvalidOperationException("game state not ready"); }
            return "ok";
        });
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.Over(cursor, row);
        ToolTipTestHarness.TickTo(0.0);

        Should.Throw<InvalidOperationException>(() => ToolTipTestHarness.TickTo(0.6));
        OpenCount.ShouldBe(0);

        // Still hovering the same host: no throw on every following frame.
        ToolTipTestHarness.TickTo(0.7);
        ToolTipTestHarness.TickTo(0.8);
        calls.ShouldBe(1);
        OpenCount.ShouldBe(0);

        // Leave and come back after the callback is healthy again.
        shouldThrow = false;
        ToolTipTestHarness.Over(cursor, null);
        ToolTipTestHarness.TickTo(1.0);
        ToolTipTestHarness.Over(cursor, row);
        ToolTipTestHarness.TickTo(2.0);
        ToolTipTestHarness.TickTo(2.6);

        OpenCount.ShouldBe(1);
        ToolTipService.GetTooltip(row)!.Content.ShouldBe("ok");
    }

    [Fact]
    public void Callback_Throwing_DoesNotBlockAnotherHostsTooltip()
    {
        ContainerRuntime bad = CreateContainerInRoot();
        ToolTipService.SetToolTip(bad, () => throw new InvalidOperationException("boom"));
        ContainerRuntime good = CreateContainerInRoot();
        ToolTipService.SetToolTip(good, "fine");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.Over(cursor, bad);
        ToolTipTestHarness.TickTo(0.0);
        Should.Throw<InvalidOperationException>(() => ToolTipTestHarness.TickTo(0.6));

        ToolTipTestHarness.Over(cursor, good);
        ToolTipTestHarness.TickTo(0.7);
        ToolTipTestHarness.TickTo(1.4);

        IsOpen(good).ShouldBeTrue();
    }

    #endregion

    #region Innermost host and mixed nesting

    [Fact]
    public void Nested_VisualsInsideVisuals_TheInnermostRegisteredHostWins()
    {
        ContainerRuntime window = CreateContainerInRoot();
        ContainerRuntime row = new ContainerRuntime();
        window.Children.Add(row);
        TextRuntime label = new TextRuntime();
        label.HasEvents = true;
        row.Children.Add(label);
        ToolTipService.SetToolTip(window, "window");
        ToolTipService.SetToolTip(row, "row");
        ToolTipService.SetToolTip(label, "label");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, label);

        IsOpen(label).ShouldBeTrue();
        IsOpen(row).ShouldBeFalse();
        IsOpen(window).ShouldBeFalse();
    }

    [Fact]
    public void Nested_HoveringAChildWithoutATooltip_FallsBackToTheNearestRegisteredAncestor()
    {
        ContainerRuntime window = CreateContainerInRoot();
        ContainerRuntime row = new ContainerRuntime();
        window.Children.Add(row);
        ContainerRuntime icon = new ContainerRuntime();
        row.Children.Add(icon);
        ToolTipService.SetToolTip(window, "window");
        ToolTipService.SetToolTip(row, "row");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, icon);

        IsOpen(row).ShouldBeTrue();
        IsOpen(window).ShouldBeFalse();
    }

    [Fact]
    public void Nested_VisualInsideAFormsControl_TheVisualWinsWhenHoveredAndTheControlOtherwise()
    {
        StackPanel panel = new StackPanel();
        panel.AddToRoot();
        panel.ToolTip = "panel";
        ContainerRuntime icon = new ContainerRuntime();
        panel.Visual.Children.Add(icon);
        ToolTipService.SetToolTip(icon, "icon");
        ContainerRuntime plainChild = new ContainerRuntime();
        panel.Visual.Children.Add(plainChild);
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, icon);
        IsOpen(icon).ShouldBeTrue();
        ToolTipService.GetTooltip(panel)!.IsOpen.ShouldBeFalse();

        ToolTipTestHarness.Over(cursor, plainChild);
        ToolTipTestHarness.TickTo(0.7);
        IsOpen(icon).ShouldBeFalse();
        ToolTipService.GetTooltip(panel)!.IsOpen.ShouldBeTrue();
    }

    [Fact]
    public void Nested_FormsControlInsideAVisual_TheControlWinsWhenItHasATooltip()
    {
        ContainerRuntime window = CreateContainerInRoot();
        ToolTipService.SetToolTip(window, "window");
        Button button = new Button();
        window.AddChild(button);
        button.ToolTip = "button";
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);

        ToolTipService.GetTooltip(button)!.IsOpen.ShouldBeTrue();
        IsOpen(window).ShouldBeFalse();
    }

    [Fact]
    public void Nested_FormsControlWithoutATooltipInsideAVisual_FallsBackToTheVisual()
    {
        ContainerRuntime window = CreateContainerInRoot();
        ToolTipService.SetToolTip(window, "window");
        Button button = new Button();
        window.AddChild(button);
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);

        IsOpen(window).ShouldBeTrue();
    }

    [Fact]
    public void Tie_ControlAndItsOwnVisualBothRegistered_TheControlsRegistrationWins()
    {
        Button button = new Button();
        button.AddToRoot();
        ToolTipService.SetToolTip(button.Visual, "from the visual");
        button.ToolTip = "from the control";
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);

        ToolTipService.GetTooltip(button)!.IsOpen.ShouldBeTrue();
        IsOpen(button.Visual).ShouldBeFalse();
    }

    [Fact]
    public void Tie_ControlWithoutATooltipButItsVisualRegistered_TheVisualsTooltipShows()
    {
        Button button = new Button();
        button.AddToRoot();
        ToolTipService.SetToolTip(button.Visual, "from the visual");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, button.Visual);

        IsOpen(button.Visual).ShouldBeTrue();
    }

    [Fact]
    public void WindowPushed_VisualHostIsUsedInsteadOfTheVisualOver()
    {
        ContainerRuntime pushed = CreateContainerInRoot();
        ToolTipService.SetToolTip(pushed, "pushed");
        ContainerRuntime over = CreateContainerInRoot();
        ToolTipService.SetToolTip(over, "over");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.Over(cursor, over);
        ToolTipTestHarness.Pushed(cursor, pushed);

        ToolTipTestHarness.TickTo(0.0);
        ToolTipTestHarness.TickTo(0.6);

        IsOpen(pushed).ShouldBeTrue();
        IsOpen(over).ShouldBeFalse();
    }

    #endregion

    #region HasEvents

    [Fact]
    public void VisualWithHasEventsFalse_RegisteringDoesNotThrow_AndNeverOpensWhenTheCursorIsNotOverIt()
    {
        TextRuntime label = new TextRuntime();
        label.HasEvents.ShouldBeFalse("because TextRuntime does not take cursor events unless asked");
        Gum.GumService.Default.Root.Children.Add(label);

        ToolTipService.SetToolTip(label, "a label");

        // The cursor never reports a HasEvents == false visual as VisualOver.
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.Over(cursor, null);
        ToolTipTestHarness.TickTo(0.0);
        ToolTipTestHarness.TickTo(1.0);
        OpenCount.ShouldBe(0);
    }

    [Fact]
    public void VisualWithHasEventsFalse_StillAppliesWhileTheCursorIsOverAnEventEnabledDescendant()
    {
        ContainerRuntime row = CreateContainerInRoot();
        row.HasEvents = false;
        ToolTipService.SetToolTip(row, "row");
        ContainerRuntime child = new ContainerRuntime();
        row.Children.Add(child);
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();

        ToolTipTestHarness.HoverUntilOpen(cursor, child);

        IsOpen(row).ShouldBeTrue();
    }

    #endregion
}
