using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// ToolTipService.RefreshInterval: a callback tooltip asks its callback again while it is open (issue #7058).
/// </summary>
public class ToolTipRefreshIntervalTests : BaseTestClass
{
    public ToolTipRefreshIntervalTests()
    {
        Reset();
    }

    public override void Dispose()
    {
        Reset();
        base.Dispose();
    }

    private static void Reset()
    {
        ToolTipService.ResetForTesting();
        ToolTipService.InitialShowDelay = TimeSpan.FromMilliseconds(500);
        ToolTipService.ShowDuration = TimeSpan.FromMilliseconds(5000);
        ToolTipService.BetweenShowDelay = TimeSpan.FromMilliseconds(100);
        ToolTipService.RefreshInterval = TimeSpan.Zero;
    }

    private static int OpenCount => FrameworkElement.PopupRoot.Children.Count;

    private static ContainerRuntime CreateContainerInRoot()
    {
        ContainerRuntime container = new ContainerRuntime();
        Gum.GumService.Default.Root.Children.Add(container);
        return container;
    }

    private static string? ContentOf(InteractiveGue host) => ToolTipService.GetTooltip(host)!.Content as string;

    [Fact]
    public void RefreshInterval_DefaultsToOff()
    {
        ToolTipService.RefreshInterval.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void Off_CallbackIsCalledOnceOnOpenAndNeverWhileOpen()
    {
        int calls = 0;
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () => "n=" + ++calls);
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);

        for (double t = 1.0; t < 4.5; t += 0.5)
        {
            ToolTipTestHarness.TickTo(t);
        }

        calls.ShouldBe(1);
        ContentOf(row).ShouldBe("n=1");
    }

    [Fact]
    public void On_CallbackIsCalledEveryIntervalWhileOpen_AndTextUpdates()
    {
        ToolTipService.RefreshInterval = TimeSpan.FromSeconds(1);
        int calls = 0;
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () => "n=" + ++calls);
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);   // opens at 0.6
        calls.ShouldBe(1);

        ToolTipTestHarness.TickTo(1.5);
        calls.ShouldBe(1, "because less than an interval has passed since opening");

        ToolTipTestHarness.TickTo(1.6);
        calls.ShouldBe(2);
        ContentOf(row).ShouldBe("n=2");

        ToolTipTestHarness.TickTo(2.0);
        calls.ShouldBe(2);

        ToolTipTestHarness.TickTo(2.7);
        calls.ShouldBe(3);
        ContentOf(row).ShouldBe("n=3");
    }

    [Fact]
    public void On_StaticStringTooltip_IsUnaffected()
    {
        ToolTipService.RefreshInterval = TimeSpan.FromMilliseconds(100);
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, "fixed");
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);

        ToolTipTestHarness.TickTo(1.0);
        ToolTipTestHarness.TickTo(2.0);

        ContentOf(row).ShouldBe("fixed");
        OpenCount.ShouldBe(1);
    }

    [Fact]
    public void On_UnchangedText_DoesNotLayOutAgain()
    {
        ToolTipService.RefreshInterval = TimeSpan.FromSeconds(1);
        int calls = 0;
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () => { calls++; return "same"; });
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);
        ToolTipTestHarness.TickTo(1.0);   // baseline frame without a refresh

        int before = GraphicalUiElement.UpdateLayoutCallCount;
        ToolTipTestHarness.TickTo(1.1);
        int idleFrameLayouts = GraphicalUiElement.UpdateLayoutCallCount - before;

        before = GraphicalUiElement.UpdateLayoutCallCount;
        ToolTipTestHarness.TickTo(1.7);   // refresh due, text unchanged
        int refreshFrameLayouts = GraphicalUiElement.UpdateLayoutCallCount - before;

        calls.ShouldBe(2);
        refreshFrameLayouts.ShouldBe(idleFrameLayouts);
    }

    [Fact]
    public void On_ChangedText_UpdatesTheTooltipAndGrowsIt()
    {
        ToolTipService.RefreshInterval = TimeSpan.FromSeconds(1);
        string text = "hi";
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () => text);
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);
        InteractiveGue visual = ToolTipService.GetTooltip(row)!.Visual;
        float widthBefore = visual.AbsoluteWidth;
        float x = visual.X;

        text = "a considerably longer tooltip text";
        ToolTipTestHarness.TickTo(1.7);

        visual.AbsoluteWidth.ShouldBeGreaterThan(widthBefore);
        visual.X.ShouldBe(x, "because the tooltip stays anchored to where it opened");
    }

    [Fact]
    public void On_CallbackReturningNull_HidesTheTooltip_AndIsNotAskedAgainForThisHover()
    {
        ToolTipService.RefreshInterval = TimeSpan.FromSeconds(1);
        string? text = "now";
        int calls = 0;
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () => { calls++; return text; });
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);
        OpenCount.ShouldBe(1);

        text = null;
        ToolTipTestHarness.TickTo(1.7);
        OpenCount.ShouldBe(0);
        int callsAfterHide = calls;

        ToolTipTestHarness.TickTo(2.0);
        ToolTipTestHarness.TickTo(3.0);
        OpenCount.ShouldBe(0);
        calls.ShouldBe(callsAfterHide);

        // Leaving and returning opens it again once the callback has content.
        text = "back";
        ToolTipTestHarness.Over(cursor, null);
        ToolTipTestHarness.TickTo(3.1);
        ToolTipTestHarness.Over(cursor, row);
        ToolTipTestHarness.TickTo(4.0);
        ToolTipTestHarness.TickTo(4.6);
        OpenCount.ShouldBe(1);
        ContentOf(row).ShouldBe("back");
    }

    [Fact]
    public void On_ClosingTheTooltip_StopsRefreshes()
    {
        ToolTipService.RefreshInterval = TimeSpan.FromSeconds(1);
        int calls = 0;
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () => "n=" + ++calls);
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);

        ToolTipTestHarness.Over(cursor, null);
        ToolTipTestHarness.TickTo(0.7);
        OpenCount.ShouldBe(0);

        ToolTipTestHarness.TickTo(2.0);
        ToolTipTestHarness.TickTo(3.0);

        calls.ShouldBe(1);
    }

    [Fact]
    public void On_SwitchingToAnotherHost_RefreshesTheNewHostsCallbackOnly()
    {
        ToolTipService.RefreshInterval = TimeSpan.FromSeconds(1);
        int callsA = 0;
        int callsB = 0;
        ContainerRuntime a = CreateContainerInRoot();
        ContainerRuntime b = CreateContainerInRoot();
        ToolTipService.SetToolTip(a, () => "a" + ++callsA);
        ToolTipService.SetToolTip(b, () => "b" + ++callsB);
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, a);

        ToolTipTestHarness.Over(cursor, b);
        ToolTipTestHarness.TickTo(0.7);
        ToolTipTestHarness.TickTo(1.0);   // b is shown right away (between-show window)
        callsB.ShouldBe(1);

        ToolTipTestHarness.TickTo(2.2);

        callsA.ShouldBe(1);
        callsB.ShouldBe(2);
        ContentOf(b).ShouldBe("b2");
        ContentOf(a).ShouldBe("a1");
    }

    [Fact]
    public void IntervalChangedWhileOpen_AppliesFromTheNextCheck()
    {
        ToolTipService.RefreshInterval = TimeSpan.FromSeconds(10);
        int calls = 0;
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () => "n=" + ++calls);
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);
        ToolTipTestHarness.TickTo(1.0);
        calls.ShouldBe(1);

        ToolTipService.RefreshInterval = TimeSpan.FromSeconds(0.5);
        ToolTipTestHarness.TickTo(1.2);
        calls.ShouldBe(2);

        ToolTipService.RefreshInterval = TimeSpan.Zero;
        ToolTipTestHarness.TickTo(3.0);
        calls.ShouldBe(2, "because zero turns refreshing off");
    }

    [Fact]
    public void VerySmallInterval_RefreshesAtMostOncePerFrame()
    {
        ToolTipService.RefreshInterval = TimeSpan.FromTicks(1);
        int calls = 0;
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () => { calls++; return "x"; });
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);
        calls.ShouldBe(1);

        ToolTipTestHarness.TickTo(0.7);
        ToolTipTestHarness.TickTo(0.8);
        ToolTipTestHarness.TickTo(0.9);

        calls.ShouldBe(4);
    }

    [Fact]
    public void NegativeInterval_IsOff()
    {
        ToolTipService.RefreshInterval = TimeSpan.FromSeconds(-1);
        int calls = 0;
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () => { calls++; return "x"; });
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);

        ToolTipTestHarness.TickTo(2.0);

        calls.ShouldBe(1);
    }

    [Fact]
    public void On_CallbackThrowing_PropagatesOncePerInterval_AndLeavesTheTooltipOpen()
    {
        ToolTipService.RefreshInterval = TimeSpan.FromSeconds(1);
        bool shouldThrow = false;
        int calls = 0;
        ContainerRuntime row = CreateContainerInRoot();
        ToolTipService.SetToolTip(row, () =>
        {
            calls++;
            if (shouldThrow) { throw new InvalidOperationException("boom"); }
            return "ok";
        });
        Mock<ICursor> cursor = ToolTipTestHarness.SetupCursor();
        ToolTipTestHarness.HoverUntilOpen(cursor, row);
        shouldThrow = true;

        Should.Throw<InvalidOperationException>(() => ToolTipTestHarness.TickTo(1.7));
        ToolTipTestHarness.TickTo(1.8);
        ToolTipTestHarness.TickTo(1.9);

        calls.ShouldBe(2);
        OpenCount.ShouldBe(1);
    }
}
