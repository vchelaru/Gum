using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Gum.Managers;

/// <summary>
/// Which elements, behaviors and project files hold edits that are not on disk yet. Edits reach
/// disk at once while Auto Save is on, so something is only ever unsaved while Auto Save is off.
/// Every save marks what it wrote as saved; every reload from disk checks it first.
/// </summary>
public interface IUnsavedChangesTracker
{
    /// <summary>Records that <paramref name="element"/> was edited and the edit was not saved.</summary>
    void MarkUnsaved(ElementSave element);

    /// <summary>Records that <paramref name="element"/> now matches its file on disk.</summary>
    void MarkSaved(ElementSave element);

    /// <summary>Whether <paramref name="element"/> holds edits its file does not.</summary>
    bool HasUnsavedChanges(ElementSave element);

    /// <summary>Records that <paramref name="behavior"/> was edited and the edit was not saved.</summary>
    void MarkUnsaved(BehaviorSave behavior);

    /// <summary>Records that <paramref name="behavior"/> now matches its file on disk.</summary>
    void MarkSaved(BehaviorSave behavior);

    /// <summary>Whether <paramref name="behavior"/> holds edits its file does not.</summary>
    bool HasUnsavedChanges(BehaviorSave behavior);

    /// <summary>Records that the project file's own data (not its elements) was edited and not saved.</summary>
    void MarkUnsaved(GumProjectSave project);

    /// <summary>Records that the project file now matches <paramref name="project"/>.</summary>
    void MarkSaved(GumProjectSave project);

    /// <summary>
    /// Whether <paramref name="project"/>, or any element or behavior in it, holds edits that
    /// are not on disk, so reloading the whole project would lose them.
    /// </summary>
    bool HasAnyUnsavedChanges(GumProjectSave project);
}

/// <inheritdoc/>
public class UnsavedChangesTracker : IUnsavedChangesTracker
{
    // Keyed by reference: a reload or project load replaces the object, and the replacement
    // starts out matching its file.
    private static readonly object Marker = new object();
    private readonly ConditionalWeakTable<object, object> _unsaved;

    public UnsavedChangesTracker()
    {
        _unsaved = new ConditionalWeakTable<object, object>();
    }

    /// <inheritdoc/>
    public void MarkUnsaved(ElementSave element) => _unsaved.AddOrUpdate(element, Marker);

    /// <inheritdoc/>
    public void MarkSaved(ElementSave element) => _unsaved.Remove(element);

    /// <inheritdoc/>
    public bool HasUnsavedChanges(ElementSave element) => _unsaved.TryGetValue(element, out _);

    /// <inheritdoc/>
    public void MarkUnsaved(BehaviorSave behavior) => _unsaved.AddOrUpdate(behavior, Marker);

    /// <inheritdoc/>
    public void MarkSaved(BehaviorSave behavior) => _unsaved.Remove(behavior);

    /// <inheritdoc/>
    public bool HasUnsavedChanges(BehaviorSave behavior) => _unsaved.TryGetValue(behavior, out _);

    /// <inheritdoc/>
    public void MarkUnsaved(GumProjectSave project) => _unsaved.AddOrUpdate(project, Marker);

    /// <inheritdoc/>
    public void MarkSaved(GumProjectSave project) => _unsaved.Remove(project);

    /// <inheritdoc/>
    public bool HasAnyUnsavedChanges(GumProjectSave project) =>
        _unsaved.TryGetValue(project, out _) ||
        project.AllElements.Any(HasUnsavedChanges) ||
        project.Behaviors.Any(behavior => behavior != null && HasUnsavedChanges(behavior));
}
