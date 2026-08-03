using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Threading;
using Gum.Controls.DataUi;
using Gum.Diagnostics;
using Gum.Plugins.VariableGrid;
using WpfDataUi.Controls;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Proactively instantiates the WPF displayer controls used by the Variables tab so that each
/// control type's one-time style/template resolution and BAML parse cost lands during startup
/// idle time instead of on the user's first click into the grid (#4283).
/// </summary>
/// <remarks>
/// Instances are constructed but never added to a visual tree or shown - construction alone is
/// enough to force WPF to resolve the type's style/template. Each construction is queued as its
/// own low-priority dispatcher callback so real UI work (window paint, user input) is always free
/// to run first rather than waiting behind one large block of warm-up work.
/// </remarks>
internal static class VariableGridDisplayerWarmup
{
    private static readonly Func<UserControl>[] Factories =
    {
        () => new TextBoxDisplay(),
        () => new SliderDisplay(),
        () => new ComboBoxDisplay(),
        () => new CheckBoxDisplay(),
        () => new NullableBoolDisplay(),
        () => new AngleSelectorDisplay(),
        () => new FileSelectionDisplay(),
        () => new MultiLineTextBoxDisplay(),
        () => new StringListTextBoxDisplay(),
        () => new ListBoxDisplay(),
        () => new ToggleButtonOptionDisplay(Array.Empty<ToggleButtonOptionDisplay.Option>()),
        () => new ColorDisplay(),
        () => new VariableRemoveButton(),
    };

    // Keeps constructed instances reachable for the lifetime of the app. Not required for
    // correctness (nothing here is reused later), but avoids relying on GC timing mid-warm-up.
    private static readonly List<UserControl> WarmedControls = new();

    /// <summary>Number of displayer types warmed so far. For diagnostics only.</summary>
    public static int WarmedCount => WarmedControls.Count;

    /// <summary>Total number of displayer types this pass will warm.</summary>
    public static int TotalCount => Factories.Length;

    /// <summary>
    /// Queues one <see cref="DispatcherPriority.Background"/> callback per displayer type on the
    /// given dispatcher. Safe to call multiple times - only the first call schedules work.
    /// </summary>
    public static void ScheduleWarmUp(Dispatcher dispatcher)
    {
        if (WarmedControls.Count > 0 || Factories.Length == 0)
        {
            return;
        }

        StartupPerfLog.Log("VariableGridDisplayerWarmup.ScheduleWarmUp queued");
        ScheduleNext(dispatcher, index: 0);
    }

    private static void ScheduleNext(Dispatcher dispatcher, int index)
    {
        if (index >= Factories.Length)
        {
            StartupPerfLog.Log($"VariableGridDisplayerWarmup complete ({WarmedControls.Count}/{Factories.Length})");
            return;
        }

        dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            var factory = Factories[index];
            try
            {
                WarmedControls.Add(factory());
            }
            catch (Exception exception)
            {
                // Warm-up is a best-effort perf optimization - a failure here must never surface
                // to the user or block the rest of the grid from working normally.
                StartupPerfLog.Log($"VariableGridDisplayerWarmup[{index}] failed: {exception.Message}");
            }

            ScheduleNext(dispatcher, index + 1);
        }));
    }
}
