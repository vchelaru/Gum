using CommunityToolkit.Mvvm.Messaging;
using Gum.Messages;

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
    public void Anchor(object? destination)
    {
        _destination = destination;
        _hasSelectionChangedSinceAnchor = false;
    }

    /// <inheritdoc/>
    public void MarkSelectionUnchanged()
    {
        _hasSelectionChangedSinceAnchor = false;
    }

    /// <inheritdoc/>
    public void MarkSelectionChanged()
    {
        _destination = null;
        _hasSelectionChangedSinceAnchor = true;
    }
}
