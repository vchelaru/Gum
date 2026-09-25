using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Gum.Avalonia.Canvas;

/// <summary>
/// Requests a canvas redraw on user input in any window, dialogs and popups included. Almost every
/// edit that changes the canvas (a variable typed in, a tree click, a hotkey, a dialog's OK) starts
/// with input, so this one hook covers them without an invalidation call in each feature (#4989).
/// </summary>
public static class CanvasInputRedrawHook
{
    /// <summary>Starts requesting redraws on input; dispose the result to stop.</summary>
    public static IDisposable Install(ICanvasRedrawScheduler scheduler)
    {
        // Tunnel routing starts at the TopLevel, so a class handler there sees every input event in
        // every window, handled or not. Drag-and-drop events only bubble.
        List<IDisposable> handlers = new List<IDisposable>
        {
            AddTunnel(InputElement.PointerPressedEvent, scheduler),
            AddTunnel(InputElement.PointerReleasedEvent, scheduler),
            AddTunnel(InputElement.PointerMovedEvent, scheduler),
            AddTunnel(InputElement.PointerWheelChangedEvent, scheduler),
            AddTunnel(InputElement.KeyDownEvent, scheduler),
            AddTunnel(InputElement.KeyUpEvent, scheduler),
            AddTunnel(InputElement.TextInputEvent, scheduler),
            DragDrop.DragOverEvent.AddClassHandler<TopLevel>((_, _) => scheduler.RequestRedraw(), RoutingStrategies.Bubble, handledEventsToo: true),
            DragDrop.DropEvent.AddClassHandler<TopLevel>((_, _) => scheduler.RequestRedraw(), RoutingStrategies.Bubble, handledEventsToo: true),
        };
        return new HandlerSet(handlers);
    }

    private static IDisposable AddTunnel<TEventArgs>(RoutedEvent<TEventArgs> routedEvent, ICanvasRedrawScheduler scheduler)
        where TEventArgs : RoutedEventArgs =>
        routedEvent.AddClassHandler<TopLevel>((_, _) => scheduler.RequestRedraw(), RoutingStrategies.Tunnel, handledEventsToo: true);

    private sealed class HandlerSet : IDisposable
    {
        private readonly List<IDisposable> _handlers;

        public HandlerSet(List<IDisposable> handlers)
        {
            _handlers = handlers;
        }

        public void Dispose()
        {
            foreach (IDisposable handler in _handlers)
            {
                handler.Dispose();
            }
            _handlers.Clear();
        }
    }
}
