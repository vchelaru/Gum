// Undo and redo compared with typing, one rule: an undo or redo has the same effect on the rest of the app as
// typing the old (or new) value. Each step must leave the element and the element reading it with the variables
// and reference rows that typing the inverse (or the same) edit gives; undoing a row add instead matches the world
// just before the add. Two worlds run one after the other, since ObjectFinder is a static and cannot hold two
// projects at once.
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using MonoGameGum.Tests;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using Shouldly;
using ToolsUtilities;
using System.Globalization;
using System.Text;

namespace Gum.Presentation.Tests;

public enum OracleEditKind { Set, Reset, ClearRow, AddRow }

public sealed record OracleEdit(OracleEditKind Kind, string Inst, string State, float? Value, string? Line)
{
    public override string ToString() => $"{Kind}({State}:{Inst}{(Value != null ? "=" + Value : "")}{(Line != null ? " '" + Line + "'" : "")})";
}

public sealed record OracleOp(OracleEdit Fwd, OracleEdit Inv);

public sealed class UndoOracleWorld : BaseTestClass
{
    private readonly ComponentSave _comp = NewComponent("Comp");
    private readonly ComponentSave _other = NewComponent("Other");
    private readonly Dictionary<string, ComponentSave> _disk = new();
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly VariableReferenceLogic _logic;
    private readonly UndoManager _undo;
    private readonly RenameLogic _rename;
    private ElementSave _selectedElement;
    private StateSave _selectedStateSave;
    private bool _autoSave = true;
    public List<string> SourceNames = new();
    public List<string> CompInstances = new();
    public Dictionary<string, string> CandidateRows = new();
    public bool HasAlt;
    public bool FollowerOps;
    public string Description = "";

    public UndoOracleWorld()
    {
        GumExpressionService.Initialize();
        _selectedElement = _comp;
        _selectedStateSave = _comp.DefaultState;
        _selectedState.Setup(x => x.SelectedElement).Returns(() => _selectedElement);
        _selectedState.Setup(x => x.SelectedComponent).Returns(() => _selectedElement as ComponentSave);
        _selectedState.Setup(x => x.SelectedStateSave).Returns(() => _selectedStateSave);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(() => _selectedElement.Categories.FirstOrDefault(item => item.States.Contains(_selectedStateSave)));
        _fileCommands.Setup(x => x.TryAutoSaveElement(It.IsAny<ElementSave?>())).Callback<ElementSave?>(SaveToFakeDisk);
        _fileCommands.Setup(x => x.TryAutoSaveCurrentElement()).Callback(() => SaveToFakeDisk(_selectedElement));

        Mock<ICompositeMemberRegistry> registry = new();
        registry.Setup(x => x.Descriptors).Returns(new List<CompositeMemberDescriptor>());
        Mock<IWireframeObjectManager> wireframe = new();
        wireframe.Setup(x => x.AllIpsos).Returns(new List<GraphicalUiElement>());
        _logic = new VariableReferenceLogic(new Mock<IGuiCommands>().Object, new Mock<IWireframeCommands>().Object,
            new Mock<IDialogService>().Object, _fileCommands.Object, registry.Object,
            new Mock<IDispatcher>().Object, wireframe.Object);

        Mock<IReferenceFinderProjectProvider> projectProvider = new();
        projectProvider.Setup(x => x.GumProjectSave).Returns(() => ObjectFinder.Self.GumProjectSave);
        Mock<INameVerifier> nameVerifier = new();
        string? whyNot;
        nameVerifier.Setup(x => x.IsInstanceNameValid(It.IsAny<string>(), It.IsAny<InstanceSave?>(), It.IsAny<IInstanceContainer>(), out whyNot)).Returns(true);
        nameVerifier.Setup(x => x.IsStateNameValid(It.IsAny<string>(), It.IsAny<StateSaveCategory?>(), It.IsAny<StateSave>(), out whyNot)).Returns(true);
        Mock<IRenameProjectProvider> renameProjectProvider = new();
        renameProjectProvider.Setup(x => x.GumProjectSave).Returns(() => ObjectFinder.Self.GumProjectSave);
        _rename = new RenameLogic(_selectedState.Object, nameVerifier.Object, new Mock<IDialogService>().Object,
            new Mock<IGuiCommands>().Object, _fileCommands.Object, new Mock<IDeleteLogic>().Object,
            renameProjectProvider.Object, new Mock<IRenamePluginNotifier>().Object,
            new Mock<IStandardElementsManagerGumTool>().Object, new ReferenceFinder(projectProvider.Object),
            new Lazy<IUndoManager>(() => _undo));
        _undo = new UndoManager(_selectedState.Object, _rename, new Mock<IGuiCommands>().Object,
            _fileCommands.Object, new Mock<IMessenger>().Object, new Mock<IUndoPluginNotifier>().Object,
            new Mock<IAnimationUndoProvider>().Object, projectProvider.Object, new Lazy<IVariableReferenceLogic>(() => _logic));
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        base.Dispose();
    }

    private static ComponentSave NewComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    private static InstanceSave NewInstance(ElementSave parent, string name)
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = "Rectangle", ParentContainer = parent };
        parent.Instances.Add(instance);
        return instance;
    }

    private static void SetWidth(StateSave state, string instanceName, float? value)
    {
        string name = instanceName + ".Width";
        VariableSave? variable = state.Variables.FirstOrDefault(item => item.Name == name);
        if (value == null)
        {
            if (variable != null) state.Variables.Remove(variable);
            return;
        }
        if (variable == null)
        {
            variable = new VariableSave { Name = name, Type = "float" };
            state.Variables.Add(variable);
        }
        variable.Value = value;
        variable.SetsValue = true;
    }

    private static void AddRow(StateSave state, string owner, params string[] rows)
    {
        state.VariableLists.Add(new VariableListSave<string> { Name = owner + ".VariableReferences", Type = "string", Value = rows.ToList() });
    }

    private void SaveToFakeDisk(ElementSave? element)
    {
        if (_autoSave && element is ComponentSave component)
        {
            _disk[component.Name] = FileManager.CloneSaveObject(component);
        }
    }

    public void Build(Random g, bool materialized, bool autoSave, bool useAlt, bool followerOps)
    {
        FollowerOps = followerOps;
        _autoSave = autoSave;
        HasAlt = useAlt;
        Description = $"materialized={materialized} autoSave={autoSave} alt={useAlt}";
        List<string> names = new() { "S0", "S1" };
        NewInstance(_comp, "S0");
        NewInstance(_comp, "S1");
        SetWidth(_comp.DefaultState, "S0", 50);
        SetWidth(_comp.DefaultState, "S1", 60);
        int nF = g.Next(2, 6);
        for (int i = 0; i < nF; i++)
        {
            string name = "F" + i;
            NewInstance(_comp, name);
            string src = g.Next(10) < 6 ? names[names.Count - 1] : names[g.Next(names.Count)];
            int k = g.Next(0, 3);
            string line = $"Width = {src}.Width" + (k > 0 ? $" + {k}" : "");
            if (g.Next(5) != 0)
            {
                AddRow(_comp.DefaultState, name, line);
                Description += $" | {name}: {line}";
            }
            else
            {
                AddRow(_comp.DefaultState, name);
                CandidateRows[name] = line;
                Description += $" | {name}: (norow, cand {line})";
            }
            names.Add(name);
        }
        CompInstances = new List<string>(names);
        int nO = g.Next(1, 4);
        List<string> otherNames = new();
        for (int j = 0; j < nO; j++)
        {
            string name = "O" + j;
            NewInstance(_other, name);
            string line;
            if (j > 0 && g.Next(3) == 0)
            {
                line = $"Width = {otherNames[g.Next(otherNames.Count)]}.Width + 1";
            }
            else
            {
                line = $"Width = Components/Comp.{names[g.Next(names.Count)]}.Width";
            }
            AddRow(_other.DefaultState, name, line);
            Description += $" | {name}: {line}";
            otherNames.Add(name);
        }
        StateSave? alt = null;
        if (useAlt)
        {
            NewInstance(_comp, "FA");
            StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
            alt = new StateSave { Name = "Alt", ParentContainer = _comp };
            category.States.Add(alt);
            _comp.Categories.Add(category);
            SetWidth(alt, "S0", 70);
            AddRow(alt, "FA", "Width = S0.Width + 1");
            // Alt does not set S1, so FB reads the default state's value: the case where a default-state edit has to reach a categorized state (#6886).
            NewInstance(_comp, "FB");
            AddRow(alt, "FB", "Width = S1.Width + 2");
            Description += " | Alt: S0=70, FA: Width = S0.Width + 1, FB: Width = S1.Width + 2";
        }

        GumProjectSave project = new GumProjectSave();
        StandardElementSave rectangle = new StandardElementSave { Name = "Rectangle" };
        StateSave rectangleDefault = new StateSave { Name = "Default", ParentContainer = rectangle };
        rectangleDefault.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 0f, SetsValue = true });
        rectangle.States.Add(rectangleDefault);
        project.StandardElements.Add(rectangle);
        project.Components.Add(_comp);
        project.Components.Add(_other);
        project.ComponentReferences.Add(new ElementReference { Name = "Comp", ElementType = ElementType.Component });
        project.ComponentReferences.Add(new ElementReference { Name = "Other", ElementType = ElementType.Component });
        ObjectFinder.Self.GumProjectSave = project;
        project.Initialize();

        if (materialized)
        {
            ElementSaveExtensions.ApplyVariableReferences(_comp, _comp.DefaultState, null);
            if (alt != null)
            {
                ElementSaveExtensions.ApplyVariableReferences(_comp, alt, null);
            }
            ElementSaveExtensions.ApplyVariableReferences(_other, _other.DefaultState, null);
        }
        _disk["Comp"] = FileManager.CloneSaveObject(_comp);
        _disk["Other"] = FileManager.CloneSaveObject(_other);
        _undo.RecordState();
    }

    private StateSave StateFor(string name) => name == "Default" ? _comp.DefaultState : _comp.Categories.Single().States.Single();

    public void Typed(OracleEdit a)
    {
        StateSave state = StateFor(a.State);
        _selectedStateSave = state;
        InstanceSave instance = _comp.GetInstance(a.Inst)!;
        using (_undo.RequestLock())
        {
            switch (a.Kind)
            {
                case OracleEditKind.Set:
                    SetWidth(state, a.Inst, a.Value);
                    _logic.DoVariableReferenceReaction(_comp, instance, "Width", state, a.Inst + ".Width", trySave: true);
                    break;
                case OracleEditKind.Reset:
                    SetWidth(state, a.Inst, null);
                    _logic.DoVariableReferenceReaction(_comp, instance, "Width", state, a.Inst + ".Width", trySave: true);
                    break;
                case OracleEditKind.ClearRow:
                    state.GetVariableListSave(a.Inst + ".VariableReferences")!.ValueAsIList.Clear();
                    _logic.DoVariableReferenceReaction(_comp, instance, "VariableReferences", state, a.Inst + ".VariableReferences", trySave: true);
                    break;
                case OracleEditKind.AddRow:
                    state.GetVariableListSave(a.Inst + ".VariableReferences")!.ValueAsIList.Add(a.Line!);
                    _logic.DoVariableReferenceReaction(_comp, instance, "VariableReferences", state, a.Inst + ".VariableReferences", trySave: true);
                    break;
            }
        }
        _fileCommands.Object.TryAutoSaveElement(_comp);
    }

    public void TypedMulti(params OracleEdit[] actions)
    {
        _selectedStateSave = _comp.DefaultState;
        using (_undo.RequestLock())
        {
            foreach (OracleEdit a in actions)
            {
                StateSave state = StateFor(a.State);
                InstanceSave instance = _comp.GetInstance(a.Inst)!;
                if (a.Kind == OracleEditKind.Set || a.Kind == OracleEditKind.Reset)
                {
                    SetWidth(state, a.Inst, a.Kind == OracleEditKind.Set ? a.Value : null);
                    _logic.DoVariableReferenceReaction(_comp, instance, "Width", state, a.Inst + ".Width", trySave: true);
                }
                else if (a.Kind == OracleEditKind.ClearRow)
                {
                    state.GetVariableListSave(a.Inst + ".VariableReferences")!.ValueAsIList.Clear();
                    _logic.DoVariableReferenceReaction(_comp, instance, "VariableReferences", state, a.Inst + ".VariableReferences", trySave: true);
                }
            }
        }
        _fileCommands.Object.TryAutoSaveElement(_comp);
    }

    public OracleOp? PickOp(Random random)
    {
        List<OracleOp> candidates = new();
        void ForState(string stateName, IEnumerable<string> insts)
        {
            StateSave state = StateFor(stateName);
            foreach (string inst in insts)
            {
                float? cur = CurrentWidth(state, inst);
                float newValue = random.Next(1, 200);
                if (newValue == cur) newValue++;
                candidates.Add(new OracleOp(
                    new OracleEdit(OracleEditKind.Set, inst, stateName, newValue, null),
                    cur == null ? new OracleEdit(OracleEditKind.Reset, inst, stateName, null, null) : new OracleEdit(OracleEditKind.Set, inst, stateName, cur, null)));
                if (cur != null)
                {
                    candidates.Add(new OracleOp(new OracleEdit(OracleEditKind.Reset, inst, stateName, null, null),
                        new OracleEdit(OracleEditKind.Set, inst, stateName, cur, null)));
                }
            }
        }
        ForState("Default", FollowerOps ? CompInstances : CompInstances.Where(item => item.StartsWith("S")).ToList());
        if (HasAlt) ForState("Default", new[] { "FA" }.Take(0));
        if (HasAlt) ForState("Alt", new[] { "S0" });

        void Rows(string stateName, string inst)
        {
            StateSave state = StateFor(stateName);
            VariableListSave? list = state.GetVariableListSave(inst + ".VariableReferences");
            if (list == null) return;
            if (list.ValueAsIList.Count > 0)
            {
                string line = (string)list.ValueAsIList[0]!;
                candidates.Add(new OracleOp(new OracleEdit(OracleEditKind.ClearRow, inst, stateName, null, null),
                    new OracleEdit(OracleEditKind.AddRow, inst, stateName, null, line)));
            }
            else
            {
                string line = CandidateRows.TryGetValue(inst, out string? c) ? c : stateName == "Alt" ? "Width = S0.Width + 1" : "Width = S0.Width";
                candidates.Add(new OracleOp(new OracleEdit(OracleEditKind.AddRow, inst, stateName, null, line),
                    new OracleEdit(OracleEditKind.ClearRow, inst, stateName, null, null)));
            }
        }
        foreach (string inst in CompInstances.Where(item => item.StartsWith("F"))) Rows("Default", inst);
        if (HasAlt) Rows("Alt", "FA");
        if (candidates.Count == 0) return null;
        return candidates[random.Next(candidates.Count)];
    }

    private static float? CurrentWidth(StateSave state, string inst)
    {
        object? v = state.Variables.FirstOrDefault(item => item.Name == inst + ".Width")?.Value;
        return v == null ? null : Convert.ToSingle(v);
    }

    public void Undo() => _undo.PerformUndo();
    public void Redo() => _undo.PerformRedo();
    public int HistoryCount => _undo.CurrentElementHistory?.Actions.Count ?? 0;
    public bool CanRedo => _undo.CanRedo();

    public bool MemoryEqualsDisk()
    {
        if (!_autoSave) return true;
        foreach (ComponentSave component in new[] { _comp, _other })
        {
            if (!FileManager.AreSaveObjectsEqual(FileManager.CloneSaveObject(component), _disk[component.Name])) return false;
        }
        return true;
    }

    public SortedDictionary<string, string> Snap()
    {
        SortedDictionary<string, string> result = new();
        foreach (ElementSave element in new ElementSave[] { _comp, _other })
        {
            foreach (StateSave state in element.AllStates)
            {
                foreach (VariableSave variable in state.Variables)
                {
                    if (variable.Value == null) continue;
                    string text;
                    try { text = Convert.ToSingle(variable.Value, CultureInfo.InvariantCulture).ToString("0.###", CultureInfo.InvariantCulture); }
                    catch { text = variable.Value.ToString() ?? ""; }
                    result[$"{element.Name}/{state.Name}/{variable.Name}"] = text;
                }
                foreach (VariableListSave list in state.VariableLists.Where(item => item.GetRootName() == "VariableReferences"))
                {
                    string rows = string.Join(" ; ", list.ValueAsIList.OfType<string>());
                    if (rows.Length > 0) result[$"{element.Name}/{state.Name}/{list.Name}"] = rows;
                }
            }
        }
        return result;
    }
}

/// <summary>
/// Random edits, resets and reference-row changes followed by undo and redo, checked against a typed-edit
/// oracle after every step (#6868, #6884).
/// </summary>
public class UndoTypedEditOracleSweepTests : BaseTestClass
{
    [Fact]
    public void Sweep_UndoAndRedoOfEditsResetsAndRowChanges_ShouldMatchTypingTheSameEdit()
    {
        // Followers are always materialized (they hold the values their rows give). A file written by hand
        // can leave a follower unstored; there, a typed edit re-applies every row of the element while undo
        // re-reads only the rows that read what it restored, so the two differ. That gap is accepted and is
        // left out of this comparison on purpose.
        int cases = SweepScale.Cases(300);
        Random random = new Random(68790);
        List<string> failures = new List<string>();
        for (int caseIndex = 0; caseIndex < cases; caseIndex++)
        {
            int buildSeed = random.Next();
            bool autoSave = random.Next(2) == 0;
            // A third of the cases add a categorized state whose row reads the default state's source (#6886).
            bool useAlt = random.Next(3) == 0;
            random.Next();
            List<string> steps = new List<string>();
            List<OracleEdit> typedSteps = new List<OracleEdit>();
            // Undoing a row add is typing the old value back, not typing the row away: the world is expected
            // to match what it was just before the add. Null for every other step, which uses the typed oracle.
            List<SortedDictionary<string, string>?> expectedBeforeAdd = new List<SortedDictionary<string, string>?>();
            Dictionary<OracleOp, SortedDictionary<string, string>> beforeEachAdd = new Dictionary<OracleOp, SortedDictionary<string, string>>(ReferenceEqualityComparer.Instance);
            List<SortedDictionary<string, string>> afterEachStep = new List<SortedDictionary<string, string>>();
            List<string> problems = new List<string>();
            string description;
            using (UndoOracleWorld world = new UndoOracleWorld())
            {
                world.Build(new Random(buildSeed), materialized: true, autoSave, useAlt, followerOps: true);
                description = world.Description;
                List<OracleOp> applied = new List<OracleOp>();
                int cursor = 0;
                for (int step = 0; step < 8; step++)
                {
                    int roll = random.Next(10);
                    int action = roll < 4 ? 0 : roll < 7 ? 1 : 2;
                    if (action == 0)
                    {
                        OracleOp? op = world.PickOp(random);
                        if (op == null)
                        {
                            continue;
                        }
                        SortedDictionary<string, string> beforeThisOp = world.Snap();
                        world.Typed(op.Fwd);
                        if (world.HistoryCount != cursor + 1 || world.CanRedo)
                        {
                            // The edit changed nothing, so it recorded nothing (a recorded edit drops the redo
                            // tail) and the model cannot continue.
                            break;
                        }
                        applied.RemoveRange(cursor, applied.Count - cursor);
                        applied.Add(op);
                        cursor = applied.Count;
                        beforeEachAdd[op] = beforeThisOp;
                        steps.Add("do " + op.Fwd);
                        typedSteps.Add(op.Fwd);
                        expectedBeforeAdd.Add(null);
                    }
                    else if (action == 1 && cursor > 0)
                    {
                        cursor--;
                        world.Undo();
                        steps.Add("undo " + applied[cursor].Fwd);
                        typedSteps.Add(applied[cursor].Inv);
                        expectedBeforeAdd.Add(applied[cursor].Fwd.Kind == OracleEditKind.AddRow ? beforeEachAdd[applied[cursor]] : null);
                    }
                    else if (action == 2 && cursor < applied.Count)
                    {
                        cursor++;
                        world.Redo();
                        steps.Add("redo " + applied[cursor - 1].Fwd);
                        typedSteps.Add(applied[cursor - 1].Fwd);
                        expectedBeforeAdd.Add(null);
                    }
                    else
                    {
                        continue;
                    }
                    afterEachStep.Add(world.Snap());
                    if (world.HistoryCount != applied.Count)
                    {
                        problems.Add($"history has {world.HistoryCount} entries, expected {applied.Count} after step {steps.Count}");
                    }
                    if (!world.MemoryEqualsDisk())
                    {
                        problems.Add($"memory differs from disk after step {steps.Count}");
                    }
                }
            }

            using (UndoOracleWorld oracle = new UndoOracleWorld())
            {
                oracle.Build(new Random(buildSeed), materialized: true, autoSave, useAlt, followerOps: true);
                for (int i = 0; i < typedSteps.Count; i++)
                {
                    SortedDictionary<string, string> typed;
                    if (expectedBeforeAdd[i] != null)
                    {
                        typed = expectedBeforeAdd[i]!;
                    }
                    else
                    {
                        oracle.Typed(typedSteps[i]);
                        typed = oracle.Snap();
                    }
                    SortedDictionary<string, string> undone = afterEachStep[i];
                    List<string> diffs = undone.Keys.Union(typed.Keys).OrderBy(key => key)
                        .Where(key => undone.GetValueOrDefault(key) != typed.GetValueOrDefault(key))
                        .Select(key => $"{key}: undo/redo={undone.GetValueOrDefault(key) ?? "-"} typed={typed.GetValueOrDefault(key) ?? "-"}")
                        .ToList();
                    if (diffs.Count > 0)
                    {
                        problems.Add($"step {i + 1} ({steps[i]}) differs from typing: " + string.Join("; ", diffs));
                        break;
                    }
                    if (expectedBeforeAdd[i] != null)
                    {
                        // The oracle world cannot be brought back to the pre-add values by typing, so the
                        // steps after an undone add are not compared.
                        break;
                    }
                }
            }

            if (problems.Count > 0)
            {
                failures.Add($"case {caseIndex}: {description}\n  steps: {string.Join(" -> ", steps)}\n  {string.Join("\n  ", problems)}");
            }
        }

        failures.Count.ShouldBe(0, string.Join("\n", failures.Take(3)));
    }
}
