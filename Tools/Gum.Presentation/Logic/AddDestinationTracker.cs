using CommunityToolkit.Mvvm.Messaging;
using Gum.Messages;
using System;

namespace Gum.Logic;

/// <inheritdoc cref="IAddDestinationTracker"/>
public class AddDestinationTracker : IAddDestinationTracker
{
    private object? _destination;
    private bool _hasSelectionChangedSinceAnchor;

    public AddDestinationTracker(IMessenger messenger)
    {
        messenger.Register<SelectionChangedMessage>(this, (_, _) => MarkSelectionChanged());
    }

    /// <inheritdoc/>
    public object? Destination => _destination;

    /// <inheritdoc/>
    public bool HasSelectionChangedSinceAnchor => _hasSelectionChangedSinceAnchor;

    /// <inheritdoc/>
    public void RunAdd(object? destination, Action add)
    {
        // Anchoring after the add is what keeps the add's own selection change from counting.
        add();

        _destination = destination;
        _hasSelectionChangedSinceAnchor = false;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _destination = null;
        _hasSelectionChangedSinceAnchor = false;
    }

    /// <inheritdoc/>
    public void MarkSelectionChanged()
    {
        _destination = null;
        _hasSelectionChangedSinceAnchor = true;
    }
}
