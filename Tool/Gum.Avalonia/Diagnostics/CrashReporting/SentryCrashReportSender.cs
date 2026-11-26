using System;
using Sentry;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Gum.Avalonia.Diagnostics.CrashReporting;

/// <summary>Sends crash reports through a Sentry hub; <see cref="Start"/> initializes the SDK's.</summary>
public class SentryCrashReportSender : ICrashReportSender
{
    private static readonly TimeSpan FatalFlushTimeout = TimeSpan.FromSeconds(2);

    private readonly IHub _hub;

    /// <summary>Creates a sender over <paramref name="hub"/>.</summary>
    public SentryCrashReportSender(IHub hub)
    {
        _hub = hub;
    }

    /// <summary>Initializes the Sentry SDK and returns a sender over it.</summary>
    public static SentryCrashReportSender Start(SentryOptions options)
    {
        SentrySdk.Init(options);
        return new SentryCrashReportSender(HubAdapter.Instance);
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
                // FileName holds only the file's name; the full path is in AbsolutePath.
                frame.AbsolutePath = scrubber.Scrub(frame.AbsolutePath);
            }
        }

        // Each loaded assembly's path and its PDB's build path, which sit under the user's folder for
        // a Gum unzipped there or a project's own plugins.
        foreach (DebugImage image in sentryEvent.DebugImages ?? (System.Collections.Generic.IEnumerable<DebugImage>)Array.Empty<DebugImage>())
        {
            image.CodeFile = scrubber.Scrub(image.CodeFile);
            image.DebugFile = scrubber.Scrub(image.DebugFile);
        }

        return sentryEvent;
    }

    /// <inheritdoc/>
    public void Send(Exception exception, string source, bool isFatal)
    {
        _hub.CaptureEvent(new SentryEvent(exception), scope =>
        {
            scope.SetTag("source", source);
            scope.SetTag("fatal", isFatal ? "true" : "false");
        });
        if (isFatal)
        {
            // The process is about to end; the flush puts the report in the offline cache or on the wire first.
            _hub.FlushAsync(FatalFlushTimeout).GetAwaiter().GetResult();
        }
    }
}
