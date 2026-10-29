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
    private static IReadOnlyDictionary<string, KnownDifference> KnownDifferences => NineSliceKnownDifferences.All;

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
