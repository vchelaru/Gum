using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Diagnostics;
using Gum.Diagnostics;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>Exceptions on the UI thread reach the crash reporter and do not end the app.</summary>
public class UnhandledExceptionHooksTests
{
    [AvaloniaFact]
    public void DispatcherException_IsReportedAsRecoverable_AndHandled()
    {
        Mock<ICrashReporter> reporter = new Mock<ICrashReporter>();
        InvalidOperationException thrown = new InvalidOperationException("from a posted job");
        using IDisposable hooks = UnhandledExceptionHooks.InstallDispatcherHook(reporter.Object);

        Dispatcher.UIThread.Post(() => throw thrown);
        Dispatcher.UIThread.RunJobs();

        reporter.Verify(x => x.ReportRecoverable(thrown, It.IsAny<string>()), Times.Once);
    }

    // An async void handler rethrows on the synchronization context it started on, which on the
    // UI thread is the dispatcher, so the same hook covers every async void event handler.
    [AvaloniaFact]
    public void AsyncVoidHandlerException_IsReportedAsRecoverable()
    {
        Mock<ICrashReporter> reporter = new Mock<ICrashReporter>();
        InvalidOperationException thrown = new InvalidOperationException("from async void");
        using IDisposable hooks = UnhandledExceptionHooks.InstallDispatcherHook(reporter.Object);

        bool reported = false;
        reporter.Setup(x => x.ReportRecoverable(thrown, It.IsAny<string>())).Callback(() => reported = true);

        // The real UI thread runs on Avalonia's context; the headless test runner installs xunit's.
        SynchronizationContext? testContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
        try
        {
            ThrowAfterAwait(thrown);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(testContext);
        }
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (!reported && DateTime.UtcNow < deadline)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }

        reporter.Verify(x => x.ReportRecoverable(thrown, It.IsAny<string>()), Times.Once);
    }

    private static async void ThrowAfterAwait(Exception exception)
    {
        await Task.Delay(10).ConfigureAwait(false);
        throw exception;
    }
}
