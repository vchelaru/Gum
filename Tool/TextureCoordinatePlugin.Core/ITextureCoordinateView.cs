using System;
using FlatRedBall.SpecializedXnaControls;
using Gum.Input;

namespace TextureCoordinateSelectionPlugin.Views;

/// <summary>
/// The texture-coordinates tab as the display controller sees it: the region-selection canvas,
/// the two scroll bars, and a few framework services. Each head builds its own view (the WPF
/// <c>MainControl</c>, the Avalonia view) and implements this over it.
/// </summary>
public interface ITextureCoordinateView
{
    /// <summary>The neutral canvas the head's control hosts.</summary>
    ImageRegionSelectionCore Canvas { get; }

    /// <summary>The control to hand to the tab manager.</summary>
    object View { get; }

    /// <summary>The view model the toolbar binds to.</summary>
    object? DataContext { get; set; }

    /// <summary>The vertical scroll bar beside the canvas.</summary>
    ICameraScrollBar VerticalScrollBar { get; }

    /// <summary>The horizontal scroll bar below the canvas.</summary>
    ICameraScrollBar HorizontalScrollBar { get; }

    /// <summary>Raised after the canvas control changes size.</summary>
    event Action? CanvasResized;

    /// <summary>
    /// Raised for a key pressed over the canvas. The handler sets <see cref="GumKeyEventArgs.Handled"/>
    /// and the head copies it back to its own event.
    /// </summary>
    event Action<GumKeyEventArgs>? KeyDown;

    /// <summary>Runs <paramref name="action"/> once the view has been laid out.</summary>
    void InvokeWhenLoaded(Action action);

    /// <summary>The app-wide base font size changed; heads with sized toolbar buttons update them.</summary>
    void UpdateButtonSizes(double baseFontSize);
}

/// <summary>
/// A scroll bar in the camera's own units: the range and viewport are doubles computed by
/// <see cref="Logic.ScrollBarLogic"/>, unlike the integer WinForms-style <c>IScrollBar</c>.
/// </summary>
public interface ICameraScrollBar
{
    /// <summary>The smallest value the bar can scroll to.</summary>
    double Minimum { get; set; }

    /// <summary>The largest value the bar can scroll to.</summary>
    double Maximum { get; set; }

    /// <summary>How much of the range is visible; sizes the thumb.</summary>
    double ViewportSize { get; set; }

    /// <summary>The current scroll position.</summary>
    double Value { get; set; }

    /// <summary>Raised whenever <see cref="Value"/> changes, by the user or by code.</summary>
    event EventHandler? ValueChanged;
}
