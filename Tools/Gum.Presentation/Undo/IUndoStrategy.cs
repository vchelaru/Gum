namespace Gum.Undo;

/// <summary>
/// A per-domain undo/redo strategy. <see cref="UndoManager"/> is a pure orchestrator that owns the
/// undo-lock lifecycle and the UndosChanged event, and delegates the domain-specific
/// snapshot/diff/apply work to a strategy chosen by the current selection (element vs behavior).
/// Adding a new domain (e.g. animations) means adding a new strategy rather than another inline track.
/// Introduced in #3403 (PR 1 of 2 toward #3399).
/// </summary>
internal interface IUndoStrategy
{
    /// <summary>
    /// True when this strategy owns undo/redo for the current selection. The orchestrator picks the
    /// first applicable strategy; the element strategy is the fallback (always applicable).
    /// </summary>
    bool AppliesToCurrentSelection { get; }

    /// <summary>
    /// Captures a baseline snapshot of the current domain object. Later compared against the live
    /// object in <see cref="TryRecord"/> to detect changes. A no-op while undo locks are held.
    /// </summary>
    void CaptureBaseline();

    /// <summary>
    /// Diffs the captured baseline against the current state and, if anything changed, appends an
    /// undo action (with its paired redo state) to this domain's history.
    /// </summary>
    void TryRecord();

    bool CanUndo();
    bool CanRedo();
    void PerformUndo();
    void PerformRedo();

    /// <summary>Discards all recorded history and the captured baseline for this domain.</summary>
    void Clear();
}
