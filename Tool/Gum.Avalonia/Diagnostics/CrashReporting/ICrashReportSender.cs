using System;

namespace Gum.Avalonia.Diagnostics.CrashReporting;

/// <summary>Sends one crash report to the Gum developers. <see cref="SentryCrashReportSender"/> sends it to Sentry.</summary>
public interface ICrashReportSender
{
    /// <summary>
    /// Sends the exception. A fatal one is written to the offline cache before this returns, so it
    /// is sent at the next launch if the process ends first. May throw.
    /// </summary>
    void Send(Exception exception, string source, bool isFatal);
}
