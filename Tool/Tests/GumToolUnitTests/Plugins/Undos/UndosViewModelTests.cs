using Gum.Plugins.Undos;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.Collections.Generic;

namespace GumToolUnitTests.Plugins.Undos;

public class UndosViewModelTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IUndoManager> _undoManager;
    private readonly UndosViewModel _viewModel;

    public UndosViewModelTests()
    {
        _mocker = new AutoMocker();
        _selectedState = _mocker.GetMock<ISelectedState>();
        _undoManager = _mocker.GetMock<IUndoManager>();
        _viewModel = _mocker.CreateInstance<UndosViewModel>();
    }

    [Fact]
    public void Constructor_ShouldSubscribeToUndosChanged_SoRaisingItNotifiesHistoryItemsAndUndoIndex()
    {
        List<string?> raisedPropertyNames = new List<string?>();
        _viewModel.PropertyChanged += (sender, args) => raisedPropertyNames.Add(args.PropertyName);

        _undoManager.Raise(
            undoManager => undoManager.UndosChanged += null,
            _undoManager.Object,
            new UndoOperationEventArgs { Operation = UndoOperation.EntireHistoryChange });

        raisedPropertyNames.ShouldContain(nameof(UndosViewModel.HistoryItems));
        raisedPropertyNames.ShouldContain(nameof(UndosViewModel.UndoIndex));
    }

    [Fact]
    public void UndoIndex_ShouldReflectInjectedUndoManagersCurrentElementHistory()
    {
        _selectedState.Setup(s => s.SelectedBehavior).Returns((Gum.DataTypes.Behaviors.BehaviorSave?)null);
        _undoManager.Setup(u => u.CurrentElementHistory).Returns(new ElementHistory { UndoIndex = 3 });

        _viewModel.UndoIndex.ShouldBe(3);
    }
}
