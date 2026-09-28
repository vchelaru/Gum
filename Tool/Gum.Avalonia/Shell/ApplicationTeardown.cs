using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging;

namespace Gum.Avalonia.Shell;

/// <summary>
/// The tool's exit work: sends <see cref="ApplicationTeardownMessage"/> and runs what its
/// recipients queued (saving the layout and window placement), in order.
/// </summary>
public sealed class ApplicationTeardown
{
    private readonly IMessenger _messenger;

    /// <summary>Creates the teardown over the app's messenger.</summary>
    public ApplicationTeardown(IMessenger messenger)
    {
        _messenger = messenger;
    }

    /// <summary>Runs the exit work. The head calls this once, when the desktop lifetime exits.</summary>
    public void Run()
    {
        List<Action> teardownActions = new List<Action>();
        _messenger.Send(new ApplicationTeardownMessage(teardownActions));
        foreach (Action action in teardownActions)
        {
            action();
        }
    }
}
