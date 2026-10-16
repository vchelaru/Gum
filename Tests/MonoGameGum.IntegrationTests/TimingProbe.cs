using System.Diagnostics;

namespace MonoGameGum.IntegrationTests;

// TEMPORARY probe for #6078; removed before the PR is finalized.
internal static class TimingProbe
{
    public static readonly string Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gum-itest-timing.log");

    public static void Mark(string phase, Stopwatch stopwatch)
    {
        System.IO.File.AppendAllText(Path, $"{phase}\t{stopwatch.ElapsedMilliseconds}\n");
        stopwatch.Restart();
    }
}
