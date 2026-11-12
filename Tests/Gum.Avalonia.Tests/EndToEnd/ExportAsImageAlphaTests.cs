using Gum.Commands;
using Gum.DataTypes;
using Microsoft.Extensions.DependencyInjection;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// File > Export > Export as Image writes a straight-alpha PNG (#7992). A translucent element must keep its
/// own color and alpha whether it is drawn directly, through a render-target container, or through a
/// translucent render-target container, and overlapping translucent elements must combine by the "over"
/// operator. Expected values by hand, with a = alpha / 255: one element is (color, a); two overlapping are
/// alpha = a2 + a1 * (1 - a2), color = (c2 * a2 + c1 * a1 * (1 - a2)) / alpha. Runs on the straight and the
/// premultiplied pipeline. Set GUM_EXPORT_ALPHA_DIR to keep the exported PNG and the measured values.
/// </summary>
public class ExportAsImageAlphaTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Feature", "FILE-007")]
    public void ExportAsImage_TranslucentElements_WriteStraightAlphaPng(bool premultiplied)
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            // Set before anything loads, so textures and fonts are premultiplied on load.
            Renderer.UsePremultipliedAlpha = premultiplied;
            try
            {
                ExportAndCheck(premultiplied);
            }
            finally
            {
                Renderer.UsePremultipliedAlpha = false;
            }
        });
    }

    private static void ExportAndCheck(bool premultiplied)
    {
        using CanvasHarness canvas = new CanvasHarness();
        ComponentSave card = canvas.Project.AddComponent("Card");
        WriteSolidPng(Path.Combine(canvas.Project.ProjectFolder, "red.png"), SkiaSharp.SKColors.Red);
        WriteSolidPng(Path.Combine(canvas.Project.ProjectFolder, "green.png"), new SkiaSharp.SKColor(0, 255, 0));

        // Opaque red sprite at Alpha 128, drawn directly.
        AddSprite(canvas, card, "Direct", 20, 20, "red.png", 128);

        // The same, inside a render-target container.
        canvas.AddInstance(card, "Holder", "Container", x: 100, y: 20, width: 60, height: 40);
        AddSprite(canvas, card, "InTarget", 0, 0, "red.png", 128);
        card.DefaultState!.SetValue("Holder.IsRenderTarget", true, "bool");
        card.DefaultState.SetValue("InTarget.Parent", "Holder", "string");

        // A filled Rectangle (Apos.Shapes, its own batcher) at fill alpha 128.
        canvas.AddInstance(card, "Shape", "Rectangle", x: 180, y: 20, width: 60, height: 40);
        card.DefaultState.SetValue("Shape.IsFilled", true, "bool");
        card.DefaultState.SetValue("Shape.FillRed", 255, "int");
        card.DefaultState.SetValue("Shape.FillGreen", 0, "int");
        card.DefaultState.SetValue("Shape.FillBlue", 0, "int");
        card.DefaultState.SetValue("Shape.FillAlpha", 128, "int");
        card.DefaultState.SetValue("Shape.StrokeWidth", 0f, "float");

        // An opaque red sprite inside a render-target container at Alpha 128.
        canvas.AddInstance(card, "Faded", "Container", x: 260, y: 20, width: 60, height: 40);
        AddSprite(canvas, card, "InFaded", 0, 0, "red.png", 255);
        card.DefaultState.SetValue("Faded.IsRenderTarget", true, "bool");
        card.DefaultState.SetValue("Faded.Alpha", 128, "int");
        card.DefaultState.SetValue("InFaded.Parent", "Faded", "string");

        // Red then green, both Alpha 128, overlapping by 30 x 20.
        AddSprite(canvas, card, "Under", 20, 80, "red.png", 128);
        AddSprite(canvas, card, "Over", 50, 100, "green.png", 128);

        // Red text at Alpha 128.
        canvas.AddInstance(card, "Label", "Text", x: 180, y: 90);
        card.DefaultState.SetValue("Label.Text", "HOUZ", "string");
        card.DefaultState.SetValue("Label.Red", 255, "int");
        card.DefaultState.SetValue("Label.Green", 0, "int");
        card.DefaultState.SetValue("Label.Blue", 0, "int");
        card.DefaultState.SetValue("Label.Alpha", 128, "int");

        canvas.Tree.SaveAll();
        Services.GetRequiredService<IGuiCommands>().RefreshElementTreeView(card);
        canvas.Wireframe.RefreshAll(forceLayout: true);
        canvas.Tree.Click(canvas.Tree.NodeFor(card));
        canvas.Frame();

        string folder = Path.Combine(Path.GetTempPath(), "GumExportAlpha", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string png = Path.Combine(folder, "Card.png");
        try
        {
            canvas.Project.Dialogs.AnswerNextSaveFile(png);
            canvas.Tree.PickMainMenu("File", "Export", "Export as Image");
            // The capture happens on the next frame the canvas draws.
            canvas.Frame();
            File.Exists(png).ShouldBeTrue(canvas.Describe());

            (int X, int Y) Screen(float x, float y)
            {
                canvas.Camera.WorldToScreen(x, y, out float sx, out float sy);
                return ((int)sx, (int)sy);
            }

            ExportedPixelChecks.Sample[] samples =
            {
                new("direct sprite", Screen(40, 30), 255, 0, 0, 128),
                new("render target", Screen(120, 30), 255, 0, 0, 128),
                new("rectangle shape", Screen(210, 40), 255, 0, 0, 128),
                new("render target at alpha 128", Screen(290, 40), 255, 0, 0, 128),
                new("under only", Screen(30, 90), 255, 0, 0, 128),
                new("overlap", Screen(65, 110), 85, 170, 0, 192),
                new("over only", Screen(100, 130), 0, 255, 0, 128),
                new("outside", Screen(340, 140), 0, 0, 0, 0),
            };
            (int left, int top) = Screen(180, 90);
            (int right, int bottom) = Screen(260, 130);

            string name = premultiplied ? "premultiplied" : "straight";
            ExportedPixelChecks.Check(png, samples, new SkiaSharp.SKRectI(left, top, right, bottom), name,
                Environment.GetEnvironmentVariable("GUM_EXPORT_ALPHA_DIR"));
        }
        finally
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (IOException)
            {
                // A file watcher may still hold it; the temp folder is cleaned up later.
            }
        }

        canvas.AssertOracles();
    }

    // 60 x 40, the sprite's default size (100% of its file).
    private static void WriteSolidPng(string path, SkiaSharp.SKColor color)
    {
        using SkiaSharp.SKBitmap bitmap = new SkiaSharp.SKBitmap(60, 40);
        bitmap.Erase(color);
        using FileStream stream = File.Create(path);
        bitmap.Encode(stream, SkiaSharp.SKEncodedImageFormat.Png, 100).ShouldBeTrue();
    }

    private static void AddSprite(CanvasHarness canvas, ComponentSave owner, string name, float x, float y, string file, int alpha)
    {
        canvas.AddInstance(owner, name, "Sprite", x, y);
        owner.DefaultState!.SetValue($"{name}.SourceFile", file, "string");
        owner.DefaultState.SetValue($"{name}.Alpha", alpha, "int");
    }
}

/// <summary>Reads an exported PNG's straight-alpha bytes and checks them against hand-computed values.</summary>
internal static class ExportedPixelChecks
{
    private const int Tolerance = 1;

    public sealed record Sample(string Name, (int X, int Y) At, byte R, byte G, byte B, byte A);

    /// <summary>
    /// Checks each sample exactly (within 1), and red text at alpha 128 inside <paramref name="text"/>: its
    /// fully covered glyph pixels are (255,0,0,128), so the most opaque pixel there must be exactly that,
    /// and every pixel at alpha 64 or more must still be pure red after un-premultiplying (within 3).
    /// </summary>
    public static void Check(string png, Sample[] samples, SkiaSharp.SKRectI text, string runName, string? keepFolder)
    {
        // Decode into an unpremultiplied bitmap so the PNG's own straight-alpha bytes are read as stored.
        using SkiaSharp.SKCodec codec = SkiaSharp.SKCodec.Create(png);
        SkiaSharp.SKImageInfo info = codec.Info.WithAlphaType(SkiaSharp.SKAlphaType.Unpremul);
        using SkiaSharp.SKBitmap image = new SkiaSharp.SKBitmap(info);
        codec.GetPixels(info, image.GetPixels()).ShouldBe(SkiaSharp.SKCodecResult.Success);

        List<string> report = new() { $"[{runName}]" };
        bool allMatch = true;
        foreach (Sample sample in samples)
        {
            SkiaSharp.SKColor pixel = image.GetPixel(sample.At.X, sample.At.Y);
            int delta = sample.A == 0
                ? pixel.Alpha
                : Math.Max(Math.Max(Math.Abs(pixel.Red - sample.R), Math.Abs(pixel.Green - sample.G)),
                    Math.Max(Math.Abs(pixel.Blue - sample.B), Math.Abs(pixel.Alpha - sample.A)));
            allMatch &= delta <= Tolerance;
            report.Add($"{sample.Name}: expected ({sample.R},{sample.G},{sample.B},{sample.A}) " +
                       $"actual ({pixel.Red},{pixel.Green},{pixel.Blue},{pixel.Alpha}) delta {delta}");
        }

        SkiaSharp.SKColor densest = default;
        int worstColor = 0;
        for (int y = text.Top; y < text.Bottom; y++)
        {
            for (int x = text.Left; x < text.Right; x++)
            {
                SkiaSharp.SKColor pixel = image.GetPixel(x, y);
                if (pixel.Alpha > densest.Alpha)
                {
                    densest = pixel;
                }
                if (pixel.Alpha >= 64)
                {
                    worstColor = Math.Max(worstColor, Math.Max(255 - pixel.Red, Math.Max(pixel.Green, pixel.Blue)));
                }
            }
        }
        int textDelta = Math.Max(Math.Max(Math.Abs(densest.Red - 255), Math.Max(densest.Green, densest.Blue)),
            Math.Abs(densest.Alpha - 128));
        allMatch &= textDelta <= Tolerance && worstColor <= 3;
        report.Add($"text, most opaque glyph pixel: expected (255,0,0,128) actual " +
                   $"({densest.Red},{densest.Green},{densest.Blue},{densest.Alpha}) delta {textDelta}");
        report.Add($"text, worst color off pure red at alpha >= 64: {worstColor} (limit 3)");

        if (!string.IsNullOrEmpty(keepFolder))
        {
            Directory.CreateDirectory(keepFolder);
            File.Copy(png, Path.Combine(keepFolder, $"export-{runName}.png"), overwrite: true);
            File.AppendAllLines(Path.Combine(keepFolder, "results.txt"), report);
        }
        allMatch.ShouldBeTrue(string.Join(Environment.NewLine, report));
    }
}
