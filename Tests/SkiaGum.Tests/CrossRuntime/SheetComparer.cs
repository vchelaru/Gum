using SkiaSharp;

namespace SkiaGum.Tests.CrossRuntime;

/// <summary>One cell of a contact sheet whose Skia pixels differ from the reference's.</summary>
public sealed record CellDifference(int Index, string Name, int DifferingPixels, int MaxChannelDifference);

/// <summary>
/// Compares a Skia-rendered contact sheet with the MonoGame-rendered baseline cell by cell and writes
/// the picture of every cell that differs. See README.md in Tests/Shared/CrossRuntime.
/// </summary>
public static class SheetComparer
{
    /// <summary>A pixel counts as different when any RGB channel is further apart than this.</summary>
    public const byte DefaultTolerance = 2;

    /// <summary>Set to a folder to have <see cref="WriteReports"/> write the pictures there.</summary>
    public const string OutputEnvironmentVariable = "GUM_COMPARE_OUT";

    public static SKBitmap LoadBaseline(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"No baseline at {path}. Run the MonoGame sheet test with GUM_UPDATE_CROSS_RUNTIME_BASELINES=1.");
        }
        return SKBitmap.Decode(path) ?? throw new InvalidOperationException($"Could not decode {path}.");
    }

    /// <summary>Returns the cells whose RGB differs by more than <paramref name="tolerance"/> in any pixel.</summary>
    public static List<CellDifference> Compare(
        SKBitmap expected, SKBitmap actual, IReadOnlyList<string> names, int cellSize, int columns,
        byte tolerance = DefaultTolerance)
    {
        if (expected.Width != actual.Width || expected.Height != actual.Height)
        {
            throw new InvalidOperationException(
                $"Sheet sizes differ: baseline {expected.Width}x{expected.Height}, actual {actual.Width}x{actual.Height}.");
        }

        List<CellDifference> differences = new();
        for (int index = 0; index < names.Count; index++)
        {
            int originX = index % columns * cellSize;
            int originY = index / columns * cellSize;
            int differing = 0;
            int maxDifference = 0;
            for (int y = 0; y < cellSize; y++)
            {
                for (int x = 0; x < cellSize; x++)
                {
                    int difference = MaxChannelDifference(
                        expected.GetPixel(originX + x, originY + y), actual.GetPixel(originX + x, originY + y));
                    if (difference > tolerance)
                    {
                        differing++;
                        maxDifference = Math.Max(maxDifference, difference);
                    }
                }
            }
            if (differing > 0)
            {
                differences.Add(new CellDifference(index, names[index], differing, maxDifference));
            }
        }
        return differences;
    }

    /// <summary>
    /// When <see cref="OutputEnvironmentVariable"/> names a folder, writes <c>skia/&lt;sheet&gt;.png</c>,
    /// <c>sheets/&lt;sheet&gt;.png</c> (reference | Skia | difference side by side) and, for each
    /// differing cell, <c>differences/&lt;sheet&gt;/&lt;case&gt;.png</c> (reference | Skia | difference, magnified).
    /// </summary>
    public static void WriteReports(
        string sheetName, SKBitmap expected, SKBitmap actual, IReadOnlyList<string> names,
        IReadOnlyList<CellDifference> differences, int cellSize, int columns, byte tolerance = DefaultTolerance)
    {
        string? outputDirectory = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
        if (string.IsNullOrEmpty(outputDirectory))
        {
            return;
        }

        SavePng(Path.Combine(outputDirectory, "skia", sheetName + ".png"), actual);

        using SKBitmap diff = DiffImage(expected, actual, tolerance);
        const int sheetScale = 3;
        using SKBitmap overview = new(expected.Width * sheetScale * 3 + 40, expected.Height * sheetScale + 20);
        using (SKCanvas canvas = new(overview))
        {
            canvas.Clear(new SKColor(40, 40, 40));
            Magnify(canvas, expected, 10, 10, sheetScale);
            Magnify(canvas, actual, 20 + expected.Width * sheetScale, 10, sheetScale);
            Magnify(canvas, diff, 30 + expected.Width * sheetScale * 2, 10, sheetScale);
        }
        SavePng(Path.Combine(outputDirectory, "sheets", sheetName + ".png"), overview);
        WriteOverview(outputDirectory);

        const int cellScale = 10;
        foreach (CellDifference difference in differences)
        {
            int originX = difference.Index % columns * cellSize;
            int originY = difference.Index / columns * cellSize;
            using SKBitmap expectedCell = Crop(expected, originX, originY, cellSize);
            using SKBitmap actualCell = Crop(actual, originX, originY, cellSize);
            using SKBitmap diffCell = Crop(diff, originX, originY, cellSize);

            int cellPixels = cellSize * cellScale;
            using SKBitmap picture = new(cellPixels * 3 + 40, cellPixels + 40);
            using (SKCanvas canvas = new(picture))
            {
                canvas.Clear(new SKColor(40, 40, 40));
                Magnify(canvas, expectedCell, 10, 30, cellScale);
                Magnify(canvas, actualCell, 20 + cellPixels, 30, cellScale);
                Magnify(canvas, diffCell, 30 + cellPixels * 2, 30, cellScale);
                using SKPaint paint = new() { Color = SKColors.White, IsAntialias = true };
                using SKFont font = new() { Size = 14 };
                canvas.DrawText(
                    $"{sheetName} {difference.Name}   MonoGame | Skia | diff ({difference.DifferingPixels} px)",
                    10, 20, SKTextAlign.Left, font, paint);
            }
            string safeName = string.Concat(difference.Name.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' ? c : '_'));
            SavePng(Path.Combine(outputDirectory, "differences", sheetName, safeName + ".png"), picture);
        }
    }

    // Stacks every sheet picture written so far into overview.png, so one image shows the whole matrix.
    private static void WriteOverview(string outputDirectory)
    {
        string[] files = Directory.GetFiles(Path.Combine(outputDirectory, "sheets"), "*.png").OrderBy(f => f).ToArray();
        List<SKBitmap> pictures = files.Select(file => SKBitmap.Decode(file)).ToList();
        try
        {
            using SKBitmap overview = new(pictures.Max(p => p.Width), pictures.Sum(p => p.Height));
            using (SKCanvas canvas = new(overview))
            {
                canvas.Clear(new SKColor(40, 40, 40));
                int y = 0;
                foreach (SKBitmap picture in pictures)
                {
                    canvas.DrawBitmap(picture, 0, y, new SKSamplingOptions());
                    y += picture.Height;
                }
            }
            SavePng(Path.Combine(outputDirectory, "overview.png"), overview);
        }
        finally
        {
            pictures.ForEach(p => p.Dispose());
        }
    }

    private static int MaxChannelDifference(SKColor a, SKColor b) =>
        Math.Max(Math.Abs(a.Red - b.Red), Math.Max(Math.Abs(a.Green - b.Green), Math.Abs(a.Blue - b.Blue)));

    // Differing pixels red, matching pixels dimmed to the reference.
    private static SKBitmap DiffImage(SKBitmap expected, SKBitmap actual, byte tolerance)
    {
        SKBitmap diff = new(expected.Width, expected.Height);
        for (int y = 0; y < expected.Height; y++)
        {
            for (int x = 0; x < expected.Width; x++)
            {
                SKColor e = expected.GetPixel(x, y);
                bool differs = MaxChannelDifference(e, actual.GetPixel(x, y)) > tolerance;
                diff.SetPixel(x, y, differs ? new SKColor(255, 0, 0) : new SKColor((byte)(e.Red / 4), (byte)(e.Green / 4), (byte)(e.Blue / 4)));
            }
        }
        return diff;
    }

    private static SKBitmap Crop(SKBitmap source, int originX, int originY, int size)
    {
        SKBitmap crop = new(size, size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                crop.SetPixel(x, y, source.GetPixel(originX + x, originY + y));
            }
        }
        return crop;
    }

    private static void Magnify(SKCanvas canvas, SKBitmap source, int x, int y, int scale)
    {
        using SKImage image = SKImage.FromBitmap(source);
        canvas.DrawImage(
            image, new SKRect(0, 0, source.Width, source.Height),
            new SKRect(x, y, x + source.Width * scale, y + source.Height * scale),
            new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None));
    }

    private static void SavePng(string path, SKBitmap bitmap)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using FileStream stream = File.Create(path);
        data.SaveTo(stream);
    }
}
