using System;
using HarfBuzzSharp;
using HbBuffer = HarfBuzzSharp.Buffer;
using Probe.Common;

namespace Probe11.SkiaHarfBuzz;

/// <summary>
/// Probe 11 - does the tool's HarfBuzz native library load and run under Wine? Probe4 covers base
/// SkiaSharp raster; the SkiaPlugin additionally uses HarfBuzz (libHarfBuzzSharp) for text shaping,
/// which is a separate native library that can fail to load independently. Constructing and using a
/// HarfBuzz Buffer forces that library to load.
/// </summary>
internal static class Program
{
    private static int Main()
    {
        return ProbeLog.Run("Probe11.SkiaHarfBuzz", () =>
        {
            ProbeLog.Info("HarfBuzzSharp", typeof(HbBuffer).Assembly.GetName().Version?.ToString() ?? "unknown");

            ProbeLog.Step("Creating a HarfBuzz Buffer (loads native libHarfBuzzSharp)");
            using HbBuffer buffer = new HbBuffer();
            buffer.AddUtf8("Gum on Wine");
            buffer.GuessSegmentProperties();

            ProbeLog.Info("BufferLength", buffer.Length.ToString());
            ProbeLog.Info("Direction", buffer.Direction.ToString());
            ProbeLog.Step("HarfBuzz native library loaded and responded");
        });
    }
}
