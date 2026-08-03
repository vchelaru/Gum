using System;
using System.Diagnostics;
using System.IO;

namespace Gum.Diagnostics;

// TEMPORARY — instrumentation for #4282 (evaluating ReadyToRun to reduce first-call JIT warmup).
// Writes startup timing milestones to a log file so a ReadyToRun publish can be compared against
// a normal publish. Remove once the evaluation is complete.
internal static class TempStartupTimingLog
{
    private static readonly string LogFilePath = Path.Combine(Path.GetTempPath(), "GumStartupTiming.log");
    private static readonly Stopwatch ElapsedSinceFirstUse = Stopwatch.StartNew();
    private static readonly object WriteLock = new();

    public static void Log(string milestone)
    {
        string line = $"{DateTime.Now:O}\t{ElapsedSinceFirstUse.ElapsedMilliseconds}ms\t{milestone}";
        lock (WriteLock)
        {
            File.AppendAllText(LogFilePath, line + Environment.NewLine);
        }
    }
}
