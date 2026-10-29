using System.Runtime.CompilerServices;
using static System.FormattableString;

namespace CrossRuntimeTests;

/// <summary>
/// One nine-slice drawn on a <see cref="NineSliceCases.CellSize"/> square canvas by every runtime that
/// takes part in a cross-runtime comparison (see README.md in this folder). Linked into
/// MonoGameGum.IntegrationTests and SkiaGum.Tests so both render exactly the same inputs.
/// </summary>
/// <param name="Name">Unique within its sheet; names the cell in reports.</param>
/// <param name="TextureWidth">Texels across the generated texture (see <see cref="NineSliceCases.Texel"/>).</param>
/// <param name="TextureHeight">Texels down the generated texture.</param>
/// <param name="Frame">CustomFrameTextureCoordinateWidth; null leaves the default (a third of the texture).</param>
/// <param name="Width">Nine-slice width in world units.</param>
/// <param name="Height">Nine-slice height in world units.</param>
/// <param name="X">World X of the nine-slice's top-left corner.</param>
/// <param name="Y">World Y of the nine-slice's top-left corner.</param>
/// <param name="Zoom">Camera zoom, with the camera's top-left at the world origin.</param>
/// <param name="Tiling">IsTilingMiddleSections.</param>
/// <param name="BorderScale">Multiplier on the border thickness.</param>
public sealed record NineSliceCase(
    string Name, int TextureWidth, int TextureHeight, int? Frame,
    float Width, float Height,
    float X = 2, float Y = 2, float Zoom = 1, bool Tiling = false, float BorderScale = 1);

/// <summary>A named group of cases, drawn into one contact sheet PNG.</summary>
public sealed record NineSliceSheet(string Name, IReadOnlyList<NineSliceCase> Cases);

public static class NineSliceCases
{
    /// <summary>Side of the square canvas each case draws on, in screen pixels.</summary>
    public const int CellSize = 32;

    /// <summary>Cells per row in a contact sheet.</summary>
    public const int Columns = 8;

    /// <summary>
    /// Every texel gets its own opaque color, so a band sampling the wrong texels shows as a
    /// different pixel and not as a slightly different blur.
    /// </summary>
    public static (byte R, byte G, byte B) Texel(int x, int y) => (
        (byte)(40 + (x * 53) % 200),
        (byte)(40 + (y * 37) % 200),
        (byte)(60 + ((x + y) * 29) % 150));

    private static string ThisFile([CallerFilePath] string path = "") => path;

    /// <summary>The source folder holding the MonoGame-rendered contact sheets (the baselines).</summary>
    public static string BaselineDirectory { get; } = Path.Combine(Path.GetDirectoryName(ThisFile())!, "NineSliceBaselines");

    public static string BaselinePath(NineSliceSheet sheet) => Path.Combine(BaselineDirectory, sheet.Name + ".png");

    public static IReadOnlyList<NineSliceSheet> Sheets { get; } = BuildSheets();

    public static (int Width, int Height) SheetSize(int caseCount)
    {
        int rows = (caseCount + Columns - 1) / Columns;
        return (Columns * CellSize, rows * CellSize);
    }

    public static (int X, int Y) CellOrigin(int index) => (index % Columns * CellSize, index / Columns * CellSize);

    // Case names key the KnownDifferences lists, so floats in them are written with the invariant culture
    // (Invariant($"...")); a machine that writes 1,5 would otherwise fail every fractional case.
    public static List<NineSliceSheet> BuildSheets()
    {
        List<NineSliceSheet> sheets = new();

        // Rudy's texture in #7029: 3x16, sliced at 1.
        sheets.Add(Grid("frame1-3x16", 3, 16, 1,
            new float[] { 0, 1, 2, 3, 5 }, new float[] { 0, 1, 2, 3, 4, 5, 8 }));

        // Default frame (a third of the texture).
        sheets.Add(Grid("default-3x3", 3, 3, null,
            new float[] { 1, 2, 3, 4, 5, 8 }, new float[] { 1, 2, 3, 4, 5, 8 }));

        sheets.Add(Grid("frame2-6x6", 6, 6, 2,
            new float[] { 1, 2, 3, 4, 5, 6, 9 }, new float[] { 1, 2, 3, 4, 5, 6, 9 }));

        // A frame of 0 draws only the center.
        sheets.Add(Grid("frame0-3x3", 3, 3, 0,
            new float[] { 0, 1, 2, 5 }, new float[] { 0, 1, 2, 5 }));

        sheets.Add(Grid("frame3-9x9", 9, 9, 3,
            new float[] { 1, 3, 5, 6, 7, 12 }, new float[] { 1, 3, 5, 6, 7, 12 }));

        List<NineSliceCase> zoom = new();
        foreach (float z in new[] { 1f, 1.5f, 2f, 3f })
        {
            foreach ((float w, float h) in new[] { (3f, 1f), (3f, 2f), (3f, 3f), (5f, 5f), (2f, 1f) })
            {
                zoom.Add(new NineSliceCase(Invariant($"3x16-f1-{w}x{h}-z{z}"), 3, 16, 1, w, h, Zoom: z));
            }
            foreach (float s in new[] { 3f, 4f, 5f })
            {
                zoom.Add(new NineSliceCase(Invariant($"6x6-f2-{s}x{s}-z{z}"), 6, 6, 2, s, s, Zoom: z));
            }
        }
        sheets.Add(new NineSliceSheet("zoom", zoom));

        List<NineSliceCase> fractional = new();
        float[] fractions = { 0.5f, 1.5f, 2.5f, 3.5f, 7.5f };
        foreach (float w in fractions)
        {
            foreach (float h in fractions)
            {
                fractional.Add(new NineSliceCase(Invariant($"3x3-f1-{w}x{h}"), 3, 3, 1, w, h));
            }
        }
        foreach (float s in fractions)
        {
            fractional.Add(new NineSliceCase(Invariant($"3x3-f1-{s}x{s}-at2.5"), 3, 3, 1, s, s, X: 2.5f, Y: 2.5f));
        }
        foreach (float s in new[] { 1.5f, 2.5f, 3.5f })
        {
            fractional.Add(new NineSliceCase(Invariant($"3x3-f1-{s}x{s}-z1.5"), 3, 3, 1, s, s, X: 2.5f, Y: 2.5f, Zoom: 1.5f));
        }
        sheets.Add(new NineSliceSheet("fractional", fractional));

        sheets.Add(Grid("tiling-3x3", 3, 3, 1,
            new float[] { 1, 2, 3, 5, 9 }, new float[] { 1, 2, 3, 5, 9 }, tiling: true));
        sheets.Add(Grid("borderscale2-3x3", 3, 3, 1,
            new float[] { 1, 2, 3, 4, 5 }, new float[] { 1, 2, 3, 4, 5 }, borderScale: 2));
        sheets.Add(Grid("borderscale0.5-6x6", 6, 6, 2,
            new float[] { 1, 2, 3, 4, 6 }, new float[] { 1, 2, 3, 4, 6 }, borderScale: 0.5f));

        return sheets;
    }

    private static NineSliceSheet Grid(string name, int textureWidth, int textureHeight, int? frame,
        float[] widths, float[] heights, bool tiling = false, float borderScale = 1)
    {
        List<NineSliceCase> cases = new();
        foreach (float height in heights)
        {
            foreach (float width in widths)
            {
                cases.Add(new NineSliceCase(Invariant($"{width}x{height}"), textureWidth, textureHeight, frame, width, height,
                    Tiling: tiling, BorderScale: borderScale));
            }
        }
        return new NineSliceSheet(name, cases);
    }
}
