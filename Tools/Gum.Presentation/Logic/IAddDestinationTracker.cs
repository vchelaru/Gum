using Gum.DataTypes;
using System;

namespace Gum.Logic;

/// <summary>
/// Remembers where the user is adding instances, so repeated adds (paste, Ctrl+Shift-click) keep
/// landing in the same container even though each add selects what it created. A selection change
/// made by anything other than an add forgets the destination and counts as the user picking a new
/// target.
/// </summary>
public interface IAddDestinationTracker
{
    /// <summary>
    /// The <see cref="ElementSave"/> or <see cref="InstanceSave"/> the last add put its instances
    /// under, or null when the user has selected something since (or nothing has been added).
    /// </summary>
    object? Destination { get; }

    /// <summary>
    /// Whether the selection has changed by the user's hand since the last <see cref="Reset"/> or
    /// <see cref="RunAdd"/>.
    /// </summary>
    bool HasSelectionChangedSinceAnchor { get; }

    /// <summary>
    /// Runs <paramref name="add"/> with selection changes it makes ignored, then remembers
    /// <paramref name="destination"/> as where the next add should go.
    /// </summary>
    void RunAdd(object? destination, Action add);

    /// <summary>Forgets the destination and treats the selection as unchanged (copy/cut).</summary>
    void Reset();

    /// <summary>Treats the selection as changed by the user, as a selection message would.</summary>
    void MarkSelectionChanged();
}
