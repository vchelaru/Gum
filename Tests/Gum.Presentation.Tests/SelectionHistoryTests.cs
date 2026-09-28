using Gum.DataTypes;
using Gum.SelectionHistory;
using Gum.ToolStates;
using Moq;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

public class SelectionHistoryTests : BaseTestClass
{
    private readonly Mock<ISelectedState> _selectedState;
    private readonly SelectionHistoryService _selectionHistory;

    public SelectionHistoryTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _selectionHistory = new SelectionHistoryService(_selectedState.Object);
    }

    [Fact]
    public void CanNavigateBack_WhenNoHistory_IsFalse()
    {
        _selectionHistory.CanNavigateBack.ShouldBeFalse();
    }

    [Fact]
    public void NavigateBack_WhenNoHistory_DoesNotChangeSelectedState()
    {
        _selectionHistory.NavigateBack();

        _selectedState.VerifySet(x => x.SelectedInstance = It.IsAny<InstanceSave>(), Times.Never);
        _selectedState.VerifySet(x => x.SelectedElement = It.IsAny<ElementSave>(), Times.Never);
    }

    [Fact]
    public void NavigateBack_AfterTwoInstanceSelections_RestoresThePreviousInstance()
    {
        var elementA = new ScreenSave { Name = "ScreenA" };
        var instanceA = new InstanceSave { Name = "InstanceA" };
        var elementB = new ScreenSave { Name = "ScreenB" };
        var instanceB = new InstanceSave { Name = "InstanceB" };

        _selectionHistory.RecordSelection(elementA, instanceA);
        _selectionHistory.RecordSelection(elementB, instanceB);

        _selectionHistory.NavigateBack();

        _selectedState.VerifySet(x => x.SelectedInstance = instanceA, Times.Once);
    }

    [Fact]
    public void NavigateBack_ThenNavigateForward_RestoresTheLaterSelection()
    {
        var instanceA = new InstanceSave { Name = "InstanceA" };
        var instanceB = new InstanceSave { Name = "InstanceB" };
        var instanceC = new InstanceSave { Name = "InstanceC" };

        _selectionHistory.RecordSelection(null, instanceA);
        _selectionHistory.RecordSelection(null, instanceB);
        _selectionHistory.RecordSelection(null, instanceC);

        _selectionHistory.NavigateBack();
        _selectionHistory.NavigateBack();
        _selectionHistory.NavigateForward();

        // Set once by the first NavigateBack (C -> B) and again by the final NavigateForward (A -> B).
        _selectedState.VerifySet(x => x.SelectedInstance = instanceB, Times.Exactly(2));
    }

    [Fact]
    public void NavigateBack_ElementOnlySelection_RestoresSelectedElementNotInstance()
    {
        var elementA = new ScreenSave { Name = "ScreenA" };
        var elementB = new ScreenSave { Name = "ScreenB" };
        var instanceB = new InstanceSave { Name = "InstanceB" };

        _selectionHistory.RecordSelection(elementA, null);
        _selectionHistory.RecordSelection(elementB, instanceB);

        _selectionHistory.NavigateBack();

        _selectedState.VerifySet(x => x.SelectedElement = elementA, Times.Once);
        _selectedState.VerifySet(x => x.SelectedInstance = It.IsAny<InstanceSave>(), Times.Never);
    }

    [Fact]
    public void RecordSelection_SameAsCurrentEntry_DoesNotGrowHistory()
    {
        var instanceA = new InstanceSave { Name = "InstanceA" };
        var elementA = new ScreenSave { Name = "ScreenA" };

        _selectionHistory.RecordSelection(elementA, instanceA);
        _selectionHistory.RecordSelection(elementA, instanceA);

        _selectionHistory.CanNavigateBack.ShouldBeFalse();
    }

    [Fact]
    public void NavigatingBackAndForward_DoesNotRecordNewHistoryOrTruncateForwardStack()
    {
        var instanceA = new InstanceSave { Name = "InstanceA" };
        var instanceB = new InstanceSave { Name = "InstanceB" };
        var instanceC = new InstanceSave { Name = "InstanceC" };

        _selectionHistory.RecordSelection(null, instanceA);
        _selectionHistory.RecordSelection(null, instanceB);
        _selectionHistory.RecordSelection(null, instanceC);

        _selectionHistory.NavigateBack();

        _selectionHistory.CanNavigateBack.ShouldBeTrue();
        _selectionHistory.CanNavigateForward.ShouldBeTrue();
    }

    [Fact]
    public void RecordSelection_AfterNavigatingBack_TruncatesForwardHistory()
    {
        var instanceA = new InstanceSave { Name = "InstanceA" };
        var instanceB = new InstanceSave { Name = "InstanceB" };
        var instanceC = new InstanceSave { Name = "InstanceC" };
        var instanceD = new InstanceSave { Name = "InstanceD" };

        _selectionHistory.RecordSelection(null, instanceA);
        _selectionHistory.RecordSelection(null, instanceB);
        _selectionHistory.RecordSelection(null, instanceC);

        _selectionHistory.NavigateBack();
        _selectionHistory.NavigateBack();

        // A genuine new user selection made while parked mid-stack (at A).
        _selectionHistory.RecordSelection(null, instanceD);

        _selectionHistory.CanNavigateForward.ShouldBeFalse();
        _selectionHistory.CanNavigateBack.ShouldBeTrue();
    }

    [Fact]
    public void NavigateBack_PassesOverADeletedElementAndItsInstances()
    {
        var screenA = new ScreenSave { Name = "ScreenA" };
        var deleted = new ScreenSave { Name = "Deleted" };
        var deletedInstance = new InstanceSave { Name = "Child", ParentContainer = deleted };
        deleted.Instances.Add(deletedInstance);
        var screenC = new ScreenSave { Name = "ScreenC" };
        _selectionHistory.RecordSelection(screenA, null);
        _selectionHistory.RecordSelection(deleted, null);
        _selectionHistory.RecordSelection(deleted, deletedInstance);
        _selectionHistory.RecordSelection(screenC, null);

        _selectionHistory.ForgetElement(deleted);
        _selectionHistory.NavigateBack();

        _selectedState.VerifySet(x => x.SelectedElement = screenA, Times.Once);
        _selectedState.VerifySet(x => x.SelectedElement = deleted, Times.Never);
        _selectedState.VerifySet(x => x.SelectedInstance = It.IsAny<InstanceSave>(), Times.Never);
        _selectionHistory.CanNavigateBack.ShouldBeFalse();
    }

    [Theory]
    [InlineData(3)]
    [InlineData(2)]
    public void NavigateBack_AfterDeletingTheSelectedElement_GoesToTheOneSelectedBeforeIt(int visited)
    {
        // Deleting the selected element clears the selection before the history hears of it.
        List<ScreenSave> screens = Enumerable.Range(0, visited).Select(i => new ScreenSave { Name = $"Screen{i}" }).ToList();
        foreach (ScreenSave screen in screens)
        {
            _selectionHistory.RecordSelection(screen, null);
        }
        _selectionHistory.RecordSelection(null, null);

        _selectionHistory.ForgetElement(screens[^1]);

        _selectionHistory.CanNavigateBack.ShouldBeTrue();
        _selectionHistory.CanNavigateForward.ShouldBeFalse();
        _selectionHistory.NavigateBack();
        _selectedState.VerifySet(x => x.SelectedElement = screens[^2], Times.Once);
    }

    [Fact]
    public void RecordSelection_AfterDeletingTheSelectedElement_DropsWhatCameAfterIt()
    {
        var screenA = new ScreenSave { Name = "ScreenA" };
        var deleted = new ScreenSave { Name = "Deleted" };
        var screenC = new ScreenSave { Name = "ScreenC" };
        var screenD = new ScreenSave { Name = "ScreenD" };
        _selectionHistory.RecordSelection(screenA, null);
        _selectionHistory.RecordSelection(deleted, null);
        _selectionHistory.RecordSelection(screenC, null);
        _selectionHistory.NavigateBack();

        _selectionHistory.ForgetElement(deleted);
        _selectionHistory.RecordSelection(screenD, null);

        _selectionHistory.CanNavigateForward.ShouldBeFalse();
        _selectionHistory.NavigateBack();
        _selectedState.VerifySet(x => x.SelectedElement = screenA, Times.Once);
        _selectionHistory.CanNavigateBack.ShouldBeFalse();
    }

    [Fact]
    public void ForgetElement_BetweenTwoVisitsOfTheSameElement_LeavesNoStepThatChangesNothing()
    {
        var screenA = new ScreenSave { Name = "ScreenA" };
        var deleted = new ScreenSave { Name = "Deleted" };
        _selectionHistory.RecordSelection(screenA, null);
        _selectionHistory.RecordSelection(deleted, null);
        _selectionHistory.RecordSelection(screenA, null);

        _selectionHistory.ForgetElement(deleted);

        _selectionHistory.CanNavigateBack.ShouldBeFalse();
    }

    [Fact]
    public void NavigateForward_PassesOverADeletedInstance()
    {
        var screen = new ScreenSave { Name = "Screen" };
        var kept = new InstanceSave { Name = "Kept", ParentContainer = screen };
        var deleted = new InstanceSave { Name = "Deleted", ParentContainer = screen };
        screen.Instances.Add(kept);
        _selectionHistory.RecordSelection(screen, kept);
        _selectionHistory.RecordSelection(screen, deleted);
        _selectionHistory.RecordSelection(screen, null);
        _selectionHistory.NavigateBack();
        _selectionHistory.NavigateBack();
        _selectedState.Invocations.Clear();

        _selectionHistory.ForgetInstance(deleted);
        _selectionHistory.NavigateForward();

        _selectedState.VerifySet(x => x.SelectedInstance = deleted, Times.Never);
        _selectedState.VerifySet(x => x.SelectedElement = screen, Times.Once);
    }

    [Fact]
    public void RecordSelection_OfNothing_AddsNoStep()
    {
        var screenA = new ScreenSave { Name = "ScreenA" };
        var screenB = new ScreenSave { Name = "ScreenB" };
        _selectionHistory.RecordSelection(screenA, null);
        _selectionHistory.RecordSelection(null, null);
        _selectionHistory.RecordSelection(screenB, null);

        _selectionHistory.NavigateBack();

        _selectedState.VerifySet(x => x.SelectedElement = screenA, Times.Once);
        _selectedState.VerifySet(x => x.SelectedElement = null, Times.Never);
    }

    [Fact]
    public void NavigateBack_ToAnInstanceAnUndoReplaced_SelectsTheInstanceNowInItsPlace()
    {
        // Undo swaps an element's instances for copies, so the recorded object is no longer in it.
        var screen = new ScreenSave { Name = "Screen" };
        var original = new InstanceSave { Name = "Box", ParentContainer = screen };
        var copy = new InstanceSave { Name = "Box", ParentContainer = screen };
        screen.Instances.Add(copy);
        _selectionHistory.RecordSelection(screen, original);
        _selectionHistory.RecordSelection(screen, null);

        _selectionHistory.NavigateBack();

        _selectedState.VerifySet(x => x.SelectedInstance = copy, Times.Once);
        _selectedState.VerifySet(x => x.SelectedInstance = original, Times.Never);
    }
}
