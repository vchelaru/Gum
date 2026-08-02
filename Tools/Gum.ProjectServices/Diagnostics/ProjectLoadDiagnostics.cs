using System;
using System.Diagnostics;
using System.IO;

namespace Gum.ProjectServices.Diagnostics;

/// <summary>
/// Temporary instrumentation for issue #4224 (slow ProjectLoad plugin handlers). Writes sub-step
/// timings to a fixed file path so they can be read back without relaying console/output-panel text.
/// Duplicated (not shared) from Gum.Presentation's copy of the same helper to avoid routing a
/// throwaway probe through GumCommon, which every runtime library also depends on. Remove both
/// copies once the issue's hot spots are identified and fixed.
/// </summary>
public static class ProjectLoadDiagnostics
{
    public static readonly string LogPath = Path.Combine(Path.GetTempPath(), "GumProjectLoadTiming.log");

    public static void Log(string message) =>
        File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");

    /// <summary>
    /// Logs the elapsed time for the scope wrapped in a <c>using</c> when it is disposed.
    /// </summary>
    public static IDisposable Time(string label) => new Scope(label);

    private sealed class Scope : IDisposable
    {
        private readonly string _label;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public Scope(string label) => _label = label;

        public void Dispose()
        {
            _stopwatch.Stop();
            Log($"{_label}: {_stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
