using Gum.Plugins.Undos;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for UndosViewModel, relocated out of Gum.csproj into the
/// headless Gum.Presentation assembly (ADR-0005, #3754) as a clean leaf VM whose two injected
/// interfaces (ISelectedState, IUndoManager) are both already headless.
/// </summary>
public class UndosViewModelTests : BaseTestClass
{
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IUndoManager> _undoManager;
    private readonly UndosViewModel _viewModel;

    public UndosViewModelTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _undoManager = new Mock<IUndoManager>();
        _viewModel = new UndosViewModel(_selectedState.Object, _undoManager.Object);
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
