using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using Gum.Diagnostics;

namespace Gum.Avalonia.Diagnostics;

/// <summary>
/// Routes the process's unhandled-exception paths to an <see cref="ICrashReporter"/>: the UI
/// thread's dispatcher (which also receives <c>async void</c> handlers started on it), unobserved
/// faulted tasks, and the AppDomain's last-chance handler for everything else.
/// </summary>
/// <remarks>
/// Input handlers run from the dispatcher, so their exceptions are survivable. A handler Avalonia
/// runs straight from the Windows window procedure (<c>Window.Activated</c>, for one) bypasses the
/// dispatcher and ends the process; it is still logged, as fatal.
/// </remarks>
public static class UnhandledExceptionHooks
{
    /// <summary>
    /// Subscribes the process-wide hooks. Call first thing in <c>Main</c>. It must not touch
    /// <see cref="Dispatcher.UIThread"/>: reading it before Avalonia's platform is set up creates a
    /// dispatcher with no main loop. Dispose to unsubscribe.
    /// </summary>
    public static IDisposable InstallProcessHooks(ICrashReporter reporter)
    {
        EventHandler<UnobservedTaskExceptionEventArgs> onUnobservedTask = (_, e) =>
        {
            reporter.ReportRecoverable(e.Exception, "Unobserved task");
            e.SetObserved();
        };
        UnhandledExceptionEventHandler onAppDomain = (_, e) =>
        {
            Exception exception = e.ExceptionObject as Exception
                ?? new Exception("Non-exception object thrown: " + e.ExceptionObject);
            if (e.IsTerminating)
            {
                reporter.ReportFatal(exception, "Unhandled exception");
            }
            else
            {
                reporter.ReportRecoverable(exception, "Unhandled exception");
            }
        };

        TaskScheduler.UnobservedTaskException += onUnobservedTask;
        AppDomain.CurrentDomain.UnhandledException += onAppDomain;

        return new Subscription(() =>
        {
            TaskScheduler.UnobservedTaskException -= onUnobservedTask;
            AppDomain.CurrentDomain.UnhandledException -= onAppDomain;
        });
    }

    /// <summary>
    /// Subscribes the UI thread's hook, once Avalonia's platform is set up. Its exceptions are
    /// marked handled, so Gum keeps running. Dispose to unsubscribe.
    /// </summary>
    public static IDisposable InstallDispatcherHook(ICrashReporter reporter)
    {
        DispatcherUnhandledExceptionEventHandler onDispatcher = (_, e) =>
        {
            reporter.ReportRecoverable(e.Exception, "UI thread");
            e.Handled = true;
        };

        Dispatcher.UIThread.UnhandledException += onDispatcher;

        return new Subscription(() => Dispatcher.UIThread.UnhandledException -= onDispatcher);
    }

    private sealed class Subscription : IDisposable
    {
        private Action? _unsubscribe;

        public Subscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            _unsubscribe?.Invoke();
            _unsubscribe = null;
        }
    }
}
