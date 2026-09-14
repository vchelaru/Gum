using System;
using Avalonia.Threading;
using IDispatcher = Gum.Services.IDispatcher;

namespace Gum.Avalonia.Services;

/// <inheritdoc cref="IDispatcher"/>
public class AvaloniaDispatcher : IDispatcher
{
    /// <inheritdoc/>
    public void Invoke(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Invoke(action);
        }
    }

    /// <inheritdoc/>
    public void Post(Action action) => Dispatcher.UIThread.Post(action, DispatcherPriority.Background);
}
