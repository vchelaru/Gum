using CrossRuntimeTests;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace SkiaGum.Tests.CrossRuntime;

/// <summary>
/// The Skia nine-slice must draw what MonoGame draws (README.md in Tests/Shared/CrossRuntime). Every
/// case of <see cref="NineSliceCases"/> is rendered with Skia into a CPU surface and compared, pixel
/// by pixel, with the contact sheet MonoGame produced
/// (<c>NineSliceCrossRuntimeSheetTests</c> in MonoGameGum.IntegrationTests keeps those sheets true).
/// </summary>
public class NineSliceCrossRuntimeTests : BaseTestClass
{
    /// <summary>
    /// Cases where Skia is known to differ and the right answer is undecided (or MonoGame is the one
    /// that looks wrong), by "sheet/case". The test requires this list to be exactly the cases that
    /// differ, so a fix must remove its entry. Each entry has the reason beside it.
    /// </summary>
    private static readonly Dictionary<string, string> KnownDifferences = BuildKnownDifferences();

    // A band stretched or shrunk so that a destination pixel center lands exactly on a texel
    // boundary: MonoGame (GPU) picks the texel above/left of it, Skia's CPU raster the other. Read
    // from the pictures; not isolated to a Skia setting.
    private const string SamplingTie = "Nearest sampling tie on a texel boundary";

    // MonoGame's odd-height rule (NineSlice.cs: Height <= 2 * frame && Height % 2 == 1 makes the bottom
    // band one texel row shorter) counts world units, not pixels, and fires for any odd Height up to
    // twice the frame, not only for 1. At zoom 2 and 3 a 1 high nine-slice therefore draws half its
    // height. Checked by disabling the rule: MonoGame then matches Skia in exactly these cells.
    private const string MonoGameOddHeight = "MonoGame's odd-height rule drops a texel row (world units, not pixels)";

    // The odd-height rule above also changes MonoGame's picture in these cells, but each one differs
    // from Skia with the rule disabled as well, so the rule is involved and not the whole cause. The
    // rest is the same texel-boundary tie as SamplingTie; not isolated further.
    private const string SamplingTieWithOddHeight = "Nearest sampling tie on a texel boundary, and the odd-height rule changes MonoGame here";

    // At zoom 1.5 the nine-slice is 4.5 pixels; MonoGame draws 4 and Skia 5 pixels with different
    // edge texels. Which is right is undecided. (6x6-f2-3x3-z1.5 is also changed by the odd-height rule
    // but differs from Skia without it, like the other cells here.)
    private const string FractionalEdges = "Fractional pixel edges at zoom 1.5";

    private static Dictionary<string, string> BuildKnownDifferences()
    {
        Dictionary<string, string> known = new();
        void Add(string sheet, string reason, params string[] cases)
        {
            foreach (string name in cases)
            {
                known[$"{sheet}/{name}"] = reason;
            }
        }

        Add("frame1-3x16", SamplingTie, "1x3", "2x3", "3x3", "5x3", "1x5", "2x5", "3x5", "5x5");
        Add("frame2-6x6", SamplingTie, "5x1", "5x2", "5x3", "5x4", "1x5", "2x5", "3x5", "4x5", "5x5", "6x5", "9x5", "5x6", "5x9");
        Add("frame3-9x9", SamplingTie, "5x1", "3x6", "5x6", "3x7", "5x7", "3x12", "5x12");
        Add("frame3-9x9", SamplingTieWithOddHeight, "5x3", "3x5", "5x5");
        Add("borderscale0.5-6x6", SamplingTie,
            "2x1", "3x1", "4x1", "6x1", "1x2", "2x2", "3x2", "4x2", "6x2", "1x3", "2x3", "3x3", "4x3", "6x3",
            "1x4", "2x4", "3x4", "4x4", "6x4", "1x6", "2x6", "3x6", "4x6", "6x6");
        Add("zoom", SamplingTie, "3x16-f1-3x3-z1", "3x16-f1-5x5-z1", "6x6-f2-5x5-z1",
            "3x16-f1-5x5-z3", "6x6-f2-5x5-z3");
        Add("zoom", SamplingTieWithOddHeight, "6x6-f2-3x3-z2", "6x6-f2-3x3-z3");
        Add("zoom", MonoGameOddHeight, "3x16-f1-3x1-z2", "3x16-f1-2x1-z2", "3x16-f1-3x1-z3", "3x16-f1-2x1-z3");
        Add("zoom", FractionalEdges, "3x16-f1-3x1-z1.5", "3x16-f1-3x2-z1.5", "3x16-f1-3x3-z1.5", "3x16-f1-5x5-z1.5",
            "3x16-f1-2x1-z1.5", "6x6-f2-3x3-z1.5", "6x6-f2-4x4-z1.5", "6x6-f2-5x5-z1.5");
        Add("fractional", FractionalEdges, "3x3-f1-2.5x2.5-z1.5", "3x3-f1-3.5x3.5-z1.5");
        return known;
    }

    public static IEnumerable<object[]> SheetNames => NineSliceCases.Sheets.Select(sheet => new object[] { sheet.Name });

    [Fact]
    public void KnownDifferences_AllNameAnExistingSheet()
    {
        // The per-sheet stale check below only looks at keys under the sheet being compared, so a key
        // with a misspelled sheet would never be matched or flagged.
        HashSet<string> sheetNames = NineSliceCases.Sheets.Select(sheet => sheet.Name).ToHashSet();

        string[] orphaned = KnownDifferences.Keys
            .Where(key => !sheetNames.Contains(key.Substring(0, key.IndexOf('/'))))
            .ToArray();

        orphaned.ShouldBeEmpty("These KnownDifferences keys do not start with an existing sheet name.");
    }

    [Theory]
    [MemberData(nameof(SheetNames))]
    public void RenderedSheet_MatchesMonoGame(string sheetName)
    {
        NineSliceSheet sheet = NineSliceCases.Sheets.Single(candidate => candidate.Name == sheetName);
        List<string> names = sheet.Cases.Select(nineSliceCase => nineSliceCase.Name).ToList();

        using SKBitmap expected = SheetComparer.LoadBaseline(NineSliceCases.BaselinePath(sheet));
        (int sheetWidth, int sheetHeight) = NineSliceCases.SheetSize(sheet.Cases.Count);
        using SKBitmap actual = new(sheetWidth, sheetHeight);
        for (int index = 0; index < sheet.Cases.Count; index++)
        {
            using SKBitmap cell = RenderCase(sheet.Cases[index]);
            (int originX, int originY) = NineSliceCases.CellOrigin(index);
            for (int y = 0; y < NineSliceCases.CellSize; y++)
            {
                for (int x = 0; x < NineSliceCases.CellSize; x++)
                {
                    actual.SetPixel(originX + x, originY + y, cell.GetPixel(x, y));
                }
            }
        }

        List<CellDifference> differences = SheetComparer.Compare(
            expected, actual, names, NineSliceCases.CellSize, NineSliceCases.Columns);
        SheetComparer.WriteReports(sheetName, expected, actual, names, differences,
            NineSliceCases.CellSize, NineSliceCases.Columns);

        string[] unexpected = differences
            .Select(difference => $"{difference.Name} ({difference.DifferingPixels} px differ)")
            .Where(description => !KnownDifferences.ContainsKey($"{sheetName}/{description.Split(' ')[0]}"))
            .ToArray();
        unexpected.ShouldBeEmpty();

        string[] stale = KnownDifferences.Keys
            .Where(key => key.StartsWith(sheetName + "/", StringComparison.Ordinal))
            .Where(key => !differences.Any(difference => $"{sheetName}/{difference.Name}" == key))
            .ToArray();
        stale.ShouldBeEmpty("These known differences no longer differ; remove them from KnownDifferences.");
    }

    /// <summary>Draws one case into a <see cref="NineSliceCases.CellSize"/> square CPU surface.</summary>
    internal static SKBitmap RenderCase(NineSliceCase nineSliceCase)
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;

        int cell = NineSliceCases.CellSize;
        using SKSurface surface = SKSurface.Create(new SKImageInfo(cell, cell, SKColorType.Rgba8888, SKAlphaType.Premul));
        SystemManagers? previousDefault = SystemManagers.Default;
        SystemManagers managers = new();
        managers.Initialize();
        SystemManagers.Default = managers;
        managers.Canvas = surface.Canvas;
        managers.Renderer.ClearsCanvas = false;
        managers.Renderer.Camera.Zoom = nineSliceCase.Zoom;

        using SKBitmap texture = new(nineSliceCase.TextureWidth, nineSliceCase.TextureHeight);
        for (int y = 0; y < nineSliceCase.TextureHeight; y++)
        {
            for (int x = 0; x < nineSliceCase.TextureWidth; x++)
            {
                (byte r, byte g, byte b) = NineSliceCases.Texel(x, y);
                texture.SetPixel(x, y, new SKColor(r, g, b));
            }
        }

        NineSliceRuntime nineSlice = new();
        nineSlice.Texture = texture;
        nineSlice.CustomFrameTextureCoordinateWidth = nineSliceCase.Frame;
        nineSlice.IsTilingMiddleSections = nineSliceCase.Tiling;
        nineSlice.BorderScale = nineSliceCase.BorderScale;
        nineSlice.WidthUnits = DimensionUnitType.Absolute;
        nineSlice.HeightUnits = DimensionUnitType.Absolute;
        nineSlice.X = nineSliceCase.X;
        nineSlice.Y = nineSliceCase.Y;
        nineSlice.Width = nineSliceCase.Width;
        nineSlice.Height = nineSliceCase.Height;
        nineSlice.AddToManagers(managers, null);

        try
        {
            surface.Canvas.Clear(SKColors.Black);
            managers.Draw();
            using SKImage image = surface.Snapshot();
            return SKBitmap.FromImage(image);
        }
        finally
        {
            nineSlice.RemoveFromManagers();
            SystemManagers.Default = previousDefault;
        }
    }
}
