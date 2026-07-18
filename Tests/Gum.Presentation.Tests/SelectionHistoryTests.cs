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
}
