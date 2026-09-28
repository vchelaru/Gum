using System.Globalization;

namespace GumPreview;

/// <summary>
/// The progress of an unattended (<c>--exit-after</c>) GumPreview run (#5471): the project loads,
/// then the first frame is drawn and captured. The game thread and the deadline timer's thread race
/// to finish the run; whichever claims it first decides the exit code, so a capture never runs
/// after a timeout has been reported and vice versa.
/// </summary>
public sealed class UnattendedPreviewRun
{
    private readonly object _lock = new object();
    private readonly double _exitAfterSeconds;
    private bool _isLoaded;
    private bool _isFinished;

    /// <param name="exitAfterSeconds">The <c>--exit-after</c> value, for the timeout message.</param>
    public UnattendedPreviewRun(double exitAfterSeconds)
    {
        _exitAfterSeconds = exitAfterSeconds;
    }

    /// <summary>Records that the project loaded and the element is shown.</summary>
    public void MarkLoaded()
    {
        lock (_lock)
        {
            _isLoaded = true;
        }
    }

    /// <summary>
    /// Called after a frame is drawn. True once, when the project has loaded and the run has not
    /// finished otherwise; the caller then captures and exits with 0.
    /// </summary>
    public bool TryClaimCapture()
    {
        lock (_lock)
        {
            if (!_isLoaded || _isFinished)
            {
                return false;
            }
            _isFinished = true;
            return true;
        }
    }

    /// <summary>
    /// Called when <c>--exit-after</c> runs out. Returns the message for stderr (the caller exits with
    /// 1), or null when the run already finished.
    /// </summary>
    public string? TryExpire()
    {
        lock (_lock)
        {
            if (_isFinished)
            {
                return null;
            }
            _isFinished = true;
            string stage = _isLoaded
                ? "no frame had been drawn after the project loaded"
                : "the project had not finished loading";
            return string.Format(
                CultureInfo.InvariantCulture,
                "GumPreview was not ready within --exit-after {0:0.#} s: {1}. No screenshot was taken.",
                _exitAfterSeconds,
                stage);
        }
    }

    /// <summary>
    /// Ends the run as a failure. Returns the message for stderr (the caller exits with 1), or null
    /// when the run already finished.
    /// </summary>
    public string? TryFail(string reason)
    {
        lock (_lock)
        {
            if (_isFinished)
            {
                return null;
            }
            _isFinished = true;
            return $"GumPreview: {reason} No screenshot was taken.";
        }
    }
}
