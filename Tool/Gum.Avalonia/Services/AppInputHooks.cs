using System;
using Gum.Avalonia.Canvas;
using Gum.Services;

namespace Gum.Avalonia.Services;

/// <summary>Installs the app-wide input hooks the head runs with.</summary>
public static class AppInputHooks
{
    /// <summary>
    /// Installs <see cref="CanvasInputRedrawHook"/> and <see cref="SecondaryClickHook"/> once each;
    /// dispose the result to remove both.
    /// </summary>
    public static IDisposable Install(ICanvasRedrawScheduler redrawScheduler, IOperatingSystemInfo operatingSystem) =>
        new HookPair(CanvasInputRedrawHook.Install(redrawScheduler), SecondaryClickHook.Install(operatingSystem));

    private sealed class HookPair : IDisposable
    {
        private readonly IDisposable _redraw;
        private readonly IDisposable _secondaryClick;

        public HookPair(IDisposable redraw, IDisposable secondaryClick)
        {
            _redraw = redraw;
            _secondaryClick = secondaryClick;
        }

        public void Dispose()
        {
            _redraw.Dispose();
            _secondaryClick.Dispose();
        }
    }
}
