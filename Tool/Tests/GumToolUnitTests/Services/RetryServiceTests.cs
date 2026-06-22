using System;
using Gum.Services;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Services;

public class RetryServiceTests
{
    [Fact]
    public void TryMultipleTimes_InvokesActionOnce_WhenItSucceedsImmediately()
    {
        RetryService retryService = new RetryService();
        int callCount = 0;

        retryService.TryMultipleTimes(() => callCount++);

        callCount.ShouldBe(1);
    }

    [Fact]
    public void TryMultipleTimes_Retries_WhenActionFailsThenSucceeds()
    {
        RetryService retryService = new RetryService();
        int callCount = 0;

        retryService.TryMultipleTimes(
            () =>
            {
                callCount++;
                if (callCount < 2)
                {
                    throw new InvalidOperationException("transient");
                }
            },
            numberOfTimesToTry: 5);

        callCount.ShouldBe(2);
    }

    [Fact]
    public void TryMultipleTimes_RethrowsLastException_WhenActionAlwaysFails()
    {
        RetryService retryService = new RetryService();
        int callCount = 0;

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            retryService.TryMultipleTimes(
                () =>
                {
                    callCount++;
                    throw new InvalidOperationException("always fails");
                },
                numberOfTimesToTry: 3));

        callCount.ShouldBe(3);
        exception.Message.ShouldBe("always fails");
    }
}
