using System.Collections.Concurrent;
using System.Reflection;
using Xunit.Runner.InProc.SystemConsole;
using Xunit.Sdk;
using Xunit.v3;

[assembly: TestFramework(typeof(MonoGameGum.IntegrationTests.MainThreadTestFramework))]

namespace MonoGameGum.IntegrationTests;

// Every test here creates a MonoGame window. macOS only allows that on the process main thread,
// and xunit runs tests on pool threads. So Main runs the xunit runner on a background thread and
// spends the main thread pumping MainThreadSynchronizationContext, and MainThreadTestFramework
// moves test execution onto that context. xunit's execution pipeline keeps its continuations on
// the current SynchronizationContext, so each test's constructor, body, and Dispose run on the
// main thread. Harmless on Windows and Linux, so every OS takes the same path.
public static class Program
{
    public static int Main(string[] args)
    {
        MainThreadSynchronizationContext context = MainThreadSynchronizationContext.Instance;

        Task<int> runnerTask = Task.Run(() => ConsoleRunner.Run(args));
        runnerTask.ContinueWith(_ => context.Complete(), TaskScheduler.Default);

        context.RunOnCurrentThread();
        return runnerTask.GetAwaiter().GetResult();
    }
}

public class MainThreadTestFramework : XunitTestFramework
{
    protected override ITestFrameworkExecutor CreateExecutor(Assembly assembly) =>
        new MainThreadTestFrameworkExecutor(
            new XunitTestAssembly(assembly, configFileName: null, assembly.GetName().Version));
}

public class MainThreadTestFrameworkExecutor : XunitTestFrameworkExecutor
{
    public MainThreadTestFrameworkExecutor(IXunitTestAssembly testAssembly) : base(testAssembly)
    {
    }

    public override ValueTask RunTestCases(
        IReadOnlyCollection<IXunitTestCase> testCases,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions,
        CancellationToken cancellationToken)
    {
        TaskCompletionSource completion = new TaskCompletionSource();

        MainThreadSynchronizationContext.Instance.Post(async _ =>
        {
            try
            {
                await base.RunTestCases(testCases, executionMessageSink, executionOptions, cancellationToken);
                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }, null);

        return new ValueTask(completion.Task);
    }
}

public sealed class MainThreadSynchronizationContext : SynchronizationContext
{
    public static MainThreadSynchronizationContext Instance { get; } = new MainThreadSynchronizationContext();

    private readonly BlockingCollection<Action> _queue;

    private MainThreadSynchronizationContext()
    {
        _queue = new BlockingCollection<Action>();
    }

    // xunit keeps the current test in AsyncLocal state, so each work item must run in the
    // ExecutionContext of the code that posted it.
    public override void Post(SendOrPostCallback d, object? state)
    {
        ExecutionContext? executionContext = ExecutionContext.Capture();
        if (executionContext == null)
        {
            _queue.Add(() => d(state));
        }
        else
        {
            _queue.Add(() => ExecutionContext.Run(executionContext, s => d(s), state));
        }
    }

    public override void Send(SendOrPostCallback d, object? state) =>
        throw new NotSupportedException("Send would deadlock when called from the main thread.");

    public override SynchronizationContext CreateCopy() => this;

    public void Complete() => _queue.CompleteAdding();

    public void RunOnCurrentThread()
    {
        SetSynchronizationContext(this);
        foreach (Action workItem in _queue.GetConsumingEnumerable())
        {
            workItem();
        }
    }
}
