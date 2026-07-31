using System;
using Gum.Controls;
using Shouldly;
using WpfScrollBar = System.Windows.Controls.Primitives.ScrollBar;

namespace GumToolUnitTests.Controls;

/// <summary>
/// Pins the WinForms-to-WPF range conversion in <see cref="WpfScrollBarAdapter"/>: the two
/// frameworks disagree about whether the last screenful of the scrolled area is reachable, and this
/// adapter is the only place that difference is reconciled.
/// </summary>
public class WpfScrollBarAdapterTests
{
    [StaFact]
    public void Maximum_ExcludesTheVisiblePageFromTheReachableRange()
    {
        WpfScrollBar scrollBar = new WpfScrollBar();
        WpfScrollBarAdapter adapter = new WpfScrollBarAdapter(scrollBar);

        adapter.LargeChange = 800;
        adapter.Minimum = -400;
        adapter.Maximum = 1600;

        scrollBar.Minimum.ShouldBe(-400);
        scrollBar.Maximum.ShouldBe(801);
        scrollBar.ViewportSize.ShouldBe(800);
    }

    [StaFact]
    public void Range_SurvivesBeingMovedBelowItsPreviousMinimum()
    {
        WpfScrollBar scrollBar = new WpfScrollBar();
        WpfScrollBarAdapter adapter = new WpfScrollBarAdapter(scrollBar);
        adapter.LargeChange = 1;
        adapter.Minimum = 500;
        adapter.Maximum = 2000;

        adapter.Minimum = -400;
        adapter.Maximum = 100;

        scrollBar.Minimum.ShouldBe(-400);
        scrollBar.Maximum.ShouldBe(100);
    }

    [StaFact]
    public void Value_IsClampedToTheReachableMaximum()
    {
        WpfScrollBar scrollBar = new WpfScrollBar();
        WpfScrollBarAdapter adapter = new WpfScrollBarAdapter(scrollBar);
        adapter.LargeChange = 200;
        adapter.Minimum = 0;
        adapter.Maximum = 1000;

        adapter.Value = 5000;

        adapter.Value.ShouldBe(801);
    }

    [StaFact]
    public void ValueChanged_IsRaisedWhenTheWrappedBarMoves()
    {
        WpfScrollBar scrollBar = new WpfScrollBar();
        WpfScrollBarAdapter adapter = new WpfScrollBarAdapter(scrollBar);
        adapter.LargeChange = 1;
        adapter.Minimum = 0;
        adapter.Maximum = 1000;
        int raisedCount = 0;
        adapter.ValueChanged += (_, _) => raisedCount++;

        scrollBar.Value = 42;

        raisedCount.ShouldBe(1);
        adapter.Value.ShouldBe(42);
    }
}
