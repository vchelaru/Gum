using System;

namespace Gum.Services;

/// <summary>
/// Retries an action that may transiently fail (for example, writing to disk), swallowing
/// intermediate exceptions and rethrowing only if every attempt fails. See
/// <see cref="Gum.Services.RetryService"/> for the concrete implementation (tool project).
/// </summary>
public interface IRetryService
{
    /// <summary>
    /// Performs the argument action multiple times, swallowing exceptions every time except the last.
    /// This can be used to perform operations which may fail from time to time, like writing to disk.
    /// </summary>
    /// <param name="action">The action to perform.</param>
    /// <param name="numberOfTimesToTry">The number of times to try.</param>
    void TryMultipleTimes(Action action, int numberOfTimesToTry = 5);
}
