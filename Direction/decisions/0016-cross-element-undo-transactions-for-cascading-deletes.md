# 0016. Cross-element undo transactions for cascading deletes

- **Status:** Accepted
- **Date:** 2026-09-09
- **Deciders:** Victor Chelaru, Claude

## Context

[#4658](https://github.com/vchelaru/Gum/issues/4658) reports that Gum refuses to delete a component
variable once it has been assigned a value on instances elsewhere in the project
(`DeleteVariableService.GetIfCanDeleteVariable`,
`Gum/Plugins/InternalPlugins/VariableGrid/DeleteVariableService.cs:80`) — even though Gum already
computes exactly which instances would be affected, since that same enumeration builds the "can't
delete" message.

The real blocker is undo, not the mutation. `UndoManager` scopes history strictly per element
(`Dictionary<ElementSave, ElementHistory>`, see the `gum-tool-undo` skill), so there is no existing
way to record one undo entry that reverts changes spanning multiple `ElementSave`s.

This shape isn't unique to variable deletion. State deletion (an instance elsewhere set to a state
that no longer exists) and instance deletion raise the same problem: a delete on one element needs
to mutate data on other elements, and undoing it needs to reverse all of that atomically. Whole-element
deletion already has a related, narrower precedent: it is accepted as non-undoable, discarding its
undo history along with it. Whether to extend that precedent to cascading deletes, or build real
cross-element undo, is the question this decision resolves.

## Decision

We will build a cross-element undo transaction primitive and use it — starting with variable
deletion — instead of leaving cascading deletes non-undoable.

- **Ownership.** The undo entry belongs to the element that initiated the delete (the component whose
  variable was removed), not to the elements whose instances were mutated. It appears only in that
  element's History tab.
- **Reuse the existing impact enumeration.** Replay is driven by the same lookup the delete-block
  message already uses: `ReferenceFinder.GetReferencesToVariable`
  (`Tools/Gum.Presentation/Logic/ReferenceFinder.cs:421`, called via
  `RenameLogic.GetChangesForRenamedVariable`). It already walks every element in the project plus the
  inheritance chain (`ObjectFinder.Self.GetElementsInheritingFrom`) to find every instance-level
  assignment of a variable, so the delete flow's block-vs-remove behavior converges on one
  enumeration instead of growing a second one.
- **Snapshot timing.** The affected-instance list is captured at delete time. Replay tolerates
  instances or elements that no longer exist by the time undo/redo fires — skip them silently rather
  than erroring. Instances added after the delete were never affected and need no handling.
- **Persistence.** Replay mutates each affected element directly and saves it through the same path a
  normal edit uses: `IFileCommands.TryAutoSaveElement`, gated by `IProjectManager.AutoSave` (see the
  `gum-tool-file-watch` skill's "Saving Edits Back to Disk" section) — never an unconditional
  force-save.
- **Plugin/event parity.** Replay fires the same variable-changed plugin events a normal edit fires,
  so codegen and other listeners stay consistent regardless of whether the change came from direct
  editing or from an undo/redo replay.
- **Conflict handling.** If the user hand-edits an affected instance's value between undo and redo,
  redo overwrites it — last-write-wins, no merge/conflict resolution.
- **Why direct mutation of other elements is safe.** Gum has no concept of multiple simultaneously
  open elements — there are no tabs, only one element is selected at a time — so only one element can
  have an active `RecordState` undo baseline at any moment. A cross-element replay can never collide
  with another element's captured baseline, which is what makes mutating other elements' data directly
  safe without having to re-baseline them.

## Consequences

Variable deletion (#4658) can ship as a real, undoable operation instead of staying blocked or
shipping as a silent, non-undoable delete. The same primitive is available for state deletion and
instance deletion, which share the identical shape.

What's deferred: this is not a general-purpose cross-element merge/rebase system — only a
deterministic "apply this frozen list of instance-level changes, atomically, tolerating
already-deleted targets" transaction. Anything requiring live re-diffing or conflict resolution across
elements is out of scope and would need its own decision if it comes up.

## Alternatives considered

- **Ship the cascading delete as non-undoable**, following the whole-element-deletion precedent.
  Rejected: the use case is broader than one issue — state deletion, instance deletion, and
  inheritance fan-out all need the same mechanism — so building the primitive once pays for itself
  across all of them rather than accepting permanent undo data loss on each.
- **Build a fully general cross-element undo/transaction framework** with open-ended merge/rebase
  support for speculative future cases. Rejected as premature scope: the known cases share one
  concrete shape (frozen affected-set, atomic apply/revert, no live conflict resolution), and a general
  framework would solve problems nobody has hit yet.
