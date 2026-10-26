using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace AvaloniaDataUi.Controls;

/// <summary>
/// A left-button drag on a handle control that holds the pointer capture while it runs. The drag
/// ends exactly once, on release, when its row is pointed at another member, or when the capture is lost (Alt+Tab, another window or control
/// taking the pointer), so an editor never keeps dragging on hover or skips its final commit.
/// Every drag or scrub editor in the Variables tab uses this rather than wiring capture itself.
/// </summary>
public static class CapturedPointerDrag
{
    /// <summary>Wires a captured drag onto <paramref name="handle"/>.</summary>
    /// <param name="handle">The control pressed to start the drag; it takes the pointer capture.</param>
    /// <param name="tryBegin">Called on a left-button press; returns false to leave the press alone.</param>
    /// <param name="move">Called for each pointer move while the drag runs.</param>
    /// <param name="end">Called once when the drag ends, by release or capture loss.</param>
    public static void Attach(Control handle, Func<PointerPressedEventArgs, bool> tryBegin, Action<PointerEventArgs> move, Action end)
    {
        bool isDragging = false;
        DataUiDisplayBase? owner = null;

        void EndForRebind(object? sender, EventArgs e) => End();

        void End()
        {
            if (isDragging)
            {
                isDragging = false;
                if (owner != null)
                {
                    owner.InstanceMemberChanging -= EndForRebind;
                    owner = null;
                }
                end();
            }
        }

        handle.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(handle).Properties.IsLeftButtonPressed || !tryBegin(e))
            {
                return;
            }
            isDragging = true;
            // The row can be pointed at another member while the button is held (another element
            // selected); the drag then ends against the member it began on.
            owner = handle.FindAncestorOfType<DataUiDisplayBase>();
            if (owner != null)
            {
                owner.InstanceMemberChanging += EndForRebind;
            }
            e.Pointer.Capture(handle);
            e.Handled = true;
        };
        handle.PointerMoved += (_, e) =>
        {
            if (isDragging)
            {
                move(e);
            }
        };
        handle.PointerReleased += (_, e) =>
        {
            if (isDragging)
            {
                End();
                e.Pointer.Capture(null);
            }
        };
        handle.PointerCaptureLost += (_, _) => End();
    }
}
