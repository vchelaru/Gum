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

namespace Gum.Presentation.Tests;

/// <summary>
/// An undo or redo has the same effect on the rest of the app as typing the old (or new) value, except it adds no
/// history entry (#6868). The general condition: the restored snapshot must equal what the reference cascade
/// produces from the restored sources, whatever the snapshots held for the followers. Real UndoManager and
/// VariableReferenceLogic; the element's Auto Save is a fake disk of element copies.
/// Comp holds Source, F1 (Width = Source.Width) and F2 (Width = F1.Width + 1); Other.O reads Comp.Source.Width.
/// </summary>
public class UndoReferenceCascadeTests : BaseTestClass
{
    private const string OtherRow = "Width = Components/Comp.Source.Width";

    private readonly ComponentSave _comp = NewComponent("Comp");
    private readonly ComponentSave _other = NewComponent("Other");
    private readonly ScreenSave _screen = NewScreen("Scr");
    private readonly Dictionary<string, ComponentSave> _disk = new();
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly VariableReferenceLogic _logic;
    private readonly UndoManager _undo;
    private readonly RenameLogic _rename;
    private ElementSave _selectedElement;
    private StateSave _selectedStateSave;
    private bool _autoSave = true;

    public UndoReferenceCascadeTests()
    {
        GumExpressionService.Initialize();

        _selectedElement = _comp;
        _selectedStateSave = _comp.DefaultState;
        _selectedState.Setup(x => x.SelectedElement).Returns(() => _selectedElement);
        _selectedState.Setup(x => x.SelectedComponent).Returns(() => _selectedElement as ComponentSave);
        _selectedState.Setup(x => x.SelectedStateSave).Returns(() => _selectedStateSave);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(() => _selectedElement.Categories.FirstOrDefault(item => item.States.Contains(_selectedStateSave)));

        _fileCommands.Setup(x => x.TryAutoSaveElement(It.IsAny<ElementSave?>()))
            .Callback<ElementSave?>(SaveToFakeDisk);
        _fileCommands.Setup(x => x.TryAutoSaveCurrentElement()).Callback(() => SaveToFakeDisk(_selectedElement));

        Mock<IWireframeObjectManager> wireframe = new();
        wireframe.Setup(x => x.AllIpsos).Returns(new List<GraphicalUiElement>());
        Mock<ICompositeMemberRegistry> compositeRegistry = new();
        compositeRegistry.Setup(x => x.Descriptors).Returns(new List<CompositeMemberDescriptor>());
        _logic = new VariableReferenceLogic(new Mock<IGuiCommands>().Object, new Mock<IWireframeCommands>().Object,
            new Mock<IDialogService>().Object, _fileCommands.Object, compositeRegistry.Object,
            new Mock<IDispatcher>().Object, wireframe.Object);

        Mock<IReferenceFinderProjectProvider> projectProvider = new();
        projectProvider.Setup(x => x.GumProjectSave).Returns(() => ObjectFinder.Self.GumProjectSave);
        Mock<INameVerifier> nameVerifier = new();
        string? whyNot;
        nameVerifier.Setup(x => x.IsInstanceNameValid(It.IsAny<string>(), It.IsAny<InstanceSave?>(), It.IsAny<IInstanceContainer>(), out whyNot))
            .Returns(true);
        nameVerifier.Setup(x => x.IsStateNameValid(It.IsAny<string>(), It.IsAny<StateSaveCategory?>(), It.IsAny<StateSave>(), out whyNot))
            .Returns(true);
        Mock<IRenameProjectProvider> renameProjectProvider = new();
        renameProjectProvider.Setup(x => x.GumProjectSave).Returns(() => ObjectFinder.Self.GumProjectSave);
        _rename = new RenameLogic(_selectedState.Object, nameVerifier.Object, new Mock<IDialogService>().Object,
            new Mock<IGuiCommands>().Object, _fileCommands.Object, new Mock<IDeleteLogic>().Object,
            renameProjectProvider.Object, new Mock<IRenamePluginNotifier>().Object,
            new Mock<IStandardElementsManagerGumTool>().Object, new ReferenceFinder(projectProvider.Object),
            new Lazy<IUndoManager>(() => _undo));
        _undo = CreateUndoManager(projectProvider.Object);
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        base.Dispose();
    }

    private UndoManager CreateUndoManager(IReferenceFinderProjectProvider projectProvider) => new UndoManager(
        _selectedState.Object, _rename, new Mock<IGuiCommands>().Object,
        _fileCommands.Object, new Mock<IMessenger>().Object, new Mock<IUndoPluginNotifier>().Object,
        new Mock<IAnimationUndoProvider>().Object, projectProvider, new Lazy<IVariableReferenceLogic>(() => _logic));

    private static ComponentSave NewComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    private static ScreenSave NewScreen(string name)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        return screen;
    }

    private static InstanceSave NewInstance(ElementSave parent, string name, float width)
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

    private static void AddRow(StateSave state, string owner, params string[] rows)
    {
        state.VariableLists.Add(new VariableListSave<string>
        {
            Name = owner + ".VariableReferences",
            Type = "string",
            Value = rows.ToList(),
        });
    }

    private void SaveToFakeDisk(ElementSave? element)
    {
        if (_autoSave && element is ComponentSave component)
        {
            _disk[component.Name] = FileManager.CloneSaveObject(component);
        }
    }

    /// <summary>
    /// Builds the project. When <paramref name="materialized"/> the followers hold the values their rows give for
    /// Source.Width = 50, as after the tool applied the rows; otherwise they hold nothing (rows authored by hand).
    /// </summary>
    private void Arrange(bool materialized = true, bool autoSave = true)
    {
        _autoSave = autoSave;
        NewInstance(_comp, "Source", 50);
        NewInstance(_comp, "F1", 0);
        NewInstance(_comp, "F2", 0);
        NewInstance(_other, "O", 0);
        SetWidth(_comp.DefaultState, "Source", 50);
        AddRow(_comp.DefaultState, "F1", "Width = Source.Width");
        AddRow(_comp.DefaultState, "F2", "Width = F1.Width + 1");
        AddRow(_other.DefaultState, "O", OtherRow);
        // A screen with an instance of Comp, and a rectangle reading that instance's Width.
        InstanceSave componentInstance = NewInstance(_screen, "C1", 0);
        componentInstance.BaseType = "Comp";
        NewInstance(_screen, "S2", 0);
        SetWidth(_screen.DefaultState, "C1", 40);
        AddRow(_screen.DefaultState, "S2", "Width = C1.Width");

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(_comp);
        project.Components.Add(_other);
        project.ComponentReferences.Add(new ElementReference { Name = "Comp", ElementType = ElementType.Component });
        project.ComponentReferences.Add(new ElementReference { Name = "Other", ElementType = ElementType.Component });
        project.Screens.Add(_screen);
        project.ScreenReferences.Add(new ElementReference { Name = "Scr", ElementType = ElementType.Screen });
        ObjectFinder.Self.GumProjectSave = project;
        project.Initialize();

        if (materialized)
        {
            ElementSaveExtensions.ApplyVariableReferences(_comp, _comp.DefaultState, null);
            ElementSaveExtensions.ApplyVariableReferences(_other, _other.DefaultState, null);
            ElementSaveExtensions.ApplyVariableReferences(_screen, _screen.DefaultState, null);
        }
        _disk["Comp"] = FileManager.CloneSaveObject(_comp);
        _disk["Other"] = FileManager.CloneSaveObject(_other);
        _undo.RecordState();
    }

    // Typing a row validates its left side against the instance type's variables, so a test that types a row
    // needs the Rectangle standard element. The other tests leave it out, so a reset variable reads as null.
    private static void AddRectangleStandard()
    {
        StandardElementSave rectangle = new StandardElementSave { Name = "Rectangle" };
        StateSave rectangleDefault = new StateSave { Name = "Default", ParentContainer = rectangle };
        rectangleDefault.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 0f, SetsValue = true });
        rectangle.States.Add(rectangleDefault);
        ObjectFinder.Self.GumProjectSave!.StandardElements.Add(rectangle);
    }

    private void EditSourceWidth(float value, StateSave? state = null)
    {
        state ??= _comp.DefaultState;
        using (_undo.RequestLock())
        {
            SetWidth(state, "Source", value);
            _logic.DoVariableReferenceReaction(_comp, _comp.GetInstance("Source"), "Width", state,
                "Source.Width", trySave: true);
        }
        // SetVariableLogic saves the edited element itself once the cascade has run.
        _fileCommands.Object.TryAutoSaveElement(_comp);
    }

    private static float? Width(ElementSave element, string instanceName, StateSave? state = null)
    {
        object? value = (state ?? element.DefaultState).Variables.FirstOrDefault(item => item.Name == instanceName + ".Width")?.Value;
        return value == null ? null : Convert.ToSingle(value);
    }

    private void ShouldHaveWidths(float source, StateSave? state = null)
    {
        Width(_comp, "Source", state).ShouldBe(source, "Source");
        Width(_comp, "F1", state).ShouldBe(source, "F1 follows Source");
        Width(_comp, "F2", state).ShouldBe(source + 1, "F2 follows F1");
        Width(_other, "O").ShouldBe(source, "O in another element follows Source");
    }

    private void MemoryShouldEqualDisk()
    {
        if (!_autoSave)
        {
            return;
        }
        foreach (ComponentSave component in new[] { _comp, _other })
        {
            FileManager.AreSaveObjectsEqual(FileManager.CloneSaveObject(component), _disk[component.Name])
                .ShouldBeTrue("disk should equal memory for " + component.Name);
        }
    }

    private int HistoryCount => _undo.CurrentElementHistory?.Actions.Count ?? 0;

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void UndoRedo_AfterEditingAFeedingVariable_ShouldLeaveFollowersWhereATypedEditWould(bool materialized, bool autoSave)
    {
        Arrange(materialized, autoSave);
        EditSourceWidth(55);
        ShouldHaveWidths(55);
        MemoryShouldEqualDisk();
        HistoryCount.ShouldBe(1);

        _undo.PerformUndo();
        ShouldHaveWidths(50);
        MemoryShouldEqualDisk();
        HistoryCount.ShouldBe(1);

        _undo.PerformRedo();
        ShouldHaveWidths(55);
        MemoryShouldEqualDisk();
        HistoryCount.ShouldBe(1);
    }

    [Fact]
    public void UndoRedo_AfterTwoEdits_ShouldFollowTheReferenceAtEveryStep()
    {
        Arrange();
        EditSourceWidth(55);
        EditSourceWidth(70);

        _undo.PerformUndo();
        ShouldHaveWidths(55);
        _undo.PerformUndo();
        ShouldHaveWidths(50);
        _undo.PerformRedo();
        ShouldHaveWidths(55);
        _undo.PerformRedo();
        ShouldHaveWidths(70);
        MemoryShouldEqualDisk();
        HistoryCount.ShouldBe(2);
    }

    [Fact]
    public void Undo_PastTheBeginning_ShouldChangeNothing()
    {
        Arrange();
        EditSourceWidth(55);
        _undo.PerformUndo();

        _undo.PerformUndo();

        ShouldHaveWidths(50);
        MemoryShouldEqualDisk();
        _undo.CanUndo().ShouldBeFalse();
    }

    [Fact]
    public void UndoRedo_OfAnEditInACategorizedState_ShouldFollowTheReferenceInThatState()
    {
        Arrange();
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        StateSave alt = new StateSave { Name = "Alt", ParentContainer = _comp };
        category.States.Add(alt);
        _comp.Categories.Add(category);
        SetWidth(alt, "Source", 60);
        AddRow(alt, "F1", "Width = Source.Width");
        SetWidth(alt, "F1", 60);
        _selectedStateSave = alt;
        _undo.RecordState();

        EditSourceWidth(65, alt);
        Width(_comp, "F1", alt).ShouldBe(65);
        HistoryCount.ShouldBe(1);

        // Undo replaces a category's states with copies, so look the state up again.
        StateSave Alt() => _comp.Categories.Single().States.Single();
        _undo.PerformUndo();
        Width(_comp, "Source", Alt()).ShouldBe(60);
        Width(_comp, "F1", Alt()).ShouldBe(60);
        _undo.PerformRedo();
        Width(_comp, "F1", Alt()).ShouldBe(65);
        Width(_comp, "F1").ShouldBe(50, "the default state is untouched");
    }

    private void RemoveRow(string follower)
    {
        using (_undo.RequestLock())
        {
            _comp.DefaultState.GetVariableListSave(follower + ".VariableReferences")!.ValueAsIList.Clear();
            _logic.DoVariableReferenceReaction(_comp, _comp.GetInstance(follower), "VariableReferences",
                _comp.DefaultState, follower + ".VariableReferences", trySave: true);
        }
        _fileCommands.Object.TryAutoSaveElement(_comp);
    }

    private void EditF2Width(float? value)
    {
        using (_undo.RequestLock())
        {
            SetWidth(_comp.DefaultState, "F2", value);
            _logic.DoVariableReferenceReaction(_comp, _comp.GetInstance("F2"), "Width", _comp.DefaultState,
                "F2.Width", trySave: true);
        }
        _fileCommands.Object.TryAutoSaveElement(_comp);
    }

    // An edit made after an undo drops the redo stack; undoing that edit must not bring back a row an earlier
    // action removed (#6887). The follower has no row, so its
    // edit sticks and is recorded.
    [Theory]
    [InlineData(true, true, true, 99f)]
    [InlineData(true, false, true, 99f)]
    [InlineData(true, true, true, null)]
    [InlineData(true, true, false, null)]
    [InlineData(true, true, false, 99f)]
    [InlineData(false, true, true, 99f)]
    [InlineData(false, true, true, null)]
    public void Undo_OfAFollowerEditMadeAfterAnUndo_ShouldKeepAnEarlierRemovedRowRemoved(bool materialized, bool autoSave, bool resetSource, float? followerValue)
    {
        Arrange(materialized, autoSave);
        AddRectangleStandard();
        RemoveRow("F1");
        RemoveRow("F2");
        if (resetSource)
        {
            ResetSource();
        }
        else
        {
            EditSourceWidth(55);
        }
        _undo.PerformUndo();

        EditF2Width(followerValue);
        HistoryCount.ShouldBe(3, "the follower edit is a new action and drops the undone source edit");
        _undo.CanRedo().ShouldBeFalse();
        _undo.PerformUndo();

        _comp.DefaultState.GetVariableListSave("F1.VariableReferences")!.ValueAsIList.Count.ShouldBe(0, "the row stays removed, as after the first action");
        MemoryShouldEqualDisk();
    }

    [Fact]
    public void Undo_AfterAFollowerEditThatRecordsNothing_ShouldPutBackTheRemovedRow()
    {
        Arrange();
        AddRectangleStandard();
        RemoveRow("F1");
        EditSourceWidth(55);
        _undo.PerformUndo();

        // F2 still has its row, so the typed value is re-read from F1 and nothing changes.
        EditF2Width(99);
        HistoryCount.ShouldBe(2, "the edit changed nothing, so it recorded nothing");
        _undo.CanRedo().ShouldBeTrue("the undone source edit is still on the redo stack");
        _undo.PerformUndo();

        _comp.DefaultState.GetVariableListSave("F1.VariableReferences")!.ValueAsIList.Count.ShouldBe(1, "the undo reached the row removal");
        MemoryShouldEqualDisk();
    }

    [Fact]
    public void UndoRedo_OfAnEditThatRemovedAReferenceRow_ShouldPutTheRowAndItsValueBack()
    {
        Arrange();
        using (_undo.RequestLock())
        {
            VariableListSave rows = _comp.DefaultState.GetVariableListSave("F1.VariableReferences")!;
            rows.ValueAsIList.Clear();
            SetWidth(_comp.DefaultState, "F1", 10);
            _logic.DoVariableReferenceReaction(_comp, _comp.GetInstance("F1"), "VariableReferences",
                _comp.DefaultState, "F1.VariableReferences", trySave: true);
        }
        EditSourceWidth(55);
        Width(_comp, "F1").ShouldBe(10, "no row, no follow");

        _undo.PerformUndo();
        _undo.PerformUndo();
        _comp.DefaultState.GetVariableListSave("F1.VariableReferences")!.ValueAsIList.Count.ShouldBe(1);
        ShouldHaveWidths(50);

        _undo.PerformRedo();
        _comp.DefaultState.GetVariableListSave("F1.VariableReferences")!.ValueAsIList.Count.ShouldBe(0);
        Width(_comp, "F1").ShouldBe(10);
    }

    [Fact]
    public void UndoRedo_OfOneLockWithTwoRows_ShouldFollowBothReferences()
    {
        Arrange();
        _comp.DefaultState.GetVariableListSave("F1.VariableReferences")!.ValueAsIList.Add("Height = Source.Height");
        _undo.RecordState();

        using (_undo.RequestLock())
        {
            SetWidth(_comp.DefaultState, "Source", 55);
            _logic.DoVariableReferenceReaction(_comp, _comp.GetInstance("Source"), "Width", _comp.DefaultState,
                "Source.Width", trySave: true);
            _comp.DefaultState.Variables.Add(new VariableSave { Name = "Source.Height", Type = "float", Value = 9f, SetsValue = true });
            _logic.DoVariableReferenceReaction(_comp, _comp.GetInstance("Source"), "Height", _comp.DefaultState,
                "Source.Height", trySave: true);
        }
        Width(_comp, "F1").ShouldBe(55);

        _undo.PerformUndo();
        Width(_comp, "F1").ShouldBe(50);
        Width(_other, "O").ShouldBe(50);
        _undo.PerformRedo();
        Width(_comp, "F1").ShouldBe(55);
        Width(_other, "O").ShouldBe(55);
        MemoryShouldEqualDisk();
    }

    [Fact]
    public void UndoRedo_AfterAnOutsideReloadRebasedTheSnapshots_ShouldKeepFollowingTheReference()
    {
        Arrange();
        EditSourceWidth(55);
        // An outside edit of Comp.Source (file reload) rewrote O; the tool rebases the snapshots.
        SetWidth(_comp.DefaultState, "Source", 80);
        StoredValueChange[] changes = _logic.ApplyReferencesToElement(_comp, trySave: true);
        _undo.RebaseStoredValues(changes);
        Width(_other, "O").ShouldBe(80);

        _undo.PerformUndo();

        Width(_comp, "Source").ShouldBe(50, "the history restores the edited variable");
        Width(_other, "O").ShouldBe(50, "O follows the restored source");
        _undo.PerformRedo();
        Width(_comp, "Source").ShouldBe(55);
        Width(_other, "O").ShouldBe(55);
    }

    [Fact]
    public void UndoRedo_OfApplyingAReference_ShouldKeepTheTypedValueAnUndoRestores()
    {
        // The value a user typed over a stale reference is what the undo restores. Its own row is not
        // re-read, since a typed edit of a variable updates what reads it, not the row that feeds it.
        Arrange();
        SetWidth(_comp.DefaultState, "F1", 10);
        _undo.RecordState();
        using (_undo.RequestLock())
        {
            // The Apply reference button re-evaluates F1's row.
            ElementSaveExtensions.ApplyVariableReferences(_comp, _comp.DefaultState, null);
        }
        Width(_comp, "F1").ShouldBe(50);

        _undo.PerformUndo();
        Width(_comp, "F1").ShouldBe(10);
        _undo.PerformRedo();
        Width(_comp, "F1").ShouldBe(50);
        _undo.PerformUndo();
        Width(_comp, "F1").ShouldBe(10);
        HistoryCount.ShouldBe(1);
    }

    [Fact]
    public void UndoRedo_OfResettingAFeedingVariable_ShouldGiveFollowersTheRestoredValue()
    {
        // Make Default on the source variable removes its stored value. Followers start unmaterialized,
        // so only the cascade can give them the restored value on undo.
        Arrange(materialized: false);
        void Reset()
        {
            using (_undo.RequestLock())
            {
                _comp.DefaultState.Variables.RemoveAll(item => item.Name == "Source.Width");
                _logic.DoVariableReferenceReaction(_comp, _comp.GetInstance("Source"), "Width", _comp.DefaultState,
                    "Source.Width", trySave: true);
            }
        }
        Reset();

        _undo.PerformUndo();
        ShouldHaveWidths(50);
        _undo.PerformRedo();
        Width(_comp, "Source").ShouldBeNull();
        _undo.PerformUndo();
        ShouldHaveWidths(50);
        MemoryShouldEqualDisk();
    }

    [Fact]
    public void UndoRedo_OnAScreenEditingAComponentInstance_ShouldFollowTheInstancesReadingIt()
    {
        Arrange(materialized: false);
        _selectedElement = _screen;
        _selectedStateSave = _screen.DefaultState;
        _undo.RecordState();

        using (_undo.RequestLock())
        {
            SetWidth(_screen.DefaultState, "C1", 70);
            _logic.DoVariableReferenceReaction(_screen, _screen.GetInstance("C1"), "Width", _screen.DefaultState,
                "C1.Width", trySave: true);
        }
        Width(_screen, "S2").ShouldBe(70);

        _undo.PerformUndo();
        Width(_screen, "C1").ShouldBe(40);
        Width(_screen, "S2").ShouldBe(40);
        _undo.PerformRedo();
        Width(_screen, "S2").ShouldBe(70);
        HistoryCount.ShouldBe(1);
    }

    [Fact]
    public void UndoRedo_OfADefaultStateEdit_ShouldAlsoRereadRowsInACategorizedState()
    {
        // The reference sets the final value, so undo and redo re-read the rows of every state that reads the
        // restored source. Typing the edit leaves a categorized state's follower stale; that path is tracked
        // in #6886, and this pins the undo side.
        Arrange();
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        StateSave alt = new StateSave { Name = "Alt", ParentContainer = _comp };
        category.States.Add(alt);
        _comp.Categories.Add(category);
        AddRow(alt, "F1", "Width = Source.Width");
        SetWidth(alt, "F1", 50);
        _undo.RecordState();
        StateSave Alt() => _comp.Categories.Single().States.Single();

        EditSourceWidth(55);

        _undo.PerformUndo();
        Width(_comp, "F1", Alt()).ShouldBe(50);
        _undo.PerformRedo();
        Width(_comp, "F1", Alt()).ShouldBe(55, "the categorized follower follows the reference");
    }

    private StateSave AddCategorizedState(params (string Owner, string Row, float? Stored)[] followers)
    {
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        StateSave alt = new StateSave { Name = "Alt", ParentContainer = _comp };
        category.States.Add(alt);
        _comp.Categories.Add(category);
        foreach ((string owner, string row, float? stored) in followers)
        {
            AddRow(alt, owner, row);
            SetWidth(alt, owner, stored);
        }
        _undo.RecordState();
        return alt;
    }

    [Fact]
    public void TypedEdit_OfADefaultStateSource_ShouldRefreshFollowersInACategorizedState()
    {
        // #6886: the reference sets the final value, so a categorized state's row that reads the edited source
        // follows it, and so does a row in that state reading the first follower.
        Arrange();
        StateSave alt = AddCategorizedState(("F1", "Width = Source.Width", 50), ("F2", "Width = F1.Width + 1", 51));

        EditSourceWidth(55);

        Width(_comp, "F1", alt).ShouldBe(55, "the categorized follower reads the new source");
        Width(_comp, "F2", alt).ShouldBe(56, "a follower of that follower is refreshed in dependency order");
        Width(_comp, "F1").ShouldBe(55);
        MemoryShouldEqualDisk();
    }

    [Fact]
    public void TypedEdit_AndUndoRedo_ShouldGiveTheSameCategorizedFollowers()
    {
        // The typed edit is the oracle for undo and redo: after each step the categorized follower holds what
        // typing the same value gives.
        Arrange();
        AddCategorizedState(("F1", "Width = Source.Width", 50));
        // Undo replaces the state objects with copies, so each read looks the state up again.
        StateSave Alt() => _comp.Categories.Single().States.Single();
        EditSourceWidth(55);
        float? afterTyping = Width(_comp, "F1", Alt());

        _undo.PerformUndo();
        Width(_comp, "F1", Alt()).ShouldBe(50);
        _undo.PerformRedo();

        Width(_comp, "F1", Alt()).ShouldBe(afterTyping);
        Width(_comp, "F1", Alt()).ShouldBe(55);
    }

    [Fact]
    public void TypedEdit_ResettingTheSource_ShouldLeaveACategorizedFollowerAtItsLastValue()
    {
        Arrange();
        StateSave alt = AddCategorizedState(("F1", "Width = Source.Width", 50));
        EditSourceWidth(55);

        ResetSource();

        Width(_comp, "F1", alt).ShouldBe(55, "a reset source never resets a follower");
    }

    [Fact]
    public void TypedEdit_WithACycleInACategorizedState_ShouldFinishAndKeepTheCycleValues()
    {
        // F1 and F2 read each other in Alt: no order exists, so neither is evaluated and both keep their values.
        Arrange();
        StateSave alt = AddCategorizedState(("F1", "Width = F2.Width", 7), ("F2", "Width = F1.Width", 8));

        EditSourceWidth(55);

        Width(_comp, "F1", alt).ShouldBe(7);
        Width(_comp, "F2", alt).ShouldBe(8);
        Width(_comp, "F1").ShouldBe(55, "the default state's rows still follow");
    }

    private void ResetSource()
    {
        using (_undo.RequestLock())
        {
            _comp.DefaultState.Variables.RemoveAll(item => item.Name == "Source.Width");
            _logic.DoVariableReferenceReaction(_comp, _comp.GetInstance("Source"), "Width", _comp.DefaultState,
                "Source.Width", trySave: true);
        }
        _fileCommands.Object.TryAutoSaveElement(_comp);
    }

    // #6884: when a source is reset or a reference is removed, followers keep the value they hold. Undo and
    // redo leave them where a typed reset would, never at the snapshot's value.
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void UndoRedo_OfResettingAFeedingVariable_ShouldLeaveFollowersAtTheirHeldValues(bool materialized, bool autoSave)
    {
        Arrange(materialized, autoSave);
        ResetSource();
        Width(_comp, "Source").ShouldBeNull();

        _undo.PerformUndo();
        ShouldHaveWidths(50);
        MemoryShouldEqualDisk();

        _undo.PerformRedo();
        Width(_comp, "Source").ShouldBeNull();
        Width(_comp, "F1").ShouldBe(50, "F1 keeps the value it held");
        Width(_comp, "F2").ShouldBe(51);
        Width(_other, "O").ShouldBe(50, "a follower in another element keeps its value too");
        MemoryShouldEqualDisk();

        _undo.PerformUndo();
        ShouldHaveWidths(50);
        HistoryCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void UndoRedo_OfRemovingAReferenceRow_ShouldLeaveTheFollowerAtItsHeldValue(bool materialized, bool autoSave)
    {
        Arrange(materialized, autoSave);
        using (_undo.RequestLock())
        {
            _comp.DefaultState.GetVariableListSave("F1.VariableReferences")!.ValueAsIList.Clear();
            _logic.DoVariableReferenceReaction(_comp, _comp.GetInstance("F1"), "VariableReferences",
                _comp.DefaultState, "F1.VariableReferences", trySave: true);
        }
        _fileCommands.Object.TryAutoSaveElement(_comp);
        EditSourceWidth(55);
        Width(_comp, "F1").ShouldBe(materialized ? 50 : null, "no row, no follow");

        _undo.PerformUndo();
        _undo.PerformUndo();
        Width(_comp, "F1").ShouldBe(50, "the restored row reads Source");
        MemoryShouldEqualDisk();

        _undo.PerformRedo();
        Width(_comp, "F1").ShouldBe(50, "removing the row keeps the value");
        Width(_comp, "F2").ShouldBe(51);
        _undo.PerformRedo();
        Width(_comp, "Source").ShouldBe(55);
        if (materialized)
        {
            // Unmaterialized, F1 was never stored while the row existed, so no action changed it and the
            // snapshot's empty value is all history has to offer.
            Width(_comp, "F1").ShouldBe(50, "without its row F1 no longer follows");
        }
        Width(_other, "O").ShouldBe(55);
        MemoryShouldEqualDisk();
    }

    [Fact]
    public void UndoRedo_OfResettingTheMiddleOfAChain_ShouldKeepTheLastFollowersValue()
    {
        // F2 reads F1, which reads Source. Removing F1's row and value is the middle source going away.
        Arrange(materialized: false);
        using (_undo.RequestLock())
        {
            _comp.DefaultState.GetVariableListSave("F1.VariableReferences")!.ValueAsIList.Clear();
            _comp.DefaultState.Variables.RemoveAll(item => item.Name == "F1.Width");
            _logic.DoVariableReferenceReaction(_comp, _comp.GetInstance("F1"), "VariableReferences",
                _comp.DefaultState, "F1.VariableReferences", trySave: true);
        }

        _undo.PerformUndo();
        ShouldHaveWidths(50);

        _undo.PerformRedo();
        Width(_comp, "F1").ShouldBe(50, "F1 keeps the value it held; its removed row only stops it following");
        Width(_comp, "F2").ShouldBe(51, "F2 keeps the value it held");
        MemoryShouldEqualDisk();
    }

    [Fact]
    public void UndoRedo_OfRenamingAnInstanceAnotherElementReads_ShouldKeepRowsAndValuesCorrect()
    {
        Arrange();
        EditSourceWidth(55);
        InstanceSave source = _comp.GetInstance("Source")!;
        const string RenamedOtherRow = "Width = Components/Comp.Src.Width";

        source.Name = "Src";
        _rename.HandleRename(_comp, source, "Source", NameChangeAction.Rename, askAboutRename: false)
            .Succeeded.ShouldBeTrue();
        Rows(_other, "O").ShouldBe(new[] { RenamedOtherRow });

        _undo.PerformUndo();
        Rows(_other, "O").ShouldBe(new[] { OtherRow });
        _comp.GetInstance("Source").ShouldNotBeNull();
        ShouldHaveWidths(55);

        _undo.PerformRedo();
        Rows(_other, "O").ShouldBe(new[] { RenamedOtherRow });
        Width(_comp, "Src").ShouldBe(55);
        Width(_comp, "F1").ShouldBe(55);
        Width(_other, "O").ShouldBe(55);

        _undo.PerformUndo();
        _undo.PerformUndo();
        Rows(_other, "O").ShouldBe(new[] { OtherRow });
        ShouldHaveWidths(50);
        MemoryShouldEqualDisk();
    }

    [Fact]
    public void UndoRedo_OfRenamingAStateAnInstanceElsewhereUses_ShouldKeepTheInstancesVariableAndFollowers()
    {
        Arrange();
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        category.States.Add(new StateSave { Name = "Alt", ParentContainer = _comp });
        _comp.Categories.Add(category);
        _screen.DefaultState.Variables.Add(new VariableSave { Name = "C1.CatState", Type = "string", Value = "Alt", SetsValue = true });
        _undo.RecordState();

        using (_undo.RequestLock())
        {
            _rename.RenameState(category.States[0], category, "Alt2");
        }
        StateValue().ShouldBe("Alt2");

        _undo.PerformUndo();
        StateValue().ShouldBe("Alt");
        _comp.Categories.Single().States.Single().Name.ShouldBe("Alt");
        _undo.PerformRedo();
        StateValue().ShouldBe("Alt2");
        Width(_screen, "S2").ShouldBe(40);

        string? StateValue() => _screen.DefaultState.Variables.First(item => item.Name == "C1.CatState").Value as string;
    }

    private static string[] Rows(ElementSave element, string owner) =>
        element.DefaultState.GetVariableListSave(owner + ".VariableReferences")!.ValueAsIList.Cast<string>().ToArray();

    // Undoing a row add is typing the old value back: the follower the add changed returns to what it held before
    // the add and the row goes.
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void UndoRedo_OfAddingAReferenceRow_ShouldRestoreTheFollowerTheAddChanged(bool followerHadAValue, bool autoSave)
    {
        Arrange(materialized: true, autoSave);
        AddRectangleStandard();
        NewInstance(_comp, "F3", 0);
        NewInstance(_comp, "F4", 0);
        NewInstance(_other, "O3", 0);
        if (followerHadAValue)
        {
            SetWidth(_comp.DefaultState, "F3", 62);
        }
        AddRow(_comp.DefaultState, "F3");
        AddRow(_comp.DefaultState, "F4", "Width = F3.Width + 1");
        AddRow(_other.DefaultState, "O3", "Width = Components/Comp.F3.Width");
        ElementSaveExtensions.ApplyVariableReferences(_comp, _comp.DefaultState, null);
        ElementSaveExtensions.ApplyVariableReferences(_other, _other.DefaultState, null);
        _disk["Comp"] = FileManager.CloneSaveObject(_comp);
        _disk["Other"] = FileManager.CloneSaveObject(_other);
        _undo.RecordState();

        using (_undo.RequestLock())
        {
            _comp.DefaultState.GetVariableListSave("F3.VariableReferences")!.ValueAsIList.Add("Width = Source.Width + 5");
            _logic.DoVariableReferenceReaction(_comp, _comp.GetInstance("F3"), "VariableReferences",
                _comp.DefaultState, "F3.VariableReferences", trySave: true);
        }
        _fileCommands.Object.TryAutoSaveElement(_comp);

        void ShouldBeAfterTheAdd()
        {
            _comp.DefaultState.GetVariableListSave("F3.VariableReferences")!.ValueAsIList.Count.ShouldBe(1);
            Width(_comp, "F3").ShouldBe(55, "the add wrote F3");
            Width(_comp, "F4").ShouldBe(56, "the add reached F4");
            Width(_other, "O3").ShouldBe(55, "the add reached O3");
            MemoryShouldEqualDisk();
        }
        void ShouldBeBeforeTheAdd()
        {
            _comp.DefaultState.GetVariableListSave("F3.VariableReferences")!.ValueAsIList.Count.ShouldBe(0);
            Width(_comp, "F3").ShouldBe(followerHadAValue ? 62 : null, "F3 holds what it held before the add");
            Width(_comp, "F4").ShouldBe(followerHadAValue ? 63 : 1, "F4 holds what it held before the add (an empty F3 reads as 0)");
            Width(_other, "O3").ShouldBe(followerHadAValue ? 62 : 0, "O3 follows F3 (an empty F3 reads as 0)");
            MemoryShouldEqualDisk();
        }
        ShouldBeAfterTheAdd();

        _undo.PerformUndo();
        ShouldBeBeforeTheAdd();
        _undo.PerformRedo();
        ShouldBeAfterTheAdd();
        _undo.PerformUndo();
        ShouldBeBeforeTheAdd();
        HistoryCount.ShouldBe(1);
    }

    [Fact]
    public void UndoRedo_OfAddingAReferenceRowInACategorizedState_ShouldRestoreTheFollowerTheAddChanged()
    {
        Arrange();
        AddRectangleStandard();
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        category.States.Add(new StateSave { Name = "Alt", ParentContainer = _comp });
        _comp.Categories.Add(category);
        StateSave Alt() => _comp.Categories.Single().States.Single();
        NewInstance(_comp, "F3", 0);
        SetWidth(Alt(), "Source", 70);
        SetWidth(Alt(), "F3", 62);
        AddRow(Alt(), "F3");
        _selectedStateSave = Alt();
        _undo.RecordState();

        using (_undo.RequestLock())
        {
            Alt().GetVariableListSave("F3.VariableReferences")!.ValueAsIList.Add("Width = Source.Width + 5");
            _logic.DoVariableReferenceReaction(_comp, _comp.GetInstance("F3"), "VariableReferences",
                Alt(), "F3.VariableReferences", trySave: true);
        }
        Width(_comp, "F3", Alt()).ShouldBe(75, "the add wrote F3");

        _undo.PerformUndo();
        Width(_comp, "F3", Alt()).ShouldBe(62, "F3 holds what it held before the add");
        _undo.PerformRedo();
        Width(_comp, "F3", Alt()).ShouldBe(75);
    }

    [Fact]
    public void UndoRedo_OfAVariableNoRowReads_ShouldRestoreItAndAddOneEntry()
    {
        // Characterization: plain undo/redo of a non-referenced variable.
        Arrange();
        using (_undo.RequestLock())
        {
            SetWidth(_comp.DefaultState, "Source", 55);
            _comp.DefaultState.Variables.Add(new VariableSave { Name = "Source.X", Type = "float", Value = 5f, SetsValue = true });
        }
        HistoryCount.ShouldBe(1);
        _undo.PerformUndo();
        _comp.DefaultState.Variables.Any(item => item.Name == "Source.X").ShouldBeFalse();
        _undo.PerformRedo();
        _comp.DefaultState.Variables.First(item => item.Name == "Source.X").Value.ShouldBe(5f);
        HistoryCount.ShouldBe(1);
        _undo.CurrentElementHistory!.UndoIndex.ShouldBe(0);
    }

    [Fact]
    public void UndoRedo_OfAddingAnInstance_ShouldRemoveAndRestoreIt()
    {
        // Characterization: instance add/remove through undo.
        Arrange();
        using (_undo.RequestLock())
        {
            NewInstance(_comp, "Extra", 0);
        }
        _undo.PerformUndo();
        _comp.GetInstance("Extra").ShouldBeNull();
        _undo.PerformRedo();
        _comp.GetInstance("Extra").ShouldNotBeNull();
        HistoryCount.ShouldBe(1);
    }

    [Fact]
    public void Sweep_RandomEditUndoRedoSequences_ShouldAlwaysMatchWhatATypedEditGives()
    {
        int cases = SweepScale.Cases(300);
        Random random = new Random(6868);
        for (int caseIndex = 0; caseIndex < cases; caseIndex++)
        {
            UndoReferenceCascadeTests fresh = new UndoReferenceCascadeTests();
            try
            {
                fresh.RunSweepCase(random, caseIndex);
            }
            finally
            {
                fresh.Dispose();
            }
        }
    }

    private void RunSweepCase(Random random, int caseIndex)
    {
        bool materialized = random.Next(2) == 0;
        Arrange(materialized, autoSave: random.Next(2) == 0);
        float source = 50;
        List<float> applied = new List<float>();
        int cursor = 0;
        List<string> trace = new List<string>();
        for (int step = 0; step < 8; step++)
        {
            int action = random.Next(3);
            string where = $"case {caseIndex} step {step} action {action}, so far: {string.Join(", ", trace)}, cursor {cursor}, applied [{string.Join(",", applied)}], history {HistoryCount}";
            trace.Add(action == 0 ? "edit" : action == 1 ? "undo" : "redo");
            if (action == 0)
            {
                // An edit to the value the source already holds records nothing and keeps the redo history,
                // which the model below (an edit drops it) does not describe.
                float value = random.Next(1, 200);
                if (value == source)
                {
                    value++;
                }
                trace[trace.Count - 1] = "edit " + value;
                EditSourceWidth(value);
                applied.RemoveRange(cursor, applied.Count - cursor);
                applied.Add(value);
                cursor = applied.Count;
            }
            else if (action == 1 && cursor > 0)
            {
                cursor--;
                _undo.PerformUndo();
            }
            else if (action == 2 && cursor < applied.Count)
            {
                cursor++;
                _undo.PerformRedo();
            }
            source = cursor == 0 ? 50 : applied[cursor - 1];
            Width(_comp, "Source").ShouldBe(source, where);
            if (materialized || applied.Count > 0)
            {
                Width(_comp, "F1").ShouldBe(source, where);
                Width(_comp, "F2").ShouldBe(source + 1, where);
                Width(_other, "O").ShouldBe(source, where);
            }
            HistoryCount.ShouldBe(applied.Count, where);
            MemoryShouldEqualDisk();
        }
    }
}
