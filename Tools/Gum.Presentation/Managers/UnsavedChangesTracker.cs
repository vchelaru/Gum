using Gum.DataTypes;
using System.Runtime.CompilerServices;

namespace Gum.Managers;

/// <summary>
/// Which elements hold edits that are not on disk yet. Edits reach disk at once while Auto Save is
/// on, so an element is only ever unsaved while Auto Save is off.
/// </summary>
public interface IUnsavedChangesTracker
{
    /// <summary>Records that <paramref name="element"/> was edited and the edit was not saved.</summary>
    void MarkUnsaved(ElementSave element);

    /// <summary>Records that <paramref name="element"/> now matches its file on disk.</summary>
    void MarkSaved(ElementSave element);

    /// <summary>Whether <paramref name="element"/> holds edits its file does not.</summary>
    bool HasUnsavedChanges(ElementSave element);
}

/// <inheritdoc/>
public class UnsavedChangesTracker : IUnsavedChangesTracker
{
    // Keyed by reference: a reload or project load replaces the element object, and the
    // replacement starts out matching its file.
    private static readonly object Marker = new object();
    private readonly ConditionalWeakTable<ElementSave, object> _unsaved;

    public UnsavedChangesTracker()
    {
        _unsaved = new ConditionalWeakTable<ElementSave, object>();
    }

    /// <inheritdoc/>
    public void MarkUnsaved(ElementSave element) => _unsaved.AddOrUpdate(element, Marker);

    /// <inheritdoc/>
    public void MarkSaved(ElementSave element) => _unsaved.Remove(element);

    /// <inheritdoc/>
    public bool HasUnsavedChanges(ElementSave element) => _unsaved.TryGetValue(element, out _);
}
