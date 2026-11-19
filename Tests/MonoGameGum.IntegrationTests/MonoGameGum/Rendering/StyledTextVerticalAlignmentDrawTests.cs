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
/// Vertical alignment of multi-line BBCode text whose lines have different sizes ([FontScale], [FontSize]).
/// The text is drawn Top-aligned at its own RelativeToChildren height, then in a box
/// <see cref="ExtraHeight"/> taller: Bottom must move every pixel down by the whole extra height and Center by half,
/// which only holds when the alignment uses the same (styled) content height the layout does.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class StyledTextVerticalAlignmentDrawTests : BaseTestClass
{
    private const int CaptureWidth = 220;
    private const int CaptureHeight = 300;
    private const int ExtraHeight = 60;

    [Theory]
    [InlineData("ab\n[FontScale=2]cd[/FontScale]\nef", VerticalAlignment.Bottom, ExtraHeight)]
    [InlineData("ab\n[FontScale=2]cd[/FontScale]\nef", VerticalAlignment.Center, ExtraHeight / 2)]
    [InlineData("[FontScale=2]ab[/FontScale]\ncd", VerticalAlignment.Bottom, ExtraHeight)]
    [InlineData("[FontScale=2]ab[/FontScale]\ncd", VerticalAlignment.Center, ExtraHeight / 2)]
    [InlineData("ab\ncd[FontScale=3]e[/FontScale]", VerticalAlignment.Bottom, ExtraHeight)]
    [InlineData("ab\n[FontScale=0.5]cd[/FontScale]\nef", VerticalAlignment.Bottom, ExtraHeight)]
    [InlineData("[Color=Red]ab[/Color]\ncd", VerticalAlignment.Bottom, ExtraHeight)]
    public void Draw_MixedSizeLines_AlignmentMovesTheTextByTheSpareHeight(string markup, VerticalAlignment alignment, int expectedShift)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        float naturalHeight;
        int topInk = InkTop(game.GraphicsDevice, markup, VerticalAlignment.Top, height: null, out naturalHeight);
        int alignedInk = InkTop(game.GraphicsDevice, markup, alignment, naturalHeight + ExtraHeight, out _);

        (alignedInk - topInk).ShouldBe(expectedShift,
            $"'{markup}' is {naturalHeight} tall in a {naturalHeight + ExtraHeight} box, aligned {alignment}");
    }

    private static int InkTop(GraphicsDevice gd, string markup, VerticalAlignment alignment, float? height, out float layoutHeight)
    {
        SystemManagers managers = SystemManagers.Default;
        TextRuntime text = new();
        text.X = 10;
        text.Y = 10;
        text.Text = markup;
        text.VerticalAlignment = alignment;
        if (height == null)
        {
            text.HeightUnits = DimensionUnitType.RelativeToChildren;
            text.Height = 0;
        }
        else
        {
            text.HeightUnits = DimensionUnitType.Absolute;
            text.Height = height.Value;
        }
        text.AddToManagers(managers, null);
        try
        {
            text.UpdateLayout();
            layoutHeight = text.GetAbsoluteHeight();

            using RenderTarget2D capture = new(gd, CaptureWidth, CaptureHeight, false, SurfaceFormat.Color,
                DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
            gd.SetRenderTarget(null);

            XnaColor[] data = new XnaColor[CaptureWidth * CaptureHeight];
            capture.GetData(data);
            for (int y = 0; y < CaptureHeight; y++)
            {
                for (int x = 0; x < CaptureWidth; x++)
                {
                    XnaColor p = data[(y * CaptureWidth) + x];
                    if (p.R > 0 || p.G > 0 || p.B > 0)
                    {
                        return y;
                    }
                }
            }
            throw new ShouldAssertException("the text drew no pixels");
        }
        finally
        {
            text.RemoveFromManagers();
        }
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
