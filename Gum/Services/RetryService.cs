using System;
using System.Threading;

namespace Gum.Services;

/// <summary>
/// Retries an action that may transiently fail (for example, writing to disk), swallowing
/// intermediate exceptions and rethrowing only if every attempt fails.
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

/// <inheritdoc cref="IRetryService"/>
public class RetryService : IRetryService
{
    /// <inheritdoc/>
    public void TryMultipleTimes(Action action, int numberOfTimesToTry = 5)
    {
        const int msSleep = 200;
        int failureCount = 0;

        while (failureCount < numberOfTimesToTry)
        {
            try
            {
                action();
                break;
            }
            catch (Exception e)
            {
                failureCount++;
                Thread.Sleep(msSleep);
                if (failureCount >= numberOfTimesToTry)
                {
                    throw e;
                }
            }
        }
    }
}
