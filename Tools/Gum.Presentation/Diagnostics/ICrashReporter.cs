using System;

namespace Gum.Diagnostics;

/// <summary>
/// Writes unhandled exceptions to log files under the user's Gum data folder and tells the user
/// where they are. The head wires the process-wide hooks to it; see <see cref="CrashReporter"/>.
/// </summary>
public interface ICrashReporter
{
    /// <summary>The folder the logs are written to.</summary>
    string DirectoryPath { get; }

    /// <summary>
    /// Logs an exception Gum survived (the UI thread, an unobserved task) and, for the first one in
    /// a session, tells the user where the log is. Returns the log's path, or null when none was
    /// written. Never throws.
    /// </summary>
    string? ReportRecoverable(Exception exception, string source);

    /// <summary>
    /// Logs an exception that is about to end the process. The user is told at the next launch by
    /// <see cref="PromptForPreviousCrash"/>. Returns the log's path, or null when none was written.
    /// Never throws.
    /// </summary>
    string? ReportFatal(Exception exception, string source);

    /// <summary>
    /// Tells the user about logs from a crash that ended an earlier session, once: the logs then
    /// move to the Reported subfolder.
    /// </summary>
    void PromptForPreviousCrash();
}
