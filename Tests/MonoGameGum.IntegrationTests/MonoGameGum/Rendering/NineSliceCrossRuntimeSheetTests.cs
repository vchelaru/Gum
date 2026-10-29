using CrossRuntimeTests;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// MonoGame is the reference the Skia nine-slice is compared against (README.md in
/// Tests/Shared/CrossRuntime). This class renders every case of <see cref="NineSliceCases"/> with
/// MonoGame and checks the result against the checked-in contact sheets, which SkiaGum.Tests
/// (<c>NineSliceCrossRuntimeTests</c>) reads. A MonoGame change that moves a pixel fails here, so the
/// sheets cannot go stale; regenerate them with <c>GUM_UPDATE_CROSS_RUNTIME_BASELINES=1</c> only for
/// an intended MonoGame change, and review the diff of the PNGs. <c>GUM_COMPARE_OUT=&lt;dir&gt;</c>
/// also writes each sheet to <c>&lt;dir&gt;/monogame</c>.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class NineSliceCrossRuntimeSheetTests : BaseTestClass
{
    public static IEnumerable<object[]> SheetNames => NineSliceCases.Sheets.Select(sheet => new object[] { sheet.Name });

    [Theory]
    [MemberData(nameof(SheetNames))]
    public void RenderedSheet_MatchesTheCheckedInBaseline(string sheetName)
    {
        NineSliceSheet sheet = NineSliceCases.Sheets.Single(candidate => candidate.Name == sheetName);
        using MinimalGame game = new();
        game.RunOneFrame();

        (int sheetWidth, int sheetHeight) = NineSliceCases.SheetSize(sheet.Cases.Count);
        Color[] sheetPixels = new Color[sheetWidth * sheetHeight];
        for (int index = 0; index < sheet.Cases.Count; index++)
        {
            Color[] cell = RenderCase(game.GraphicsDevice, sheet.Cases[index]);
            (int originX, int originY) = NineSliceCases.CellOrigin(index);
            for (int y = 0; y < NineSliceCases.CellSize; y++)
            {
                Array.Copy(cell, y * NineSliceCases.CellSize, sheetPixels, ((originY + y) * sheetWidth) + originX,
                    NineSliceCases.CellSize);
            }
        }

        string baselinePath = NineSliceCases.BaselinePath(sheet);
        string? outDirectory = Environment.GetEnvironmentVariable("GUM_COMPARE_OUT");
        if (!string.IsNullOrEmpty(outDirectory))
        {
            SavePng(game.GraphicsDevice, Path.Combine(outDirectory, "monogame", sheet.Name + ".png"), sheetPixels, sheetWidth, sheetHeight);
        }

        if (Environment.GetEnvironmentVariable("GUM_UPDATE_CROSS_RUNTIME_BASELINES") == "1")
        {
            SavePng(game.GraphicsDevice, baselinePath, sheetPixels, sheetWidth, sheetHeight);
            return;
        }

        File.Exists(baselinePath).ShouldBeTrue($"No baseline at {baselinePath}; run with GUM_UPDATE_CROSS_RUNTIME_BASELINES=1.");
        using FileStream stream = File.OpenRead(baselinePath);
        using Texture2D baseline = Texture2D.FromStream(game.GraphicsDevice, stream);
        baseline.Width.ShouldBe(sheetWidth);
        baseline.Height.ShouldBe(sheetHeight);
        Color[] expected = new Color[sheetWidth * sheetHeight];
        baseline.GetData(expected);

        List<string> differing = new();
        for (int index = 0; index < sheet.Cases.Count; index++)
        {
            (int originX, int originY) = NineSliceCases.CellOrigin(index);
            bool differs = false;
            for (int y = 0; y < NineSliceCases.CellSize && !differs; y++)
            {
                for (int x = 0; x < NineSliceCases.CellSize && !differs; x++)
                {
                    int i = ((originY + y) * sheetWidth) + originX + x;
                    differs = (expected[i].R, expected[i].G, expected[i].B) != (sheetPixels[i].R, sheetPixels[i].G, sheetPixels[i].B);
                }
            }
            if (differs)
            {
                differing.Add(sheet.Cases[index].Name);
            }
        }
        differing.ShouldBeEmpty();
    }

    private static Color[] RenderCase(GraphicsDevice graphicsDevice, NineSliceCase nineSliceCase)
    {
        SystemManagers managers = SystemManagers.Default;
        int cell = NineSliceCases.CellSize;

        using Texture2D texture = new(graphicsDevice, nineSliceCase.TextureWidth, nineSliceCase.TextureHeight);
        Color[] texels = new Color[nineSliceCase.TextureWidth * nineSliceCase.TextureHeight];
        for (int y = 0; y < nineSliceCase.TextureHeight; y++)
        {
            for (int x = 0; x < nineSliceCase.TextureWidth; x++)
            {
                (byte r, byte g, byte b) = NineSliceCases.Texel(x, y);
                texels[(y * nineSliceCase.TextureWidth) + x] = new Color(r, g, b, (byte)255);
            }
        }
        texture.SetData(texels);

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

        ContainerRuntime root = new();
        root.AddChild(nineSlice);
        root.AddToManagers(managers, null);
        root.UpdateLayout();

        Camera camera = managers.Renderer.Camera;
        CameraCenterOnScreen previousCenter = camera.CameraCenterOnScreen;
        float previousZoom = camera.Zoom;
        float previousX = camera.X;
        float previousY = camera.Y;
        camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;
        camera.X = 0;
        camera.Y = 0;
        camera.Zoom = nineSliceCase.Zoom;

        try
        {
            using RenderTarget2D capture = new(graphicsDevice, cell, cell, false, SurfaceFormat.Color, DepthFormat.None, 0,
                RenderTargetUsage.PreserveContents);
            for (int i = 0; i < 2; i++)
            {
                graphicsDevice.SetRenderTarget(capture);
                graphicsDevice.Clear(Color.Black);
                managers.Renderer.Draw(managers);
            }
            graphicsDevice.SetRenderTarget(null);

            Color[] pixels = new Color[cell * cell];
            capture.GetData(pixels);
            return pixels;
        }
        finally
        {
            camera.Zoom = previousZoom;
            camera.X = previousX;
            camera.Y = previousY;
            camera.CameraCenterOnScreen = previousCenter;
            root.RemoveFromManagers();
        }
    }

    private static void SavePng(GraphicsDevice graphicsDevice, string path, Color[] pixels, int width, int height)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using Texture2D texture = new(graphicsDevice, width, height);
        texture.SetData(pixels);
        using FileStream stream = File.Create(path);
        texture.SaveAsPng(stream, width, height);
    }

    private sealed class MinimalGame : SharedGameLease
    {
        public GumService GumService { get; } = new GumService();

        protected override void OnInitialize() => GumService.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
        }
    }
}
