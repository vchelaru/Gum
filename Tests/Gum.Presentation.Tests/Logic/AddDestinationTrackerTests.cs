using CommunityToolkit.Mvvm.Messaging;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Messages;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests.Logic;

public class AddDestinationTrackerTests
{
    private readonly WeakReferenceMessenger _messenger = new();
    private readonly AddDestinationTracker _tracker;

    public AddDestinationTrackerTests()
    {
        _tracker = new AddDestinationTracker(_messenger);
    }

    [Fact]
    public void Anchor_AfterAnAddsOwnSelectionChange_RemembersDestinationAndClearsChangedFlag()
    {
        InstanceSave container = new InstanceSave { Name = "Container" };
        _messenger.Send(new SelectionChangedMessage());

        _tracker.Anchor(container);

        _tracker.Destination.ShouldBeSameAs(container);
        _tracker.HasSelectionChangedSinceAnchor.ShouldBeFalse();
    }

    [Fact]
    public void SelectionChanged_OutsideAdd_ForgetsDestinationAndMarksUserSelection()
    {
        InstanceSave container = new InstanceSave { Name = "Container" };
        _tracker.Anchor(container);

        _messenger.Send(new SelectionChangedMessage());

        _tracker.Destination.ShouldBeNull();
        _tracker.HasSelectionChangedSinceAnchor.ShouldBeTrue();
    }

    [Fact]
    public void Reset_AfterUserSelection_ClearsBothDestinationAndChangedFlag()
    {
        _tracker.Anchor(new InstanceSave());
        _messenger.Send(new SelectionChangedMessage());

        _tracker.Reset();

        _tracker.Destination.ShouldBeNull();
        _tracker.HasSelectionChangedSinceAnchor.ShouldBeFalse();
    }

    [Fact]
    public void MarkSelectionChanged_ForgetsDestinationAndMarksUserSelection()
    {
        _tracker.Anchor(new InstanceSave());

        _tracker.MarkSelectionChanged();

        _tracker.Destination.ShouldBeNull();
        _tracker.HasSelectionChangedSinceAnchor.ShouldBeTrue();
    }
}
