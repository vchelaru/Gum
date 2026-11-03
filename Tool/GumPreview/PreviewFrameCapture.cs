using System;
using System.IO;
using System.Linq;

namespace GumPreview;

/// <summary>
/// Decides when Preview saves a screenshot of its own window for the diagnostic log, and names and
/// prunes the files: at most one capture per <see cref="MinimumIntervalMilliseconds"/>, files named
/// <c>preview-frame-0007.png</c> next to the log with a sequence that keeps counting across runs, and
/// only the newest <c>keep</c> files left. Only created when logging is on; a request while a capture
/// is waiting for the next draw just updates its reason.
/// </summary>
public sealed class PreviewFrameCapture
{
    private const string Prefix = "preview-frame-";
    private readonly string _folder;
    private readonly Func<long> _nowMilliseconds;
    private readonly int _keep;
    private long _lastCaptureMilliseconds = long.MinValue / 2;
    private int _sequence;

    public PreviewFrameCapture(string folder, Func<long> nowMilliseconds, int keep = 20)
    {
        _folder = folder;
        _nowMilliseconds = nowMilliseconds;
        _keep = keep;
        _sequence = ExistingSequences().DefaultIfEmpty(0).Max();
    }

    /// <summary>The shortest time between two captures: 4 per second.</summary>
    public const long MinimumIntervalMilliseconds = 250;

    /// <summary>Why the waiting capture was asked for, or null when none waits.</summary>
    public string? PendingReason { get; private set; }

    public bool HasPending => PendingReason != null;

    /// <summary>Asks for a capture at the end of the next draw. Returns false when it is too soon after the last one.</summary>
    public bool Request(string reason)
    {
        if (PendingReason == null && _nowMilliseconds() - _lastCaptureMilliseconds < MinimumIntervalMilliseconds)
        {
            return false;
        }
        PendingReason = reason;
        return true;
    }

    /// <summary>
    /// Claims the waiting capture: returns the path to write and the reason, clears the request and removes
    /// the files beyond the newest <c>keep</c>. Call only when <see cref="HasPending"/>.
    /// </summary>
    public string Claim(out string reason)
    {
        reason = PendingReason ?? "";
        PendingReason = null;
        _lastCaptureMilliseconds = _nowMilliseconds();
        _sequence++;
        string path = Path.Combine(_folder, $"{Prefix}{_sequence:D4}.png");
        Prune(path);
        return path;
    }

    private System.Collections.Generic.IEnumerable<int> ExistingSequences()
    {
        if (!Directory.Exists(_folder))
        {
            yield break;
        }
        foreach (string file in Directory.GetFiles(_folder, Prefix + "*.png"))
        {
            string number = Path.GetFileNameWithoutExtension(file).Substring(Prefix.Length);
            if (int.TryParse(number, out int sequence))
            {
                yield return sequence;
            }
        }
    }

    private void Prune(string aboutToWrite)
    {
        try
        {
            string[] old = Directory.GetFiles(_folder, Prefix + "*.png")
                .Where(f => f != aboutToWrite)
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToArray();
            // One slot is for the file about to be written.
            for (int i = 0; i < old.Length - (_keep - 1); i++)
            {
                File.Delete(old[i]);
            }
        }
        catch (Exception)
        {
            // Pruning is best effort; a log helper must never stop Preview.
        }
    }
}
