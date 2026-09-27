using Avalonia.Headless;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Runs graphics device test code on the headless Avalonia UI thread, which is where the head
/// creates the shared KNI GL device and the only thread that device may be used from. The wait
/// is bounded: a native SDL or GL call that never returns fails the test with a message instead
/// of hanging the whole run (#5259).
/// </summary>
internal static class DeviceTestThread
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    public static void Run(Action action)
    {
        Task dispatched = HeadlessUnitTestSession.GetOrStartForAssembly(typeof(DeviceTestThread).Assembly)
            .Dispatch(action, CancellationToken.None);
        Task finished = Task.WhenAny(dispatched, Task.Delay(Timeout)).GetAwaiter().GetResult();
        if (finished != dispatched)
        {
            throw new TimeoutException(
                $"Device test code did not finish on the UI thread within {Timeout.TotalMinutes} minutes. " +
                "A native SDL or GL call is likely stuck; the UI thread stays blocked, so later tests that use it fail or hang too.");
        }
        dispatched.GetAwaiter().GetResult();
    }
}
