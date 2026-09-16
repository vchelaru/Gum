using System;

namespace Gum.Avalonia.Canvas;

/// <summary>
/// A lazily-created shared instance that retries instead of caching a failed attempt: if
/// <c>initialize</c> throws, the half-built instance is disposed and discarded so the next
/// <see cref="GetOrCreate"/> call builds a fresh one rather than handing out one whose init never
/// completed (#4786). Not thread-safe; callers serialize access themselves.
/// </summary>
internal sealed class RetryingSharedInstance<T> where T : class, IDisposable
{
    private T? _instance;

    public T GetOrCreate(Func<T> factory, Action<T> initialize)
    {
        if (_instance == null)
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
        }
        return _instance;
    }

    public void Clear() => _instance = null;
}
