using System;
using System.Threading;

namespace Gum.Services;

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
