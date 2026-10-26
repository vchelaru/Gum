using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// <see cref="IUndoManager.RebaseStoredValues"/> with the real UndoManager: a value written outside an
/// undoable edit (a reference re-evaluated after a reload from disk) goes into the snapshots that have the
/// reference row feeding it, and into no others.
/// </summary>
public class RebaseStoredValuesTests : BaseTestClass
{
    private readonly UndoManager _undoManager;
    private readonly ComponentSave _reader = new() { Name = "Reader" };
    private readonly ComponentSave _other = new() { Name = "Other" };
    private ComponentSave _selected;

    public RebaseStoredValuesTests()
    {
        _reader.States.Add(new StateSave { Name = "Default", ParentContainer = _reader });
        _other.States.Add(new StateSave { Name = "Default", ParentContainer = _other });
        _selected = _reader;
        Mock<ISelectedState> selectedState = new();
        selectedState.Setup(x => x.SelectedElement).Returns(() => _selected);
        selectedState.Setup(x => x.SelectedComponent).Returns(() => _selected);
        selectedState.Setup(x => x.SelectedStateSave).Returns(() => _selected.DefaultState);
        Mock<IReferenceFinderProjectProvider> projectProvider = new();
        projectProvider.Setup(x => x.GumProjectSave).Returns(new GumProjectSave());
        _undoManager = new UndoManager(selectedState.Object, new Mock<IUndoRenameLogic>().Object,
            new Mock<IGuiCommands>().Object, new Mock<IFileCommands>().Object, new Mock<IMessenger>().Object,
            new Mock<IUndoPluginNotifier>().Object, new Mock<IAnimationUndoProvider>().Object, projectProvider.Object);
    }

    private static void AddRow(ComponentSave element)
    {
        element.DefaultState.VariableLists.Add(new VariableListSave<string>
        {
            Name = "Background.VariableReferences",
            Type = "string",
            Value = new List<string> { "Red = Components/Styles.Primary.FillRed" },
        });
    }

    private static void SetValue(ComponentSave element, string name, object? value)
    {
        VariableSave? variable = element.DefaultState.Variables.FirstOrDefault(item => item.Name == name);
        if (variable == null)
        {
            variable = new VariableSave { Name = name, Type = value is int ? "int" : "float", SetsValue = true };
            element.DefaultState.Variables.Add(variable);
        }
        variable.Value = value;
    }

    private static object? Value(ComponentSave element, string name) =>
        element.DefaultState.Variables.FirstOrDefault(item => item.Name == name)?.Value;

    private StoredValueChange RedChange(ComponentSave element, int value) =>
        new StoredValueChange(element, null, "Default", "Background.Red", "int", value);

    [Fact]
    public void RebaseStoredValues_ShouldKeepTheNewValue_WhenAnEarlierEditIsUndone()
    {
        AddRow(_reader);
        SetValue(_reader, "Background.Red", 21);
        _undoManager.RecordState();
        SetValue(_reader, "Width", 80f);
        _undoManager.RecordUndo();

        SetValue(_reader, "Background.Red", 200);
        _undoManager.RebaseStoredValues(new[] { RedChange(_reader, 200) });
        _undoManager.PerformUndo();

        Value(_reader, "Width").ShouldBeNull();
        Value(_reader, "Background.Red").ShouldBe(200);
        _undoManager.PerformRedo();
        Value(_reader, "Width").ShouldBe(80f);
        Value(_reader, "Background.Red").ShouldBe(200);
    }

    [Fact]
    public void RebaseStoredValues_ShouldNotAddTheValue_ToASnapshotTakenBeforeTheReferenceRowExisted()
    {
        _undoManager.RecordState();
        AddRow(_reader);
        SetValue(_reader, "Background.Red", 21);
        _undoManager.RecordUndo();

        SetValue(_reader, "Background.Red", 200);
        _undoManager.RebaseStoredValues(new[] { RedChange(_reader, 200) });
        _undoManager.PerformUndo();

        // As without a reload: undoing the row removes the row and the value it produced.
        _reader.DefaultState.VariableLists.ShouldBeEmpty();
        Value(_reader, "Background.Red").ShouldBeNull();
    }

    [Fact]
    public void RebaseStoredValues_ShouldLeaveAnotherElementsSnapshotsAlone()
    {
        _selected = _other;
        AddRow(_other);
        SetValue(_other, "Background.Red", 21);
        _undoManager.RecordState();
        SetValue(_other, "Width", 80f);
        _undoManager.RecordUndo();

        // The change names the reader; Other's history must still restore Other's own value.
        _undoManager.RebaseStoredValues(new[] { RedChange(_reader, 200) });
        _undoManager.PerformUndo();

        Value(_other, "Background.Red").ShouldBe(21);
    }
}
