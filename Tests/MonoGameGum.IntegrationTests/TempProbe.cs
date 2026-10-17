using System.Diagnostics;
using System.Text;

namespace MonoGameGum.IntegrationTests;

// TEMPORARY (#6085): per-phase timings, reported through a thrown exception so CI prints them.
internal static class TempProbe
{
    private static readonly Stopwatch Watch = new Stopwatch();
    private static readonly StringBuilder Text = new StringBuilder();
    private static long _last;

    public static void Reset()
    {
        Text.Clear();
        Watch.Restart();
        _last = 0;
    }

    public static void Mark(string phase)
    {
        long now = Watch.ElapsedMilliseconds;
        Text.Append(phase).Append('=').Append(now - _last).Append("ms; ");
        _last = now;
    }

    public static void Report()
    {
        Mark("end");
        throw new System.InvalidOperationException("PROBE total=" + Watch.ElapsedMilliseconds + "ms: " + Text);
    }
}
