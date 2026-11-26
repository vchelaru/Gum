using System;
using Gum.Diagnostics;

namespace Gum.Avalonia.Diagnostics.CrashReporting;

/// <summary>
/// Wraps the local <see cref="ICrashReporter"/>: every call still writes the local log, and a
/// recoverable or fatal error is also sent when the user has opted in. Errors logged without telling
/// the user (<see cref="LogRecoverable"/>) stay local. Never throws, since it runs in last-chance handlers.
/// </summary>
public class SentryCrashReporter : ICrashReporter
{
    /// <summary>
    /// The Sentry project's DSN (Settings > Client Keys), built into the binary; a DSN is public by
    /// design. Emptying it turns crash reporting off: Sentry is never started, the local reporter is
    /// not wrapped and the consent prompt never shows.
    /// </summary>
    public const string Dsn = "https://766918fc390377f81ba9f84db571b61c@o4512500395933696.ingest.us.sentry.io/4512500480999424";

    private readonly ICrashReporter _inner;
    private readonly ICrashReportSender _sender;
    private readonly Func<bool> _isSendingAllowed;

    /// <param name="inner">Writes the local logs.</param>
    /// <param name="sender">Sends a report.</param>
    /// <param name="isSendingAllowed">Whether the user opted in, read at each report.</param>
    public SentryCrashReporter(ICrashReporter inner, ICrashReportSender sender, Func<bool> isSendingAllowed)
    {
        _inner = inner;
        _sender = sender;
        _isSendingAllowed = isSendingAllowed;
    }

    /// <summary>The local reporter wrapped in a <see cref="SentryCrashReporter"/>, or the local reporter itself when <paramref name="dsn"/> is empty.</summary>
    /// <param name="createSender">Starts Sentry; not called when <paramref name="dsn"/> is empty. If it throws, the local reporter is returned.</param>
    public static ICrashReporter Wrap(ICrashReporter inner, string dsn, Func<ICrashReportSender> createSender, Func<bool> isSendingAllowed)
    {
        if (string.IsNullOrWhiteSpace(dsn))
        {
            return inner;
        }

        try
        {
            return new SentryCrashReporter(inner, createSender(), isSendingAllowed);
        }
        catch (Exception exception)
        {
            // Gum starts without crash reports rather than not at all.
            inner.LogRecoverable(exception, "crash reporting startup");
            return inner;
        }
    }

    /// <inheritdoc/>
    public string DirectoryPath => _inner.DirectoryPath;

    /// <inheritdoc/>
    public string? ReportRecoverable(Exception exception, string source)
    {
        // Sent first: the inner reporter may show a modal, and a report should not wait on the user.
        TrySend(exception, source, isFatal: false);
        return TryInner(() => _inner.ReportRecoverable(exception, source));
    }

    /// <inheritdoc/>
    public string? LogRecoverable(Exception exception, string source) =>
        TryInner(() => _inner.LogRecoverable(exception, source));

    /// <inheritdoc/>
    public string? ReportFatal(Exception exception, string source)
    {
        string? path = TryInner(() => _inner.ReportFatal(exception, source));
        TrySend(exception, source, isFatal: true);
        return path;
    }

    /// <inheritdoc/>
    public void PromptForPreviousCrash() => TryInner<object?>(() =>
    {
        _inner.PromptForPreviousCrash();
        return null;
    });

    private void TrySend(Exception exception, string source, bool isFatal)
    {
        try
        {
            if (_isSendingAllowed())
            {
                _sender.Send(exception, source, isFatal);
            }
        }
        catch
        {
            // A failed report must not take down the handler reporting the original error.
        }
    }

    private static T? TryInner<T>(Func<T> call)
    {
        try
        {
            return call();
        }
        catch
        {
            return default;
        }
    }
}
