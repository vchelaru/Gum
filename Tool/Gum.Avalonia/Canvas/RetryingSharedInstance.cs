using System;

namespace Gum.Avalonia.Canvas;

/// <summary>
/// A lazily-created shared instance that retries instead of caching a failed attempt: if
/// <c>initialize</c> throws, the half-built instance is disposed and discarded so the next
/// <see cref="GetOrCreate"/> call builds a fresh one rather than handing out one whose init never
/// completed (#4786). Not thread-safe; callers serialize access themselves.
/// </summary>
/// <remarks>
/// Creation can pump the UI dispatcher (KNI's device creation does), so a dispatcher callback can
/// call <see cref="GetOrCreate"/> again on the same thread before the first call returns. That
/// nested call throws: a second instance built then would be overwritten by the outer call and
/// leaked. Callers that can wait check <see cref="IsCreating"/> first and try again later.
/// </remarks>
internal sealed class RetryingSharedInstance<T> where T : class, IDisposable
{
    private T? _instance;

    /// <summary>Whether a <see cref="GetOrCreate"/> call is still building the instance.</summary>
    public bool IsCreating { get; private set; }

    public T GetOrCreate(Func<T> factory, Action<T> initialize)
    {
        if (_instance != null)
        {
            return _instance;
        }
        if (IsCreating)
        {
            throw new InvalidOperationException(
                $"{typeof(T).Name} was requested again while it was still being created.");
        }

        IsCreating = true;
        try
        {
            T candidate = factory();
            try
            {
                initialize(candidate);
            }
            catch
            {
                candidate.Dispose();
                throw;
            }
            _instance = candidate;
            return candidate;
        }
        finally
        {
            IsCreating = false;
        }
    }
}
