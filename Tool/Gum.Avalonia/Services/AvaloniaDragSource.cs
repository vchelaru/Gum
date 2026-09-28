using System;
using System.Threading.Tasks;
using Avalonia.Input;

namespace Gum.Avalonia.Services;

/// <summary>
/// Starts a platform drag for this head's drag sources (tree rows, search results, Standards
/// chips). Headless Avalonia has no drag source, so end-to-end tests replace <see cref="Start"/>
/// with one that waits while they deliver the drop, then complete it.
/// </summary>
public static class AvaloniaDragSource
{
    private static readonly Func<PointerEventArgs, IDataTransfer, DragDropEffects, Task<DragDropEffects>> Platform =
        (triggerEvent, data, allowedEffects) => DragDrop.DoDragDropAsync(triggerEvent, data, allowedEffects);

    /// <summary>Runs a drag of <c>data</c> started by the pointer event; completes when it is dropped or cancelled.</summary>
    internal static Func<PointerEventArgs, IDataTransfer, DragDropEffects, Task<DragDropEffects>> Start { get; set; } = Platform;

    /// <summary>Puts back the platform's drag, after a test replaced <see cref="Start"/>.</summary>
    internal static void RestorePlatform() => Start = Platform;
}
