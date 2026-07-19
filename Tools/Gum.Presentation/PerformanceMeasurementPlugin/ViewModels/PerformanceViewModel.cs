using Gum.Services;
using System;
using System.ComponentModel;

namespace PerformanceMeasurementPlugin.ViewModels;

/// <summary>
/// Surfaces the editor's per-frame render-state-change counts so the cost of different
/// visuals can be compared (notably SpriteBatch-backed vs Apos.Shapes-backed). Reads the active
/// renderer via <see cref="IRenderDiagnosticsService"/> (kept out of this headless assembly's
/// reach — see that interface) and polls it on a timer, raising change notifications for the
/// bound view. Also exposes the global sibling-ordering mode so it can be toggled live.
/// </summary>
public class PerformanceViewModel : INotifyPropertyChanged
{
    private readonly IUiTimer _uiTimer;
    private readonly IRenderDiagnosticsService _renderDiagnostics;

    /// <summary>SpriteBatch begins in the last frame — one per render-state change.</summary>
    public int SpriteBatchBeginCount => _renderDiagnostics.SpriteBatchBeginCount;

    /// <summary>Apos.Shapes ShapeBatch begins in the last frame.</summary>
    public int ShapeBatchBeginCount => _renderDiagnostics.ShapeBatchBeginCount;

    /// <summary>The combined batch begins across both command streams in the last frame.</summary>
    public int TotalRenderStateChanges => SpriteBatchBeginCount + ShapeBatchBeginCount;

    /// <summary>
    /// True when the renderer walks siblings depth-first (the legacy ordering). Mutually
    /// exclusive with <see cref="SortByBatchKey"/>.
    /// </summary>
    public bool RenderDepthFirst
    {
        get => !SortByBatchKey;
        set
        {
            if (value)
            {
                SortByBatchKey = false;
            }
        }
    }

    /// <summary>
    /// True when the renderer regroups same-BatchKey draws into contiguous runs, reducing batch
    /// flushes at the cost of a reorder pass. Setting this swaps the renderer's global sibling
    /// ordering; the editor renders continuously, so the next frame (and the begin counts above)
    /// reflect the change.
    /// </summary>
    public bool SortByBatchKey
    {
        get => _renderDiagnostics.SortByBatchKey;
        set
        {
            if (value == SortByBatchKey)
            {
                return;
            }

            _renderDiagnostics.SortByBatchKey = value;
            RaiseAllChanged();
        }
    }

    /// <summary>
    /// Toggles the renderer's off-screen render cull: when on, renderables that fall entirely
    /// outside an active clip region — and their children — are skipped. The editor renders
    /// continuously, so the begin counts above reflect the change on the next frame. Exposed here
    /// to validate the (experimental) cull against real projects (#2998).
    /// </summary>
    public bool CullOffscreenWhenClipped
    {
        get => _renderDiagnostics.CullOffscreenWhenClipped;
        set
        {
            if (value == _renderDiagnostics.CullOffscreenWhenClipped)
            {
                return;
            }

            _renderDiagnostics.CullOffscreenWhenClipped = value;
            RaiseAllChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PerformanceViewModel(IUiTimer uiTimer, IRenderDiagnosticsService renderDiagnostics)
    {
        _uiTimer = uiTimer;
        _renderDiagnostics = renderDiagnostics;
        _uiTimer.Tick += HandleTick;
        _uiTimer.Start(TimeSpan.FromMilliseconds(500));
    }

    private void HandleTick()
    {
        RaiseAllChanged();
    }

    private void RaiseAllChanged()
    {
        // Null property name asks WPF to re-read every bound property on this VM.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}
