using Gum.DataTypes;

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
    /// <see cref="Anchor"/>.
    /// </summary>
    bool HasSelectionChangedSinceAnchor { get; }

    /// <summary>
    /// Remembers <paramref name="destination"/> as where the next add should go and treats the
    /// selection as unchanged. Call after an add has selected what it created, so that selection
    /// change does not count as the user picking a new target.
    /// </summary>
    void Anchor(object? destination);

    /// <summary>Forgets the destination and treats the selection as unchanged (copy/cut).</summary>
    void Reset();

    /// <summary>Treats the selection as changed by the user, as a selection message would.</summary>
    void MarkSelectionChanged();
}
