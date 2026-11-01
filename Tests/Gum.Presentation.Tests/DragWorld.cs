using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.DataTypes.Behaviors;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins;
using Gum.Logic;
using Gum.Services;
using MonoGameGum.Tests;
using Gum.PropertyGridHelpers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using GumRuntime;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using Moq.AutoMock;
using ToolsUtilities;
using System.Globalization;

namespace Gum.Presentation.Tests;

/// <summary>
/// A small project driven through the real <see cref="SetVariableLogic"/>, the way the Variables tab drives it
/// while a value is dragged: intermediate ticks, then one full commit. Elements are plain components whose
/// instances' <c>Width</c> rows read each other, in the same element and across elements.
/// </summary>
public sealed class DragWorld : BaseTestClass
{
    private readonly ComponentSave _comp = NewComponent("Comp");
    private readonly ComponentSave _other = NewComponent("Other");
    private readonly ComponentSave _baseComponent = NewComponent("Base");
    private readonly List<ComponentSave> _extra = new List<ComponentSave>();
    private readonly Dictionary<string, ComponentSave> _disk = new Dictionary<string, ComponentSave>();
    private readonly Mock<ISelectedState> _selectedState = new Mock<ISelectedState>();
    private readonly Mock<IFileCommands> _fileCommands = new Mock<IFileCommands>();
    private readonly VariableReferenceLogic _logic;
    private readonly UndoManager _undo;
    private readonly SetVariableLogic _setVariable;
    private StateSave _selectedStateSave;
    private bool _autoSave = true;

    public List<string> CompInstances { get; } = new List<string>();
    public bool HasAlt { get; private set; }
    public string Description { get; private set; } = "";
    public int SaveCount { get; private set; }

    public DragWorld()
    {
        GumExpressionService.Initialize();
        _selectedStateSave = _comp.DefaultState;
        _selectedState.Setup(x => x.SelectedElement).Returns(_comp);
        _selectedState.Setup(x => x.SelectedComponent).Returns(_comp);
        _selectedState.Setup(x => x.SelectedStateSave).Returns(() => _selectedStateSave);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(() => _comp.Categories.FirstOrDefault(item => item.States.Contains(_selectedStateSave)));
        _fileCommands.Setup(x => x.TryAutoSaveElement(It.IsAny<ElementSave?>())).Callback<ElementSave?>(SaveToFakeDisk);
        _fileCommands.Setup(x => x.TryAutoSaveCurrentElement()).Callback(() => SaveToFakeDisk(_comp));

        Mock<ICompositeMemberRegistry> registry = new Mock<ICompositeMemberRegistry>();
        registry.Setup(x => x.Descriptors).Returns(new List<CompositeMemberDescriptor>());
        Mock<IWireframeObjectManager> wireframe = new Mock<IWireframeObjectManager>();
        wireframe.Setup(x => x.AllIpsos).Returns(new List<GraphicalUiElement>());
        _logic = new VariableReferenceLogic(new Mock<IGuiCommands>().Object, new Mock<IWireframeCommands>().Object,
            new Mock<IDialogService>().Object, _fileCommands.Object, registry.Object,
            new Mock<IDispatcher>().Object, wireframe.Object);

        Mock<IReferenceFinderProjectProvider> projectProvider = new Mock<IReferenceFinderProjectProvider>();
        projectProvider.Setup(x => x.GumProjectSave).Returns(() => ObjectFinder.Self.GumProjectSave);
        Mock<INameVerifier> nameVerifier = new Mock<INameVerifier>();
        Mock<IRenameProjectProvider> renameProjectProvider = new Mock<IRenameProjectProvider>();
        renameProjectProvider.Setup(x => x.GumProjectSave).Returns(() => ObjectFinder.Self.GumProjectSave);
        RenameLogic rename = new RenameLogic(_selectedState.Object, nameVerifier.Object, new Mock<IDialogService>().Object,
            new Mock<IGuiCommands>().Object, _fileCommands.Object, new Mock<IDeleteLogic>().Object,
            renameProjectProvider.Object, new Mock<IRenamePluginNotifier>().Object,
            new Mock<IStandardElementsManagerGumTool>().Object, new ReferenceFinder(projectProvider.Object),
            new Lazy<IUndoManager>(() => _undo));
        _undo = new UndoManager(_selectedState.Object, rename, new Mock<IGuiCommands>().Object,
            _fileCommands.Object, new Mock<IMessenger>().Object, new Mock<IUndoPluginNotifier>().Object,
            new Mock<IAnimationUndoProvider>().Object, projectProvider.Object, new Lazy<IVariableReferenceLogic>(() => _logic));

        AutoMocker mocker = new AutoMocker();
        mocker.Use<ISelectedState>(_selectedState.Object);
        mocker.Use<IFileCommands>(_fileCommands.Object);
        mocker.Use<IVariableReferenceLogic>(_logic);
        mocker.Use<IUndoManager>(_undo);
        mocker.Use<IWireframeObjectManager>(wireframe.Object);
        _setVariable = mocker.CreateInstance<SetVariableLogic>();
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        base.Dispose();
    }

    public ComponentSave Comp => _comp;
    public int HistoryCount => _undo.CurrentElementHistory?.Actions.Count ?? 0;

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
            if (variable != null)
            {
                state.Variables.Remove(variable);
            }
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

    private static void AddRow(StateSave state, string owner, string row)
    {
        state.VariableLists.Add(new VariableListSave<string> { Name = owner + ".VariableReferences", Type = "string", Value = new List<string> { row } });
    }

    private void SaveToFakeDisk(ElementSave? element)
    {
        SaveCount++;
        if (_autoSave && element is ComponentSave component)
        {
            _disk[component.Name] = FileManager.CloneSaveObject(component);
        }
    }

    /// <summary>
    /// Comp has the dragged sources S0 and S1 and followers F0..; Other holds cross-element readers of Comp;
    /// optionally a categorized state with its own source and follower.
    /// </summary>
    public void BuildRandom(Random g, bool autoSave, bool useAlt)
    {
        _autoSave = autoSave;
        HasAlt = useAlt;
        Description = $"autoSave={autoSave} alt={useAlt}";
        List<string> names = new List<string> { "S0", "S1" };
        NewInstance(_comp, "S0");
        NewInstance(_comp, "S1");
        SetWidth(_comp.DefaultState, "S0", 50);
        SetWidth(_comp.DefaultState, "S1", 60);
        int followerCount = g.Next(2, 6);
        for (int i = 0; i < followerCount; i++)
        {
            string name = "F" + i;
            NewInstance(_comp, name);
            string source = g.Next(10) < 6 ? names[names.Count - 1] : names[g.Next(names.Count)];
            int offset = g.Next(0, 3);
            string line = $"Width = {source}.Width" + (offset > 0 ? $" + {offset}" : "");
            AddRow(_comp.DefaultState, name, line);
            Description += $" | {name}: {line}";
            names.Add(name);
        }
        CompInstances.AddRange(names);

        int otherCount = g.Next(1, 4);
        List<string> otherNames = new List<string>();
        for (int j = 0; j < otherCount; j++)
        {
            string name = "O" + j;
            NewInstance(_other, name);
            string line = j > 0 && g.Next(3) == 0
                ? $"Width = {otherNames[g.Next(otherNames.Count)]}.Width + 1"
                : $"Width = Components/Comp.{names[g.Next(names.Count)]}.Width";
            AddRow(_other.DefaultState, name, line);
            Description += $" | {name}: {line}";
            otherNames.Add(name);
        }

        if (useAlt)
        {
            NewInstance(_comp, "FA");
            StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
            StateSave alt = new StateSave { Name = "Alt", ParentContainer = _comp };
            category.States.Add(alt);
            _comp.Categories.Add(category);
            SetWidth(alt, "S0", 70);
            AddRow(alt, "FA", "Width = S0.Width + 1");
            Description += " | Alt: S0=70, FA: Width = S0.Width + 1";
        }
        Finish(materialize: true);
    }

    /// <summary>
    /// Comp.S0 is the dragged source. <paramref name="readers"/> components read it across elements;
    /// <paramref name="bystanders"/> components read only Base, so nothing in them reads Comp.
    /// </summary>
    public void BuildScale(int readers, int bystanders)
    {
        NewInstance(_comp, "S0");
        SetWidth(_comp.DefaultState, "S0", 50);
        CompInstances.Add("S0");
        NewInstance(_baseComponent, "B");
        SetWidth(_baseComponent.DefaultState, "B", 5);
        for (int i = 0; i < readers + bystanders; i++)
        {
            ComponentSave component = NewComponent("C" + i);
            NewInstance(component, "Box");
            AddRow(component.DefaultState, "Box", i < readers ? "Width = Components/Comp.S0.Width + 1" : "Width = Components/Base.B.Width + 1");
            _extra.Add(component);
        }
        Finish(materialize: true);
    }

    /// <summary>
    /// Comp has the dragged source S0 and a same-element follower F0 (Width = S0.Width + 1); Other holds a
    /// cross-element reader O0 of Comp.S0. Comp also carries a behavior whose tool-only line
    /// <c>StackSpacing = Spacing</c> mirrors the Forms property <c>Spacing</c> the way StackPanel does.
    /// </summary>
    public void BuildSameElement()
    {
        NewInstance(_comp, "S0");
        SetWidth(_comp.DefaultState, "S0", 50);
        NewInstance(_comp, "F0");
        AddRow(_comp.DefaultState, "F0", "Width = S0.Width + 1");
        CompInstances.Add("S0");
        NewInstance(_other, "O0");
        AddRow(_other.DefaultState, "O0", "Width = Components/Comp.S0.Width");

        _comp.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "SpacingBehavior" });
        _comp.DefaultState.Variables.Add(new VariableSave { Name = "Spacing", Type = "float", Value = 4f, SetsValue = true });
        Finish(materialize: true);

        BehaviorSave behavior = new BehaviorSave { Name = "SpacingBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "float", Name = "Spacing" });
        behavior.ToolOnlyVariableReferences.Add("StackSpacing = Spacing");
        ObjectFinder.Self.GumProjectSave!.Behaviors.Add(behavior);
    }

    /// <summary>Drags the element-level Spacing variable: an intermediate tick.</summary>
    public void TickSpacing(float value) => ReportSpacing(value, isFullCommit: false);

    /// <summary>The full commit of a Spacing edit.</summary>
    public void CommitSpacing(float value) => ReportSpacing(value, isFullCommit: true);

    private void ReportSpacing(float value, bool isFullCommit)
    {
        _selectedStateSave = _comp.DefaultState;
        VariableSave variable = _comp.DefaultState.Variables.Single(item => item.Name == "Spacing");
        object? old = variable.Value;
        variable.Value = value;
        _setVariable.PropertyValueChanged("Spacing", old, null, _comp.DefaultState, refresh: true,
            recordUndo: isFullCommit, trySave: isFullCommit, isFullCommit: isFullCommit);
    }

    public object? StackSpacing => _comp.DefaultState.GetValue("StackSpacing");

    /// <summary>
    /// Comp has the dragged source S0 and <paramref name="count"/> instances of the Forms-style component Btn,
    /// whose behavior derives <c>ButtonCategoryState</c> from <c>IsEnabled</c>.
    /// </summary>
    public void BuildButtons(int count)
    {
        NewInstance(_comp, "S0");
        SetWidth(_comp.DefaultState, "S0", 50);
        _extra.Add(NewComponent("Btn"));
        for (int i = 0; i < count; i++)
        {
            InstanceSave button = new InstanceSave { Name = "Btn" + i, BaseType = "Btn", ParentContainer = _comp };
            _comp.Instances.Add(button);
        }
        _extra[0].Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });
        Finish(materialize: true);

        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled" });
        behavior.ToolOnlyVariableReferences.Add("ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");
        ObjectFinder.Self.GumProjectSave!.Behaviors.Add(behavior);
    }

    /// <summary>Reports a tick of <c>Btn{index}.IsEnabled</c>.</summary>
    public void TickIsEnabled(int index, bool value)
    {
        _selectedStateSave = _comp.DefaultState;
        string name = $"Btn{index}.IsEnabled";
        VariableSave variable = new VariableSave { Name = name, Type = "bool", Value = value, SetsValue = true };
        _comp.DefaultState.Variables.Add(variable);
        _setVariable.PropertyValueChanged("IsEnabled", null, _comp.GetInstance("Btn" + index)!, _comp.DefaultState,
            refresh: true, recordUndo: false, trySave: false, isFullCommit: false);
    }

    public object? ButtonCategoryStateOf(int index) => _comp.DefaultState.GetValue($"Btn{index}.ButtonCategoryState");

    public int ButtonStateWrites => _comp.DefaultState.Variables.Count(item => item.Name.EndsWith(".ButtonCategoryState"));

    private void Finish(bool materialize)
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementSave rectangle = new StandardElementSave { Name = "Rectangle" };
        StateSave rectangleDefault = new StateSave { Name = "Default", ParentContainer = rectangle };
        rectangleDefault.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 0f, SetsValue = true });
        rectangle.States.Add(rectangleDefault);
        project.StandardElements.Add(rectangle);
        List<ComponentSave> all = new List<ComponentSave> { _comp, _other, _baseComponent };
        all.AddRange(_extra);
        foreach (ComponentSave component in all)
        {
            project.Components.Add(component);
            project.ComponentReferences.Add(new ElementReference { Name = component.Name, ElementType = ElementType.Component });
        }
        ObjectFinder.Self.GumProjectSave = project;
        project.Initialize();

        if (materialize)
        {
            foreach (ComponentSave component in _extra.Concat(new[] { _other, _comp }))
            {
                foreach (StateSave state in component.AllStates)
                {
                    ElementSaveExtensions.ApplyVariableReferences(component, state, null);
                }
            }
        }
        foreach (ComponentSave component in all)
        {
            _disk[component.Name] = FileManager.CloneSaveObject(component);
        }
        _undo.RecordState();
    }

    private StateSave StateFor(string name) => name == "Default" ? _comp.DefaultState : _comp.Categories.Single().States.Single();

    /// <summary>Writes the value on the instance's state and reports it the way an intermediate drag tick does.</summary>
    public void Tick(string instance, string state, float value) => Report(instance, state, value, isFullCommit: false);

    /// <summary>Writes the value and reports it the way the release of a drag, or a typed value, does.</summary>
    public void Commit(string instance, string state, float value) => Report(instance, state, value, isFullCommit: true);

    private void Report(string instanceName, string stateName, float value, bool isFullCommit)
    {
        StateSave state = StateFor(stateName);
        _selectedStateSave = state;
        InstanceSave instance = _comp.GetInstance(instanceName)!;
        object? old = state.Variables.FirstOrDefault(item => item.Name == instanceName + ".Width")?.Value;
        SetWidth(state, instanceName, value);
        _setVariable.PropertyValueChanged("Width", old, instance, state, refresh: true,
            recordUndo: isFullCommit, trySave: isFullCommit, isFullCommit: isFullCommit);
    }

    public float? WidthOf(ElementSave element, string stateName, string instanceName)
    {
        StateSave? state = element.AllStates.FirstOrDefault(item => item.Name == stateName);
        object? value = state?.Variables.FirstOrDefault(item => item.Name == instanceName + ".Width")?.Value;
        return value == null ? null : Convert.ToSingle(value, CultureInfo.InvariantCulture);
    }

    public ComponentSave Component(string name) => (ComponentSave)ObjectFinder.Self.GumProjectSave!.Components.Single(item => item.Name == name);

    public bool MemoryEqualsDisk()
    {
        if (!_autoSave)
        {
            return true;
        }
        foreach (ComponentSave component in ObjectFinder.Self.GumProjectSave!.Components)
        {
            if (!FileManager.AreSaveObjectsEqual(FileManager.CloneSaveObject(component), _disk[component.Name]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Every variable value and reference row in the project, as text.</summary>
    public SortedDictionary<string, string> Snap()
    {
        SortedDictionary<string, string> result = new SortedDictionary<string, string>();
        foreach (ComponentSave element in ObjectFinder.Self.GumProjectSave!.Components)
        {
            foreach (StateSave state in element.AllStates)
            {
                foreach (VariableSave variable in state.Variables)
                {
                    if (variable.Value == null)
                    {
                        continue;
                    }
                    string text;
                    try
                    {
                        text = Convert.ToSingle(variable.Value, CultureInfo.InvariantCulture).ToString("0.###", CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        text = variable.Value.ToString() ?? "";
                    }
                    result[$"{element.Name}/{state.Name}/{variable.Name}"] = text;
                }
                foreach (VariableListSave list in state.VariableLists.Where(item => item.GetRootName() == "VariableReferences"))
                {
                    string rows = string.Join(" ; ", list.ValueAsIList.OfType<string>());
                    if (rows.Length > 0)
                    {
                        result[$"{element.Name}/{state.Name}/{list.Name}"] = rows;
                    }
                }
            }
        }
        return result;
    }
}
