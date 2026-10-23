using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #6548: a [Custom] letter's ScaleX and ScaleY scale its cell about the origin point independently,
/// so a letter's rows depend only on ScaleY and its columns only on ScaleX.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class TextCustomLetterScaleTests : BaseTestClass
{
    private const int CaptureWidth = 160;
    private const int CaptureHeight = 120;

    [Theory]
    // Same ScaleY, different ScaleX: the lit rows must match.
    [InlineData(1f, 2f, 2f, 2f, true)]
    [InlineData(2f, 1f, 1f, 1f, true)]
    [InlineData(3f, 1.5f, 1.5f, 1.5f, true)]
    // Same ScaleX, different ScaleY: the lit columns must match.
    [InlineData(2f, 1f, 2f, 2f, false)]
    [InlineData(1f, 2f, 1f, 1f, false)]
    // Scale 1 on both axes against a ScaleY-only letter: no ScaleX, so the columns match.
    [InlineData(1f, 1f, 1f, 3f, false)]
    public void CustomLetter_RowsFollowScaleY_ColumnsFollowScaleX(
        float scaleXA, float scaleYA, float scaleXB, float scaleYB, bool compareRows)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;

        (int Start, int End) a = RenderLetterExtent(game.GraphicsDevice, managers, scaleXA, scaleYA, compareRows);
        (int Start, int End) b = RenderLetterExtent(game.GraphicsDevice, managers, scaleXB, scaleYB, compareRows);

        a.ShouldBe(b,
            $"({scaleXA}, {scaleYA}) and ({scaleXB}, {scaleYB}) share their {(compareRows ? "ScaleY" : "ScaleX")}, " +
            $"so the letter's lit {(compareRows ? "rows" : "columns")} must match");
    }

    private static (int Start, int End) RenderLetterExtent(GraphicsDevice gd, SystemManagers managers,
        float scaleX, float scaleY, bool rows)
    {
        string customizationName = $"Issue6548_{scaleX}_{scaleY}";
        Text.Customizations[customizationName] = (index, block) => new LetterCustomization
        {
            ScaleX = scaleX,
            ScaleY = scaleY,
            ScaleXOrigin = HorizontalAlignment.Left,
            ScaleYOrigin = VerticalAlignment.Top,
        };

        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.X = 40;
        text.Y = 30;
        text.Text = $"[Custom={customizationName}]H[/Custom]";
        text.AddToManagers(managers, null);

        try
        {
            text.UpdateLayout();
            XnaColor[] pixels = Render(gd, managers);
            return LitExtent(pixels, rows);
        }
        finally
        {
            text.RemoveFromManagers();
            Text.Customizations.Remove(customizationName);
        }
    }

    private static (int Start, int End) LitExtent(XnaColor[] pixels, bool rows)
    {
        int start = int.MaxValue;
        int end = -1;
        for (int y = 0; y < CaptureHeight; y++)
        {
            for (int x = 0; x < CaptureWidth; x++)
            {
                if (pixels[(y * CaptureWidth) + x].R > 0)
                {
                    int coordinate = rows ? y : x;
                    start = System.Math.Min(start, coordinate);
                    end = System.Math.Max(end, coordinate);
                }
            }
        }

        end.ShouldBeGreaterThanOrEqualTo(0, "the letter should draw at least one lit pixel");
        return (start, end);
    }

    private static XnaColor[] Render(GraphicsDevice gd, SystemManagers managers)
    {
        using RenderTarget2D capture = new(gd, CaptureWidth, CaptureHeight, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] data = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(data);
        return data;
    }

    private sealed class MinimalGame : SharedGameLease
    {
        protected override void OnInitialize() => Gum.GumService.Default.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
        }
    }
}
