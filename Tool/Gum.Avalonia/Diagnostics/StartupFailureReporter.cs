using System;
using System.IO;

namespace Gum.Avalonia.Diagnostics;

/// <summary>
/// Reports an exception thrown by the tool's startup. It always goes to stderr, so an unattended
/// run and <c>HeadProcessTests</c> can see it. It also goes to the main window, unless an
/// unattended (<c>--exit-after</c>) run has already given up and is closing that window.
/// </summary>
public sealed class StartupFailureReporter
{
    private readonly Action<Exception> _showInWindow;
    private readonly TextWriter _error;
    private bool _isUnattendedExitStarted;

    /// <summary>Creates a reporter.</summary>
    /// <param name="showInWindow">Shows the failure in the main window.</param>
    /// <param name="error">Where the failure is written, normally stderr.</param>
    public StartupFailureReporter(Action<Exception> showInWindow, TextWriter error)
    {
        _showInWindow = showInWindow;
        _error = error;
    }

    /// <summary>
    /// Called when an unattended run starts shutting the app down. The run has already exited
    /// nonzero, so a later failure is written to stderr only.
    /// </summary>
    public void OnUnattendedExitStarted() => _isUnattendedExitStarted = true;

    /// <summary>Reports <paramref name="exception"/> as the reason startup failed.</summary>
    public void Report(Exception exception)
    {
        _error.WriteLine("Startup failed: " + exception);
        if (!_isUnattendedExitStarted)
        {
            _showInWindow(exception);
        }
    }
}
