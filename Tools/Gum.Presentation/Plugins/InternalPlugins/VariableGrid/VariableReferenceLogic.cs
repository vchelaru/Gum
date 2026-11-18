using Gum.Diagnostics;
using Gum.Commands;
using Gum.Expressions;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.ProjectServices.Editing;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Undo;
using ToolsUtilities;
using System.Diagnostics.CodeAnalysis;

namespace Gum.Plugins.InternalPlugins.VariableGrid;
public class VariableReferenceLogic : IVariableReferenceLogic
{
    #region Fields/Properties

    private readonly IGuiCommands _guiCommands;
    private readonly IWireframeCommands _wireframeCommands;
    private readonly IDialogService _dialogService;
    private readonly IFileCommands _fileCommands;
    private readonly IVariableReferenceValidator _validator;
    private readonly IVariableReferenceCascade _cascade;
    private readonly IDispatcher _dispatcher;
    private readonly IWireframeObjectManager _wireframeObjectManager;

    #endregion

    public VariableReferenceLogic(IGuiCommands guiCommands,
        IWireframeCommands wireframeCommands,
        IDialogService dialogService,
        IFileCommands fileCommands,
        IVariableReferenceValidator validator,
        IVariableReferenceCascade cascade,
        IDispatcher dispatcher,
        IWireframeObjectManager wireframeObjectManager)
    {
        _guiCommands = guiCommands;
        _wireframeCommands = wireframeCommands;
        _dialogService =  dialogService;
        _fileCommands = fileCommands;
        _validator = validator;
        _cascade = cascade;
        _dispatcher = dispatcher;
        _wireframeObjectManager = wireframeObjectManager;
    }

    public AssignmentExpressionSyntax? GetAssignmentSyntax(string item) => VariableReferenceValidator.ParseAssignment(item);

    #region Validation (failures)

    private static void CommentFailures(VariableListSave newDirectValue, List<(string, GeneralResponse)> failures)
    {
        foreach (var failure in failures)
        {

            var count = newDirectValue.ValueAsIList.Count;

            // find the failure, comment it out:
            for (int i = 0; i < count; i++)
            {
                if (newDirectValue.ValueAsIList[i] as string == failure.Item1)
                {
                    newDirectValue.ValueAsIList[i] = "//" + newDirectValue.ValueAsIList[i];
                }
            }
        }
    }

    private void ShowFailureMessage(List<(string, GeneralResponse)> failures)
    {
        if (failures.Count > 0)
        {
            string message = "";

            foreach (var failure in failures)
            {
                var generalResponseMessage = failure.Item2.Message;
                if (string.IsNullOrEmpty(generalResponseMessage))
                {
                    message += $"[{failure.Item1}]\n";
                }
                else
                {
                    message += $"[{failure.Item1}] {failure.Item2.Message} \n";
                }
            }

            message += "\n\nInvalid lines will be commented out";

            _dialogService.ShowMessage(message, "Invalid Variable Reference");
        }
    }

    #endregion

    #region Assignment Reactions

    /// <summary>One edit whose state-wide reaction is waiting for the end of a batch.</summary>
    private sealed record PendingEdit(InstanceSave? Instance, string UnqualifiedMember, string QualifiedName);

    /// <summary>The deferred state-wide work for one state of one element.</summary>
    private sealed class PendingReaction
    {
        public PendingReaction(ElementSave element, StateSave state, Dictionary<string, object?> valuesBefore)
        {
            Element = element;
            State = state;
            ValuesBefore = valuesBefore;
        }

        public ElementSave Element { get; }
        public StateSave State { get; }
        public Dictionary<string, object?> ValuesBefore { get; }
        public bool TrySave { get; set; }
        public List<PendingEdit> Edits { get; } = new List<PendingEdit>();
    }

    private List<PendingReaction>? _pendingReactions;
    private int _batchDepth;

    /// <summary>
    /// Elements scanned for readers plus components whose behavior lines were evaluated, summed over every
    /// reaction so far. Tests compare it across project sizes instead of timing a drag tick.
    /// </summary>
    internal int ScanCount { get; private set; }

    /// <inheritdoc/>
    public IDisposable BeginReferenceBatch()
    {
        _batchDepth++;
        _pendingReactions ??= new List<PendingReaction>();
        return new BatchScope(this);
    }

    private sealed class BatchScope : IDisposable
    {
        private VariableReferenceLogic? _owner;

        public BatchScope(VariableReferenceLogic owner) => _owner = owner;

        public void Dispose()
        {
            VariableReferenceLogic? owner = _owner;
            _owner = null;
            owner?.EndBatch();
        }
    }

    private void EndBatch()
    {
        _batchDepth--;
        if (_batchDepth > 0)
        {
            return;
        }

        List<PendingReaction> pending = _pendingReactions ?? new List<PendingReaction>();
        _pendingReactions = null;
        foreach (PendingReaction reaction in pending)
        {
            using (ToolLog.Begin("ReferenceBatchReaction", count: reaction.Edits.Count))
            {
                RunStateWideReaction(reaction.Element, reaction.State, reaction.ValuesBefore, reaction.Edits,
                    reaction.TrySave, isFullCommit: true);
            }
        }
    }

    public void DoVariableReferenceReaction(ElementSave parentElement, InstanceSave? leftSideInstance, string unqualifiedMember,
        StateSave stateSave, string qualifiedName, bool trySave, bool isFullCommit = true)
    {
        // The currently-rendered wireframe representation of parentElement, if any. Lets a right
        // side like "Root.AbsoluteWidth" resolve against the live, already-laid-out object - null
        // when nothing is rendered for this element, in which case such identifiers stay
        // unresolved, same as any other unresolvable identifier.
        var liveRoot = _wireframeObjectManager.GetRepresentation(parentElement);

        if (unqualifiedMember == "VariableReferences")
        {
            var newDirectValue = stateSave.GetVariableListSave(qualifiedName);

            if (newDirectValue != null)
            {
                List<(string, GeneralResponse)> failures;
                using (ToolLog.Begin("GetFailures", count: newDirectValue.ValueAsIList?.Count ?? 0))
                {
                    failures = _validator.GetFailures(parentElement, leftSideInstance, newDirectValue.ValueAsIList?.Cast<string>() ?? Enumerable.Empty<string>(), liveRoot);
                }

                if (failures.Count > 0)
                {
                    // Comment the invalid lines synchronously so the data model is consistent
                    // before any refresh runs.
                    CommentFailures(newDirectValue, failures);

                    // Defer the failure dialog instead of showing it inline. A VariableReferences
                    // edit is frequently committed by focus loss when the user clicks a different
                    // instance; a synchronous modal here pumps the WPF message loop in the middle
                    // of the commit, interleaving the pending selection change and leaving the
                    // variable grid painted for the previously-selected instance (issue #566).
                    // Posting at background priority lets the commit and the selection change
                    // settle first, then shows the dialog and re-refreshes the grid against the
                    // now-current selection.
                    _dispatcher.Post(() =>
                    {
                        ShowFailureMessage(failures);
                        _guiCommands.RefreshVariables(force: true);
                    });
                }
            }
        }

        // apply references on this element first, then apply the values to the other references:
        Dictionary<string, object?> valuesBefore = new Dictionary<string, object?>();
        foreach (VariableSave variable in stateSave.Variables)
        {
            valuesBefore[variable.Name] = variable.Value;
        }

        PendingEdit edit = new PendingEdit(leftSideInstance, unqualifiedMember, qualifiedName);

        // In a multi-select commit every selected instance would re-apply all references of the state (and
        // cascade into the other elements) for a result that only the last edit settles, so it runs once when
        // the batch ends (#7509). The edit itself is already in the state; only the reaction waits.
        if (_batchDepth > 0 && isFullCommit && _pendingReactions != null)
        {
            PendingReaction? pending = _pendingReactions.FirstOrDefault(item => item.Element == parentElement && item.State == stateSave);
            if (pending == null)
            {
                pending = new PendingReaction(parentElement, stateSave, valuesBefore);
                _pendingReactions.Add(pending);
            }
            pending.TrySave |= trySave;
            pending.Edits.Add(edit);
            return;
        }

        RunStateWideReaction(parentElement, stateSave, valuesBefore, new[] { edit }, trySave, isFullCommit);
    }

    /// <summary>
    /// The part of a reaction that covers the whole state and what reads it: re-applies the references of
    /// <paramref name="stateSave"/>, the other states, and the elements that read this one. Runs once per edit,
    /// or once per batch for every edit in <paramref name="edits"/>.
    /// </summary>
    private void RunStateWideReaction(ElementSave parentElement, StateSave stateSave, Dictionary<string, object?> valuesBefore,
        IReadOnlyList<PendingEdit> edits, bool trySave, bool isFullCommit)
    {
        PendingEdit first = edits[0];
        InstanceSave? leftSideInstance = first.Instance;
        string unqualifiedMember = first.UnqualifiedMember;
        bool containsReferencesEdit = edits.Any(item => item.UnqualifiedMember == "VariableReferences");
        // A tick of a drag applies only the references inside the current element (its own rows, the
        // behavior's tool-only lines, its other states), so a followed value is live and the cost does not grow
        // with the project. The part that reaches other elements, files or the whole canvas runs once, on the
        // full commit that ends the drag, so the result is the one a typed edit gives. Every drag, scrub and
        // slider ends with a full commit (#6888, #7438).
        bool isWide = isFullCommit || containsReferencesEdit;
        var liveRoot = _wireframeObjectManager.GetRepresentation(parentElement);

        // apply references on this element first, then apply the values to the other references:
        using (ToolLog.Begin("ApplyVariableReferences(state)", count: stateSave.Variables.Count))
        {
            ElementSaveExtensions.ApplyVariableReferences(parentElement, stateSave, liveRoot, isFullCommit);
        }

        // Then evaluate any behavior-level ToolOnlyVariableReferences so design-time
        // wireframe preview reflects FormsProperty values (e.g. IsEnabled = false →
        // ButtonCategoryState = "Disabled"). Tool-only by design — never invoked at
        // runtime, since the Forms control's own setter owns the visual at runtime.
        if (isWide)
        {
            ScanCount += BehaviorToolOnlyReferencesApplier.Apply(parentElement, stateSave);
        }
        else
        {
            // A tick touches only the lines that read the dragged member, on the instance dragged.
            ScanCount += BehaviorToolOnlyReferencesApplier.ApplyDrivenBy(parentElement, leftSideInstance, unqualifiedMember, stateSave);
        }

        // A categorized state's rows read the same instances, so they follow too (#6886).
        HashSet<string> changedNames = new HashSet<string>(edits.Select(item => item.QualifiedName));
        foreach (VariableSave variable in stateSave.Variables)
        {
            if (!valuesBefore.TryGetValue(variable.Name, out object? oldValue) || !ElementSaveExtensions.ValueEquality(oldValue, variable.Value))
            {
                changedNames.Add(variable.Name);
            }
        }
        using (ToolLog.Begin("ApplyOtherStatesReading", count: changedNames.Count))
        {
            _cascade.ApplyOtherStatesReading(parentElement, stateSave, changedNames, liveRoot, isFullCommit);
        }

        if (containsReferencesEdit)
        {
            using (ToolLog.Begin("RefreshVariableValues"))
            {
                _guiCommands.RefreshVariableValues();
            }
        }

        if (!isWide)
        {
            return;
        }

        using (ToolLog.Begin("ApplyReferencesToElement", count: parentElement.Instances.Count))
        {
            ApplyReferencesToElement(parentElement, trySave, isFullCommit);
        }

        bool didSetDeepReference = false;
        foreach (PendingEdit edit in edits)
        {
            var newValue = stateSave.GetValueRecursive(edit.QualifiedName);

            // this could be a tunneled variable. If so, we may need to propagate the value to other instances one level deeper
            didSetDeepReference |= _cascade.ApplyToInstanceInternals(parentElement, edit.Instance, stateSave, edit.UnqualifiedMember, newValue);
        }

        // now force save it if it's a variable reference:
        if (containsReferencesEdit && trySave)
        {
            _fileCommands.TryAutoSaveElement(parentElement);
        }

        // The MainEditorTabPlugin handles refreshing in most cases, but if there was a "deep" set due to variable references,
        // then we forcefully change the values here.
        if (didSetDeepReference)
        {
            using (ToolLog.Begin("WireframeRefresh(forceLayout)"))
            {
                _wireframeCommands.Refresh(forceLayout: true);
            }
        }
    }

    /// <inheritdoc/>
    public void ReapplySiblingDependentReferences(ElementSave element)
    {
        List<StateSave> states = element.AllStates
            .Where(state => state.VariableLists.Any(IsSiblingDependentRow))
            .ToList();
        // A behavior line (a tab reading @Index) goes stale the same way as a typed row.
        bool behaviorLinesReadIndex = BehaviorToolOnlyReferencesApplier.AnyInstanceLineMatches(element, SiblingDependentName);
        if (states.Count == 0 && !behaviorLinesReadIndex)
        {
            return;
        }

        // Index reads the live layout, so the wireframe must already reflect the new structure.
        _wireframeObjectManager.RefreshAll(forceLayout: true);
        GraphicalUiElement? liveRoot = _wireframeObjectManager.GetRepresentation(element);
        foreach (StateSave state in states)
        {
            ElementSaveExtensions.ApplyVariableReferences(element, state, liveRoot);
        }

        if (behaviorLinesReadIndex && element.DefaultState != null)
        {
            BehaviorToolOnlyReferencesApplier.Apply(element, element.DefaultState);
        }

        // The values written above are what the wireframe should now show.
        _wireframeObjectManager.RefreshAll(forceLayout: true);
        _fileCommands.TryAutoSaveElement(element);
    }

    private static bool IsSiblingDependentRow(VariableListSave list) =>
        list.GetRootName() == "VariableReferences" &&
        list.ValueAsIList.OfType<string>().Any(row => SiblingDependentName.IsMatch(row));

    private static readonly System.Text.RegularExpressions.Regex SiblingDependentName =
        new System.Text.RegularExpressions.Regex(@"\bIndex\b");

    /// <inheritdoc/>
    public StoredValueChange[] ReapplyReferences(ElementSave element, ElementSave before, ElementSave? otherSide)
    {
        RestoredChanges restored = RestoredChanges.Find(element, before);
        if (restored.IsEmpty)
        {
            return Array.Empty<StoredValueChange>();
        }

        GraphicalUiElement? liveRoot = _wireframeObjectManager.GetRepresentation(element);
        foreach (StateSave state in element.AllStates)
        {
            foreach (string removed in RestoredChanges.RemovedWrittenNames(state, RestoredChanges.FindState(before, state, element)))
            {
                KeepHeldValue(state, RestoredChanges.FindState(before, state, element), RestoredChanges.FindState(otherSide, state, element), removed);
            }
            // Any variable the action itself left alone keeps what it holds: a cascade (or an earlier undo)
            // may have changed it since the snapshot was taken, and undoing this action must not undo that.
            StateSave? heldState = RestoredChanges.FindState(before, state, element);
            StateSave? otherState = RestoredChanges.FindState(otherSide, state, element);
            // A state the action added or removed has no counterpart to compare with and is restored whole.
            if (heldState != null && otherState != null)
            {
                foreach (string name in state.Variables.Select(item => item.Name)
                    .Concat(heldState.Variables.Select(item => item.Name)).Distinct().ToList())
                {
                    KeepHeldValue(state, heldState, otherState, name);
                }
            }

            VariableReferenceGraph graph = VariableReferenceGraph.GetForState(element, state);
            if (graph.HasRows)
            {
                List<ReferenceRow> rows = graph.OrderedRows.Concat(graph.CyclicRows)
                    .Where(row => restored.AddedRows.Contains(RestoredChanges.RowKey(row.SourceObject, row.Line)))
                    .ToList();
                HashSet<ReferenceRow> toApply = VariableReferenceCascade.FindRowsToApply(graph, restored.VariableNames, rows);
                if (toApply.Count > 0)
                {
                    // A row whose source is gone or null writes nothing, and the variable keeps its last value:
                    // start each fed variable from the value it held before the restore, as a typed edit does,
                    // not from the snapshot's.
                    StateSave? old = RestoredChanges.FindState(before, state, element);
                    foreach (ReferenceRow row in toApply)
                    {
                        KeepHeldValue(state, old, RestoredChanges.FindState(otherSide, state, element), row.WrittenName);
                    }
                    ElementSaveExtensions.ApplyVariableReferences(element, state, liveRoot, onlyRows: toApply);
                }
            }

            // As a typed edit does, derive again what the behaviors' tool-only references write from the
            // restored Forms values. The snapshot may hold what a reload from disk has since replaced (#6907).
            BehaviorToolOnlyReferencesApplier.Apply(element, state);
        }

        return ApplyReferencesToElement(element, trySave: true);
    }

    private static void KeepHeldValue(StateSave state, StateSave? held, StateSave? otherSide, string variableName)
    {
        // The action changed this variable itself (it was typed, reset on purpose, or written by the cascade of
        // a row it added): take the snapshot's value, as typing the old value would.
        if (otherSide != null && !HoldsSameValue(state, otherSide, variableName))
        {
            return;
        }

        VariableSave? was = held?.Variables.FirstOrDefault(item => item.Name == variableName);
        VariableSave? now = state.Variables.FirstOrDefault(item => item.Name == variableName);
        if (was == null)
        {
            if (now != null)
            {
                state.Variables.Remove(now);
            }
            return;
        }

        if (now == null)
        {
            now = new VariableSave { Name = was.Name, Type = was.Type };
            state.Variables.Add(now);
        }
        now.Value = was.Value;
        now.SetsValue = was.SetsValue;
    }

    private static bool HoldsSameValue(StateSave first, StateSave second, string variableName)
    {
        VariableSave? a = first.Variables.FirstOrDefault(item => item.Name == variableName);
        VariableSave? b = second.Variables.FirstOrDefault(item => item.Name == variableName);
        return a == null || b == null ? a == b : ElementSaveExtensions.ValueEquality(a.Value, b.Value);
    }

    /// <inheritdoc/>
    public StoredValueChange[] ReapplyToolOnlyReferences(BehaviorSave behavior, BehaviorSave? previous, bool trySave)
    {
        GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
        if (project == null)
        {
            return Array.Empty<StoredValueChange>();
        }

        ISet<string> previousNames = previous == null
            ? new HashSet<string>()
            : BehaviorToolOnlyReferencesApplier.GetDrivenVariableNames(previous);
        Dictionary<ComponentSave, ISet<string>> stale = new Dictionary<ComponentSave, ISet<string>>();
        foreach (ComponentSave component in project.Components)
        {
            if (FormsBehaviorChain.GetBehaviors(component).Any(item => item.Name == behavior.Name))
            {
                HashSet<string> removed = new HashSet<string>(previousNames);
                removed.ExceptWith(BehaviorToolOnlyReferencesApplier.GetDrivenVariableNames(component));
                stale[component] = removed;
            }
        }
        return ReapplyToolOnlyReferencesAround(project, stale, trySave);
    }

    /// <inheritdoc/>
    public StoredValueChange[] ReapplyToolOnlyReferences(ElementSave element, ElementSave? previous, bool trySave)
    {
        GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
        if (project == null)
        {
            return Array.Empty<StoredValueChange>();
        }

        // Only a component carries behaviors, but a screen's own instances are re-applied the same way.
        Dictionary<ComponentSave, ISet<string>> stale = new Dictionary<ComponentSave, ISet<string>>();
        if (element is ComponentSave component)
        {
            HashSet<string> removed = previous is ComponentSave previousComponent
                ? new HashSet<string>(BehaviorToolOnlyReferencesApplier.GetDrivenVariableNames(previousComponent))
                : new HashSet<string>();
            removed.ExceptWith(BehaviorToolOnlyReferencesApplier.GetDrivenVariableNames(component));
            stale[component] = removed;
        }
        return ReapplyToolOnlyReferencesAround(project, stale, trySave, extraTargets: new[] { element });
    }

    /// <summary>
    /// Re-applies the tool-only references to each component of <paramref name="staleNames"/> and to every
    /// element with an instance of one, which is where <see cref="BehaviorToolOnlyReferencesApplier.Apply"/>
    /// materializes a component's references. First, the variables a component's removed references wrote
    /// (its value in <paramref name="staleNames"/>) are removed from it and from those instances. Elements
    /// whose stored values changed are saved and their readers updated.
    /// </summary>
    private StoredValueChange[] ReapplyToolOnlyReferencesAround(GumProjectSave project, Dictionary<ComponentSave, ISet<string>> staleNames,
        bool trySave, IEnumerable<ElementSave>? extraTargets = null)
    {
        HashSet<ElementSave> affected = new HashSet<ElementSave>(staleNames.Keys);
        if (extraTargets != null)
        {
            affected.UnionWith(extraTargets);
        }
        if (staleNames.Count > 0)
        {
            foreach (ElementSave container in project.Components.Cast<ElementSave>().Concat(project.Screens))
            {
                if (container.Instances.Any(instance => ObjectFinder.Self.GetElementSave(instance.BaseType) is ComponentSave baseComponent && staleNames.ContainsKey(baseComponent)))
                {
                    affected.Add(container);
                }
            }
        }

        List<StoredValueChange> changes = new List<StoredValueChange>();
        List<ElementSave> changedElements = new List<ElementSave>();
        foreach (ElementSave element in affected)
        {
            StateSave? state = element.DefaultState;
            if (state == null)
            {
                continue;
            }

            Dictionary<string, object?> before = new Dictionary<string, object?>();
            foreach (VariableSave variable in state.Variables)
            {
                before[variable.Name] = variable.Value;
            }
            RemoveStaleValues(element, state, staleNames);
            BehaviorToolOnlyReferencesApplier.Apply(element, state);

            int countBefore = changes.Count;
            foreach (VariableSave variable in state.Variables)
            {
                if (!before.TryGetValue(variable.Name, out object? oldValue) || !Equals(oldValue, variable.Value))
                {
                    changes.Add(new StoredValueChange(element, null, state.Name, variable.Name, variable.Type, variable.Value));
                }
            }
            // A reference that now resolves to a resting value removes the value an instance held.
            foreach (string removed in before.Keys.Where(name => state.Variables.All(variable => variable.Name != name)))
            {
                changes.Add(new StoredValueChange(element, null, state.Name, removed, null, null));
            }
            if (changes.Count > countBefore)
            {
                changedElements.Add(element);
            }
        }

        foreach (ElementSave element in changedElements)
        {
            changes.AddRange(ApplyReferencesToElement(element, trySave));
            if (trySave)
            {
                _fileCommands.TryAutoSaveElement(element);
            }
        }

        return changes.ToArray();
    }

    /// <summary>
    /// Removes from <paramref name="state"/> the values a removed reference wrote: the component's own variable
    /// when <paramref name="element"/> is the component, and <c>Instance.Variable</c> for each of its instances.
    /// </summary>
    private static void RemoveStaleValues(ElementSave element, StateSave state, Dictionary<ComponentSave, ISet<string>> staleNames)
    {
        if (element is ComponentSave self && staleNames.TryGetValue(self, out ISet<string>? own))
        {
            state.Variables.RemoveAll(variable => own.Contains(variable.Name));
        }
        foreach (InstanceSave instance in element.Instances)
        {
            if (ObjectFinder.Self.GetElementSave(instance.BaseType) is ComponentSave baseComponent &&
                staleNames.TryGetValue(baseComponent, out ISet<string>? names) && names.Count > 0)
            {
                state.Variables.RemoveAll(variable => names.Any(name => variable.Name == instance.Name + "." + name));
            }
        }
    }

    /// <inheritdoc/>
    public StoredValueChange[] ApplyReferencesToElement(ElementSave element, bool trySave, bool isFullCommit = true)
    {
        ReferenceCascadeResult result = _cascade.ApplyToElementsReading(element, _wireframeObjectManager.GetRepresentation, isFullCommit);
        ScanCount += result.ElementsScanned;

        if (trySave)
        {
            foreach (var elementToSave in result.ChangedElements)
            {
                _fileCommands.TryAutoSaveElement(elementToSave);
            }
        }

        return result.Changes;
    }

    public void ReactIfChangedMemberIsVariableReference(InstanceSave? instance, StateSave stateSave, string changedMember, object? oldValue)
    {
        ///////////////////// Early Out/////////////////////////////////////
        if (changedMember != "VariableReferences") return;

        var changedMemberWithPrefix = changedMember;
        if (instance != null)
        {
            changedMemberWithPrefix = instance.Name + "." + changedMember;
        }

        var newValueAsList = stateSave.GetVariableListSave(changedMemberWithPrefix)?.ValueAsIList as List<string>;

        ///////////////////End Early Out/////////////////////////////////////

        bool didChange = ModifyLines(oldValue, newValueAsList);

        if (didChange)
        {
            using (ToolLog.Begin("RefreshVariables(force) after line expansion"))
            {
                _guiCommands.RefreshVariables(force: true);
            }
        }
    }

    #endregion

    #region Line Assignment Expansion / Modifications

    bool ModifyLines(object? oldValue, List<string>? newValueAsList)
    {
        var oldValueAsList = oldValue as List<string>;


        if (newValueAsList != null)
        {
            _validator.NormalizeLines(newValueAsList);
        }

        var didChange = false;
        if (oldValueAsList == null || newValueAsList == null)
        {
            // Changed unless both are null.
            didChange = oldValueAsList != newValueAsList;
        }
        else if (oldValueAsList.Count != newValueAsList.Count)
        {
            didChange = true;
        }
        else
        {
            // not null, same items, so let's loop
            for (int i = 0; i < oldValueAsList.Count; i++)
            {
                if (oldValueAsList[i] != newValueAsList[i])
                {
                    didChange = true;
                    break;
                }
            }
        }

        return didChange;
    }

    #endregion
}
