using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Threading;

namespace PerformanceMeasurementPlugin.ViewModels;

/// <summary>
/// Surfaces the editor's per-frame render-state-change counts so the cost of different
/// visuals can be compared (notably SpriteBatch-backed vs Apos.Shapes-backed). Polls the
/// active <see cref="Renderer"/> on a timer and raises change notifications for the bound view.
/// </summary>
public class PerformanceViewModel : INotifyPropertyChanged
{
    DispatcherTimer mTimer;

    /// <summary>
    /// SpriteBatch begins in the last frame — one per render-state change (texture/clip/blend/
    /// transform). Sourced from <see cref="SpriteRenderer.LastFrameDrawStates"/>.
    /// </summary>
    public int SpriteBatchBeginCount => Renderer?.SpriteRenderer?.LastFrameDrawStates.Count() ?? 0;

    /// <summary>
    /// Apos.Shapes ShapeBatch begins in the last frame. These live on a separate GPU command
    /// stream from the SpriteBatch, so each one is an extra batch flush that shape-backed
    /// visuals add over SpriteBatch-only visuals.
    /// </summary>
    public int ShapeBatchBeginCount => Renderer?.RenderStateChangeStatistics?.ShapeBatchBeginCount ?? 0;

    /// <summary>The combined batch begins across both command streams in the last frame.</summary>
    public int TotalRenderStateChanges => SpriteBatchBeginCount + ShapeBatchBeginCount;

    private static Renderer? Renderer => SystemManagers.Default?.Renderer;

    public event PropertyChangedEventHandler? PropertyChanged;

    public PerformanceViewModel()
    {
        mTimer = new DispatcherTimer();
        mTimer.Tick += HandleTick;
        mTimer.Interval = TimeSpan.FromMilliseconds(500);

        mTimer.Start();
    }

    private void HandleTick(object? sender, EventArgs e)
    {
        // Null property name asks WPF to re-read every bound property on this VM.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}
