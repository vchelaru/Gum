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
    public void MarkSelectionUnchanged_KeepsDestinationAndClearsChangedFlag()
    {
        // A copy re-anchors on the current selection but is not a click on a node, so the container
        // the user last picked still receives the next click-add.
        InstanceSave container = new InstanceSave { Name = "Container" };
        _tracker.Anchor(container);

        _tracker.MarkSelectionUnchanged();

        _tracker.Destination.ShouldBeSameAs(container);
        _tracker.HasSelectionChangedSinceAnchor.ShouldBeFalse();

        _tracker.MarkSelectionChanged();
        _tracker.MarkSelectionUnchanged();

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
