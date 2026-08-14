using System;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pastes against the real <see cref="UndoManager"/> rather than a mock, because the whole point of the
/// single lock the service takes is how it interacts with undo recording. A mock cannot show that.
/// </summary>
public class VariableCategoryCopyPasteUndoTests : BaseTestClass
{
    private readonly ComponentSave _component;
    private readonly UndoManager _undoManager;

    /// <summary>A row backed by a real <see cref="StateSave"/>, so an undo snapshot actually sees the write.</summary>
    private class StateBackedRow : IVariableCategoryRow
    {
        private readonly StateSave _state;

        public StateBackedRow(StateSave state, string rootVariableName)
        {
            _state = state;
            RootVariableName = rootVariableName;
        }

        public string RootVariableName { get; }

        public bool IsReadOnly => false;

        public bool IsAssignedByReference => false;

        public bool IsIndeterminate => false;

        public object? Value => _state.GetValue(RootVariableName);

        public Type? ValueType => Value?.GetType();

        public bool TrySetValue(object value)
        {
            _state.SetValue(RootVariableName, value);
            return true;
        }
    }

    private sealed class NoAnimationUndoProvider : IAnimationUndoProvider
    {
        public ElementAnimationsSave? GetCurrentAnimations(ElementSave element) => null;

        public void ApplyAnimations(ElementSave element, ElementAnimationsSave animations)
        {
        }
    }

    public VariableCategoryCopyPasteUndoTests()
    {
        _component = new ComponentSave();
        _component.States.Add(new StateSave { Name = "Default" });

        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.Setup(item => item.SelectedElement).Returns(_component);
        selectedState.Setup(item => item.SelectedComponent).Returns(_component);
        selectedState.Setup(item => item.SelectedStateSave).Returns(_component.DefaultState);

        _undoManager = new UndoManager(
            selectedState.Object,
            new Mock<IUndoRenameLogic>().Object,
            new Mock<IGuiCommands>().Object,
            new Mock<IFileCommands>().Object,
            new Mock<IMessenger>().Object,
            new Mock<IUndoPluginNotifier>().Object,
            new NoAnimationUndoProvider());
    }

    /// <summary>
    /// A pasted group must undo as one action. If each write recorded its own undo, a single
    /// <see cref="UndoManager.PerformUndo"/> would roll back only the last variable and leave the rest
    /// of the group applied.
    /// </summary>
    [Fact]
    public void PerformUndo_AfterAPaste_ShouldRestoreEveryVariableInTheGroupAtOnce()
    {
        StateSave sourceState = new StateSave { Name = "Source" };
        sourceState.SetValue("FontSize", 36);
        sourceState.SetValue("IsBold", true);
        sourceState.SetValue("OutlineThickness", 4);

        _component.DefaultState.SetValue("FontSize", 12);
        _component.DefaultState.SetValue("IsBold", false);
        _component.DefaultState.SetValue("OutlineThickness", 0);

        _undoManager.RecordState();

        VariableCategoryCopyPasteService service = new VariableCategoryCopyPasteService(_undoManager);
        service.Copy("Font", new IVariableCategoryRow[]
        {
            new StateBackedRow(sourceState, "FontSize"),
            new StateBackedRow(sourceState, "IsBold"),
            new StateBackedRow(sourceState, "OutlineThickness")
        });
        service.Paste(new IVariableCategoryRow[]
        {
            new StateBackedRow(_component.DefaultState, "FontSize"),
            new StateBackedRow(_component.DefaultState, "IsBold"),
            new StateBackedRow(_component.DefaultState, "OutlineThickness")
        });

        _component.DefaultState.GetValueOrDefault<int>("FontSize").ShouldBe(36);
        _component.DefaultState.GetValueOrDefault<bool>("IsBold").ShouldBe(true);
        _component.DefaultState.GetValueOrDefault<int>("OutlineThickness").ShouldBe(4);

        _undoManager.PerformUndo();

        _component.DefaultState.GetValueOrDefault<int>("FontSize").ShouldBe(12);
        _component.DefaultState.GetValueOrDefault<bool>("IsBold").ShouldBe(false);
        _component.DefaultState.GetValueOrDefault<int>("OutlineThickness").ShouldBe(0);
    }
}
