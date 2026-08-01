using System;

namespace Gum.Services;

/// <inheritdoc cref="IRefreshCoalescer"/>
public class RefreshCoalescer : IRefreshCoalescer
{
    private readonly IDispatcher _dispatcher;
    private readonly Action _refresh;
    private bool _isPending;

    public RefreshCoalescer(IDispatcher dispatcher, Action refresh)
    {
        _dispatcher = dispatcher;
        _refresh = refresh;
    }

    public void RequestRefresh()
    {
        if (_isPending)
        {
            return;
        }

        _isPending = true;
        _dispatcher.Post(() =>
        {
            _isPending = false;
            _refresh();
        });
    }
}
