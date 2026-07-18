using Gum.Services;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.ComponentModel;
using System.Linq;

namespace PerformanceMeasurementPlugin.ViewModels;

/// <summary>
/// Surfaces the editor's per-frame render-state-change counts so the cost of different
/// visuals can be compared (notably SpriteBatch-backed vs Apos.Shapes-backed). Polls the
/// active <see cref="Renderer"/> on a timer and raises change notifications for the bound view.
/// Also exposes the global sibling-ordering mode so it can be toggled live.
/// </summary>
public class PerformanceViewModel : INotifyPropertyChanged
{
    private readonly IUiTimer _uiTimer;

    /// <summary>
    /// SpriteBatch begins in the last frame — one per render-state change (texture/clip/blend/
    /// transform). Sourced from <see cref="SpriteRenderer.LastFrameDrawStates"/>.
    /// </summary>
    public int SpriteBatchBeginCount => ActiveRenderer?.SpriteRenderer?.LastFrameDrawStates.Count() ?? 0;

    /// <summary>
    /// Apos.Shapes ShapeBatch begins in the last frame. These live on a separate GPU command
    /// stream from the SpriteBatch, so each one is an extra batch flush that shape-backed
    /// visuals add over SpriteBatch-only visuals.
    /// </summary>
    public int ShapeBatchBeginCount => ActiveRenderer?.RenderStateChangeStatistics?.ShapeBatchBeginCount ?? 0;

    /// <summary>The combined batch begins across both command streams in the last frame.</summary>
    public int TotalRenderStateChanges => SpriteBatchBeginCount + ShapeBatchBeginCount;

    /// <summary>
    /// True when the renderer walks siblings depth-first (the legacy
    /// <see cref="HierarchicalOrderer"/>). Mutually exclusive with <see cref="SortByBatchKey"/>.
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
    /// True when the renderer regroups same-BatchKey draws into contiguous runs
    /// (<see cref="BatchKeyGroupedOrderer"/>), reducing batch flushes at the cost of a reorder
    /// pass. Setting this swaps the global <see cref="Renderer.SiblingOrdering"/>; the editor
    /// renders continuously, so the next frame (and the begin counts above) reflect the change.
    /// </summary>
    public bool SortByBatchKey
    {
        get => ReferenceEquals(Renderer.SiblingOrdering, BatchKeyGroupedOrderer.Instance);
        set
        {
            if (value == SortByBatchKey)
            {
                return;
            }

            Renderer.SiblingOrdering = value
                ? BatchKeyGroupedOrderer.Instance
                : HierarchicalOrderer.Instance;

            RaiseAllChanged();
        }
    }

    /// <summary>
    /// Toggles the global off-screen render cull (<see cref="Renderer.CullOffscreenWhenClipped"/>):
    /// when on, renderables that fall entirely outside an active clip region — and their children —
    /// are skipped. The editor renders continuously, so the begin counts above reflect the change on
    /// the next frame. Exposed here to validate the (experimental) cull against real projects (#2998).
    /// </summary>
    public bool CullOffscreenWhenClipped
    {
        get => Renderer.CullOffscreenWhenClipped;
        set
        {
            if (value == Renderer.CullOffscreenWhenClipped)
            {
                return;
            }

            Renderer.CullOffscreenWhenClipped = value;
            RaiseAllChanged();
        }
    }

    private static Renderer? ActiveRenderer => SystemManagers.Default?.Renderer;

    public event PropertyChangedEventHandler? PropertyChanged;

    public PerformanceViewModel(IUiTimer uiTimer)
    {
        _uiTimer = uiTimer;
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
