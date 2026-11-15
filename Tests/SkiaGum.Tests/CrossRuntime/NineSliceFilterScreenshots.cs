using System.Text;
using CrossRuntimeTests;
using SkiaGum.Renderables;
using SkiaSharp;
using Xunit;

namespace SkiaGum.Tests.CrossRuntime;

/// <summary>
/// Spike: renders NineSlice cases with the white Modulate filter (A) and without it (B), next to the
/// checked-in MonoGame picture (C), into the folder named by GUM_NINESLICE_SHOTS_OUT. Does nothing when
/// the variable is unset. Every picture is upscaled 6x (nearest neighbour) with a banner.
/// </summary>
public class NineSliceFilterScreenshots : BaseTestClass
{
    public const string OutputVariable = "GUM_NINESLICE_SHOTS_OUT";
    private const int Scale = 6;
    private const int BannerHeight = 28;

    [Fact]
    public void WriteScreenshots()
    {
        string? outputRoot = Environment.GetEnvironmentVariable(OutputVariable);
        if (string.IsNullOrEmpty(outputRoot))
        {
            return;
        }

        NineSliceSheet sheet = NineSliceCases.Sheets.Single(candidate => candidate.Name == "frame2-6x6");
        using SKBitmap baseline = SheetComparer.LoadBaseline(NineSliceCases.BaselinePath(sheet));

        // Whole sheet.
        Write(Path.Combine(outputRoot, "sheet-frame2-6x6"),
            RenderSheet(sheet, skip: false), RenderSheet(sheet, skip: true), baseline);

        // Cells with the largest differences on macOS CI.
        foreach (string name in new[] { "9x1", "9x6", "3x9" })
        {
            int index = sheet.Cases.ToList().FindIndex(c => c.Name == name);
            NineSliceCase nineSliceCase = sheet.Cases[index];
            (int originX, int originY) = NineSliceCases.CellOrigin(index);
            SKBitmap expected = new(NineSliceCases.CellSize, NineSliceCases.CellSize);
            for (int y = 0; y < expected.Height; y++)
            {
                for (int x = 0; x < expected.Width; x++)
                {
                    expected.SetPixel(x, y, baseline.GetPixel(originX + x, originY + y));
                }
            }
            Write(Path.Combine(outputRoot, "cell-" + name), RenderCell(nineSliceCase, skip: false),
                RenderCell(nineSliceCase, skip: true), expected);
        }
    }

    private static SKBitmap RenderCell(NineSliceCase nineSliceCase, bool skip)
    {
        RenderableShapeBase.SpikeSkipOpaqueWhiteTint = skip;
        try
        {
            return NineSliceCrossRuntimeTests.RenderCase(nineSliceCase);
        }
        finally
        {
            RenderableShapeBase.SpikeSkipOpaqueWhiteTint = false;
        }
    }

    private static SKBitmap RenderSheet(NineSliceSheet sheet, bool skip)
    {
        (int width, int height) = NineSliceCases.SheetSize(sheet.Cases.Count);
        SKBitmap result = new(width, height);
        for (int index = 0; index < sheet.Cases.Count; index++)
        {
            using SKBitmap cell = RenderCell(sheet.Cases[index], skip);
            (int originX, int originY) = NineSliceCases.CellOrigin(index);
            for (int y = 0; y < cell.Height; y++)
            {
                for (int x = 0; x < cell.Width; x++)
                {
                    result.SetPixel(originX + x, originY + y, cell.GetPixel(x, y));
                }
            }
        }
        return result;
    }

    private static void Write(string folder, SKBitmap a, SKBitmap b, SKBitmap c)
    {
        Directory.CreateDirectory(folder);
        StringBuilder text = new();
        SKBitmap diff = new(a.Width, a.Height);
        int differing = 0;
        int max = 0;
        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                SKColor ca = a.GetPixel(x, y);
                SKColor cb = b.GetPixel(x, y);
                int d = Math.Max(Math.Abs(ca.Red - cb.Red), Math.Max(Math.Abs(ca.Green - cb.Green), Math.Abs(ca.Blue - cb.Blue)));
                d = Math.Max(d, Math.Abs(ca.Alpha - cb.Alpha));
                diff.SetPixel(x, y, d > 0 ? new SKColor(255, 0, 255) : SKColors.White);
                if (d > 0)
                {
                    differing++;
                    max = Math.Max(max, d);
                    text.AppendLine($"  ({x},{y}) A=#{ca.Red:X2}{ca.Green:X2}{ca.Blue:X2}{ca.Alpha:X2} B=#{cb.Red:X2}{cb.Green:X2}{cb.Blue:X2}{cb.Alpha:X2} diff={d}");
                }
            }
        }
        string header = $"A = with white filter (today's main), B = white filter skipped. Max channel difference: {max}. Differing pixels: {differing}." + Environment.NewLine
            + (differing == 0 ? "No differing pixels." : "Coordinates (x,y in the unscaled image):") + Environment.NewLine;
        File.WriteAllText(Path.Combine(folder, "diff.txt"), header + text);

        Save(Path.Combine(folder, "A.png"), a, "A: WITH white filter");
        Save(Path.Combine(folder, "B.png"), b, "B: white filter SKIPPED");
        Save(Path.Combine(folder, "C.png"), c, "C: MonoGame baseline");
        Save(Path.Combine(folder, "diff-AB.png"), diff, $"diff A-B, magenta=differs: {differing}px");
    }

    private static void Save(string path, SKBitmap source, string banner)
    {
        int width = source.Width * Scale;
        int height = source.Height * Scale;
        using SKSurface surface = SKSurface.Create(new SKImageInfo(width, height + BannerHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Black);
        using SKImage image = SKImage.FromBitmap(source);
        using SKPaint imagePaint = new() { IsAntialias = false };
        canvas.DrawImage(image, new SKRect(0, BannerHeight, width, height + BannerHeight),
            new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None), imagePaint);
        using SKPaint bannerPaint = new() { Color = SKColors.Yellow, IsAntialias = true };
        using SKFont font = new() { Size = 12 };
        canvas.DrawText(banner, 4, 19, font, bannerPaint);
        using SKImage output = surface.Snapshot();
        using SKData data = output.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
    }
}
