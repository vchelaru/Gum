#nullable enable

using System;

namespace Gum.Input;

// The host-independent half of an interactive Skia host's Cursor, compiled into each host assembly
// (SilkNetGum, StrideGum, UnityGum) through SkiaInputHost.props. A host adds its own Cursor.<Host>.cs
// with GetMouseState, which reads ScrollWheelValue from here.
public partial class Cursor : IDisposable
{
    // Undoes the host event subscriptions registered with OnDispose.
    private Action? _unsubscribe;

    // Running scroll total in XNA units (120 per notch), which the shared ScrollWheelChange math
    // in Cursor.cs expects. Hosts report notches, not totals.
    private double _scrollWheelUnits;

    // Touches the host supplies when IsMobile is true. Empty for hosts that don't wire touch.
    private TouchCollection _hostTouches = new TouchCollection();

    private const int ScrollUnitsPerNotch = 120;

    private int ScrollWheelValue => (int)_scrollWheelUnits;

    private void AddScrollUnits(double units) => _scrollWheelUnits += units;

    // Registers the unsubscribe for a host event subscription made when the cursor is attached.
    private void OnDispose(Action unsubscribe) => _unsubscribe += unsubscribe;

    /// <summary>
    /// Undoes the host event subscriptions. <c>GumService.Uninitialize</c> and a second
    /// <c>Initialize</c> call this on the cursor the service created.
    /// </summary>
    public void Dispose()
    {
        _unsubscribe?.Invoke();
        _unsubscribe = null;
    }

    private TouchCollection GetTouchCollection() => _hostTouches;

    // Skia hosts draw into a canvas that fills the window, so there is no viewport inset.
    private int? GetViewportLeft() => 0;

    private int? GetViewportTop() => 0;
}
