namespace Gum.Services;

/// <summary>
/// Coalesces repeated refresh requests raised within the same synchronous burst into a single
/// deferred invocation.
/// </summary>
public interface IRefreshCoalescer
{
    /// <summary>
    /// Requests a refresh. If a refresh is already pending (a prior call has not yet run), this is a
    /// no-op; otherwise the refresh action is posted via <see cref="IDispatcher"/> to run once.
    /// </summary>
    void RequestRefresh();
}
