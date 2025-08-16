using System;
using System.Windows.Threading;

namespace Gum.Services;

public class AppDispatcher : IDispatcher
{
    private Lazy<Dispatcher> Dispatcher { get; }
    
    public AppDispatcher(Func<Dispatcher> dispatcher)
    {
        Dispatcher = new(dispatcher);
    }

    public void Invoke(Action action) => Dispatcher.Value.Invoke(action);
    public void Post(Action action) => Dispatcher.Value.BeginInvoke(action, DispatcherPriority.Background);
}