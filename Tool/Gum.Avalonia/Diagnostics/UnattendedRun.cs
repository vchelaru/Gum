using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Gum.Avalonia.Diagnostics;

/// <summary>How the tool's startup ended, for an unattended run.</summary>
public enum UnattendedStartupOutcome
{
    /// <summary>The project (if any) loaded and the startup selection applied.</summary>
    Ready,
    /// <summary>Startup threw; the error is already on stderr.</summary>
    Failed,
    /// <summary>The command line asked for an immediate exit, which is already under way.</summary>
    ExitRequested,
}

/// <summary>
/// The order of an unattended (<c>--exit-after</c>) run (#5170): wait for startup (project load and
/// <c>--select</c>), then for the canvas to present a frame, optionally zoom to fit and wait for the
/// fitted frame, then capture. <c>--exit-after</c> is only the upper bound: a run that isn't ready
/// by then fails with a nonzero exit code instead of capturing a half-loaded window.
/// </summary>
public static class UnattendedRun
{
    /// <summary>
    /// Runs the sequence and returns the process exit code: 0 once captured, 1 when startup failed
    /// or the deadline passed first (with the reason on <paramref name="error"/>), or null when
    /// startup already asked the app to exit.
    /// </summary>
    /// <param name="startup">Completes when the tool's startup ends.</param>
    /// <param name="deadline">Completes when <c>--exit-after</c> runs out.</param>
    /// <param name="exitAfterSeconds">The <c>--exit-after</c> value, for the error message.</param>
    /// <param name="nextCanvasFrame">Asks the editor canvas to draw; completes once it has presented that frame.</param>
    /// <param name="zoomToFit">Frames the selected element and describes the result; null unless <c>--zoom-to-fit</c>.</param>
    /// <param name="capture">Writes the screenshot; null unless <c>--screenshot</c>.</param>
    /// <param name="output">Where the zoom-to-fit description goes.</param>
    /// <param name="error">Where a failure's reason goes.</param>
    /// <param name="describeCanvas">Describes the canvas's frame state; appended to a failure that waited on a frame (#5680).</param>
    public static async Task<int?> RunAsync(
        Task<UnattendedStartupOutcome> startup,
        Task deadline,
        double exitAfterSeconds,
        Func<Task> nextCanvasFrame,
        Func<string?>? zoomToFit,
        Action? capture,
        TextWriter output,
        TextWriter error,
        Func<string?>? describeCanvas = null)
    {
        if (!await CompletesBefore(startup, deadline))
        {
            return Fail(error, exitAfterSeconds, "the project had not finished loading");
        }

        switch (await startup)
        {
            case UnattendedStartupOutcome.ExitRequested:
                return null;
            case UnattendedStartupOutcome.Failed:
                return 1;
        }

        // The first frame also brings the camera's view size up to date for the fit.
        if (!await CompletesBefore(nextCanvasFrame(), deadline))
        {
            return Fail(error, exitAfterSeconds, "the canvas had not drawn a frame after the project loaded", describeCanvas);
        }

        if (zoomToFit != null)
        {
            output.WriteLine(zoomToFit() ?? "Zoom to fit: nothing is selected, or the selection has nothing visible; the camera was left alone.");
            if (!await CompletesBefore(nextCanvasFrame(), deadline))
            {
                return Fail(error, exitAfterSeconds, "the canvas had not drawn a frame after zooming to fit", describeCanvas);
            }
        }

        capture?.Invoke();
        return 0;
    }

    private static async Task<bool> CompletesBefore(Task task, Task deadline)
    {
        Task first = await Task.WhenAny(task, deadline);
        if (first == task)
        {
            // Surfaces a faulted wait (no canvas to answer, say) instead of treating it as done.
            await task;
            return true;
        }
        return false;
    }

    private static int Fail(TextWriter error, double exitAfterSeconds, string reason, Func<string?>? describeCanvas = null)
    {
        error.WriteLine(string.Format(
            CultureInfo.InvariantCulture,
            "Gum was not ready within --exit-after {0:0.#} s: {1}. No screenshot was taken.",
            exitAfterSeconds,
            reason));
        if (describeCanvas != null)
        {
            string state;
            try
            {
                state = describeCanvas() ?? "unavailable";
            }
            catch (Exception exception)
            {
                state = "unavailable (" + exception.Message + ")";
            }
            error.WriteLine("Canvas state: " + state);
        }
        error.Flush();
        return 1;
    }
}
