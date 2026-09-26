using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Gum.Managers;
using Gum.SelectionHistory;
using Gum.Services.Dialogs;

namespace Gum.Avalonia.Shell;

/// <summary>
/// Pointer and drag gestures the main window handles for everything inside it, whatever control
/// is under the pointer. Kept apart from <see cref="MainWindow"/> so a headless test window
/// runs the same code.
/// </summary>
internal static class AppWideWindowGestures
{
    /// <summary>The mouse's back and forward side buttons step through selection history.</summary>
    public static void RouteSelectionHistoryButtons(Window window, ISelectionHistory selectionHistory)
    {
        window.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            PointerUpdateKind kind = e.GetCurrentPoint(window).Properties.PointerUpdateKind;
            if (kind == PointerUpdateKind.XButton1Pressed)
            {
                selectionHistory.NavigateBack();
                e.Handled = true;
            }
            else if (kind == PointerUpdateKind.XButton2Pressed)
            {
                selectionHistory.NavigateForward();
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Dropping a project file anywhere in <paramref name="window"/> opens it, the same as File &gt;
    /// Load Project. Avalonia's drag events only bubble, so the control under the pointer sees the
    /// drag first and the window then overrides its answer. The canvas (<see cref="WireframeDropPayload"/>)
    /// and the tree (<see cref="DragDropManager.OnFilesDroppedInTreeView"/>) do nothing with a drop
    /// that carries a project file.
    /// </summary>
    public static void OpenDroppedProjects(Window window, IProjectFileDropLogic dropLogic, IDialogService dialogService)
    {
        DragDrop.SetAllowDrop(window, true);
        EventHandler<DragEventArgs> acceptProjectDrag = (_, e) =>
        {
            if (dropLogic.GetProjectFileToOpen(DroppedFiles(e)) != null)
            {
                e.DragEffects = DragDropEffects.Copy;
                e.Handled = true;
            }
        };
        window.AddHandler(DragDrop.DragEnterEvent, acceptProjectDrag, RoutingStrategies.Bubble, handledEventsToo: true);
        window.AddHandler(DragDrop.DragOverEvent, acceptProjectDrag, RoutingStrategies.Bubble, handledEventsToo: true);
        window.AddHandler(DragDrop.DropEvent, (_, e) =>
        {
            List<string> droppedFiles = DroppedFiles(e);
            if (dropLogic.GetProjectFileToOpen(droppedFiles) == null)
            {
                return;
            }
            e.DragEffects = DragDropEffects.Copy;
            e.Handled = true;
            OpenAsync(droppedFiles);
        }, RoutingStrategies.Bubble, handledEventsToo: true);

        // The drop is finished routing before the load completes; a failure is shown, not thrown
        // into the dispatcher.
        async void OpenAsync(List<string> droppedFiles)
        {
            try
            {
                await dropLogic.TryOpenDroppedProjectAsync(droppedFiles);
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage($"Error loading dropped project:\n{ex.Message}");
            }
        }
    }

    private static List<string> DroppedFiles(DragEventArgs e) =>
        e.DataTransfer.TryGetFiles()?
            .Select(item => item.TryGetLocalPath())
            .OfType<string>()
            .ToList()
        ?? new List<string>();
}
