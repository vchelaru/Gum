using System;
using Sentry;
using Sentry.Protocol;

namespace Gum.Avalonia.Diagnostics.CrashReporting;

/// <summary>Sends crash reports through the Sentry SDK, which <see cref="Start"/> initializes.</summary>
public class SentryCrashReportSender : ICrashReportSender
{
    private static readonly TimeSpan FatalFlushTimeout = TimeSpan.FromSeconds(2);

    /// <summary>Initializes the Sentry SDK and returns a sender over it.</summary>
    public static SentryCrashReportSender Start(SentryOptions options)
    {
        SentrySdk.Init(options);
        return new SentryCrashReportSender();
    }

    /// <summary>
    /// The options Gum starts Sentry with: no default PII, the tool version as the release, an
    /// offline cache so a fatal crash is sent at the next launch, Sentry's own process hooks off
    /// (<see cref="UnhandledExceptionHooks"/> routes those) and paths scrubbed before sending.
    /// </summary>
    public static SentryOptions CreateOptions(string dsn, string release, string cacheDirectory, CrashReportScrubber scrubber)
    {
        SentryOptions options = new SentryOptions
        {
            Dsn = dsn,
            Release = release,
            SendDefaultPii = false,
            CacheDirectoryPath = cacheDirectory,
            AutoSessionTracking = false,
        };
        options.DisableAppDomainUnhandledExceptionCapture();
        options.DisableUnobservedTaskExceptionCapture();
        options.SetBeforeSend((sentryEvent, _) => Scrub(sentryEvent, scrubber));
        return options;
    }

    /// <summary>Removes the machine name and replaces user and project paths in the event's text.</summary>
    public static SentryEvent Scrub(SentryEvent sentryEvent, CrashReportScrubber scrubber)
    {
        sentryEvent.ServerName = null;
        if (sentryEvent.Message is { } message)
        {
            message.Message = scrubber.Scrub(message.Message);
            message.Formatted = scrubber.Scrub(message.Formatted);
        }

        foreach (SentryException exception in sentryEvent.SentryExceptions ?? Array.Empty<SentryException>())
        {
            exception.Value = scrubber.Scrub(exception.Value);
            foreach (SentryStackFrame frame in exception.Stacktrace?.Frames ?? (System.Collections.Generic.IList<SentryStackFrame>)Array.Empty<SentryStackFrame>())
            {
                frame.FileName = scrubber.Scrub(frame.FileName);
                frame.AbsolutePath = scrubber.Scrub(frame.AbsolutePath);
            }
        }

        return sentryEvent;
    }

    /// <inheritdoc/>
    public void Send(Exception exception, string source, bool isFatal)
    {
        SentrySdk.CaptureException(exception, scope =>
        {
            scope.SetTag("source", source);
            scope.SetTag("fatal", isFatal ? "true" : "false");
        });
        if (isFatal)
        {
            SentrySdk.Flush(FatalFlushTimeout);
        }
    }
}
