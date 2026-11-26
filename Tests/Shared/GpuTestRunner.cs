using System.Runtime.ExceptionServices;

namespace GumTests.Shared;

/// <summary>
/// Runs a test that needs a real GPU context (a GL window, a Vulkan device) the same way in every host's tests:
/// on its own thread with a watchdog, so a stuck driver fails the test instead of hanging the job, and with
/// "no context" a skip on a developer machine but a failure on the CI streams that set <c>requireVariable=1</c>.
/// </summary>
internal static class GpuTestRunner
{
    /// <summary>Thrown by setup code when the machine has no context to give; <see cref="Run"/> skips or fails on it.</summary>
    public sealed class NoContextException : Exception
    {
        public NoContextException(string message) : base(message) { }
    }

    /// <summary>
    /// Runs <paramref name="body"/> on a worker thread. A <see cref="NoContextException"/> from it skips the test, or
    /// fails it when the environment variable <paramref name="requireVariable"/> is "1". Any other exception is rethrown.
    /// </summary>
    public static void Run(string what, string requireVariable, Action body, int watchdogSeconds = 60)
    {
        Exception? failure = null;
        Thread worker = new Thread(() =>
        {
            try { body(); }
            catch (Exception e) { failure = e; }
        }) { IsBackground = true };
        worker.Start();
        if (!worker.Join(TimeSpan.FromSeconds(watchdogSeconds)))
        {
            throw new TimeoutException($"The {what} test did not finish within {watchdogSeconds} seconds.");
        }
        if (failure is NoContextException noContext)
        {
            if (Environment.GetEnvironmentVariable(requireVariable) == "1")
            {
                throw new InvalidOperationException($"{noContext.Message} ({requireVariable}=1)");
            }
            Skip.If(true, noContext.Message);
        }
        if (failure != null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
