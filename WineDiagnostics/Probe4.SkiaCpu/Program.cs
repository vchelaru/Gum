using System;
using System.IO;
using SkiaSharp;
using Probe.Common;

namespace Probe4.SkiaCpu;

/// <summary>
/// Probe 4 - can SkiaSharp load its native library (libSkiaSharp) and render on the CPU under Wine?
/// In the Gum tool the SVG/Lottie plugin renders Skia content on the CPU and uploads it as a texture
/// (the GPU/OpenGL shared-context path is compiled out of the tool). This creates a raster surface,
/// draws, and saves a PNG. A FAIL here points at the SkiaSharp native asset loading in the prefix.
/// </summary>
internal static class Program
{
    private static int Main()
    {
        return ProbeLog.Run("Probe4.SkiaCpu", () =>
        {
            ProbeLog.Info("SkiaSharp", typeof(SKSurface).Assembly.GetName().Version?.ToString() ?? "unknown");

            ProbeLog.Step("Creating CPU raster SKSurface (256x256) - this loads the native libSkiaSharp");
            SKImageInfo info = new SKImageInfo(256, 256);
            using SKSurface surface = SKSurface.Create(info);
            if (surface == null)
            {
                throw new InvalidOperationException("SKSurface.Create returned null (raster surface could not be created).");
            }

            ProbeLog.Step("Drawing with Skia");
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(SKColors.CornflowerBlue);
            using SKPaint paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
            canvas.DrawCircle(128, 128, 90, paint);
            canvas.Flush();

            ProbeLog.Step("Encoding PNG snapshot");
            using SKImage image = surface.Snapshot();
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 90);

            string? configured = Environment.GetEnvironmentVariable("PROBE_LOG_DIR");
            string directory = string.IsNullOrWhiteSpace(configured) ? AppContext.BaseDirectory : configured;
            string outPath = Path.Combine(directory, "Probe4.SkiaCpu.png");
            using (FileStream fs = File.Create(outPath))
            {
                data.SaveTo(fs);
            }
            ProbeLog.Step("Wrote " + outPath);
        });
    }
}
