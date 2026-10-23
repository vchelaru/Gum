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
/// Issue #6549: styled (BBCode) text places its later runs and lines along the rotated axes, so it
/// draws the same pixels as the same text without styling at any rotation, and [Custom] letter offsets
/// move along the rotated axes.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class StyledTextRotationTests : BaseTestClass
{
    private const int CaptureSize = 200;

    [Theory]
    [InlineData(0f, "[Color=White]HH[/Color]HH", "HHHH")]
    [InlineData(30f, "[Color=White]HH[/Color]HH", "HHHH")]
    [InlineData(90f, "[Color=White]HH[/Color]HH", "HHHH")]
    [InlineData(30f, "[Color=White]HH[/Color]\nHH", "HH\nHH")]
    [InlineData(90f, "[Color=White]HH[/Color]\nHH", "HH\nHH")]
    public void StyledText_Rotated_DrawsWhereUnstyledTextDraws(float rotation, string styled, string plain)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;

        (int Left, int Top, int Right, int Bottom) styledBounds =
            RenderLitBounds(game.GraphicsDevice, managers, styled, rotation);
        (int Left, int Top, int Right, int Bottom) plainBounds =
            RenderLitBounds(game.GraphicsDevice, managers, plain, rotation);

        // No slack: each run and line starts where the same letter starts in the plain text (#6575).
        styledBounds.ShouldBe(plainBounds, $"styled {styledBounds} plain {plainBounds}");
    }

    [Theory]
    [InlineData(0f, "[Color=Red]HH[/Color][Color=Lime]HH[/Color]", "HHHH")]
    [InlineData(0f, "[Color=Red]H[/Color][Color=Lime]H[/Color][Color=Blue]H[/Color]H", "HHHH")]
    [InlineData(0f, "[Color=Red]HH[/Color] HH", "HH HH")]
    [InlineData(90f, "[Color=Red]HH[/Color][Color=Lime]HH[/Color]", "HHHH")]
    [InlineData(180f, "[Color=Red]HH[/Color][Color=Lime]HH[/Color]", "HHHH")]
    [InlineData(30f, "[Color=Red]HH[/Color][Color=Lime]HH[/Color]", "HHHH")]
    public void StyledRuns_LightExactlyThePixelsUnstyledTextLights(float rotation, string styled, string plain)
    {
        // Issue #6575: a run advances by its letters' full XAdvance, which is where the same letter starts
        // in the plain line, so the lit pixels match exactly with no per-run drift. The measured width
        // trims only the line's last glyph (TrimRight), so it is shorter than a run's advance by design.
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;

        bool[] styledLit = RenderLitMask(game.GraphicsDevice, managers, styled, rotation, out float styledWidth);
        bool[] plainLit = RenderLitMask(game.GraphicsDevice, managers, plain, rotation, out float plainWidth);

        styledWidth.ShouldBe(plainWidth);

        int litCount = 0;
        for (int i = 0; i < plainLit.Length; i++)
        {
            if (plainLit[i])
            {
                litCount++;
            }
            styledLit[i].ShouldBe(plainLit[i], $"pixel ({i % CaptureSize}, {i / CaptureSize})");
        }
        litCount.ShouldBeGreaterThan(0);
    }

    [Theory]
    // XOffset 10 and YOffset 6 move along the text's own axes: unrotated that is (+10, +6) on screen,
    // at 90 degrees the x axis points up and the y axis right, so (+6, -10).
    [InlineData(0f, 10, 6)]
    [InlineData(90f, 6, -10)]
    public void CustomLetterOffset_Rotated_MovesAlongTextAxes(float rotation, int expectedScreenX, int expectedScreenY)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;

        string customizationName = $"Issue6549_{rotation}";
        Text.Customizations[customizationName] = (index, block) => new LetterCustomization
        {
            XOffset = 10,
            YOffset = 6,
        };

        try
        {
            (int Left, int Top, int Right, int Bottom) offsetBounds = RenderLitBounds(game.GraphicsDevice, managers,
                $"[Custom={customizationName}]HHHH[/Custom]", rotation);
            (int Left, int Top, int Right, int Bottom) plainBounds = RenderLitBounds(game.GraphicsDevice, managers,
                "HHHH", rotation);

            System.Math.Abs(offsetBounds.Left - plainBounds.Left - expectedScreenX).ShouldBeLessThanOrEqualTo(1, $"offset {offsetBounds} plain {plainBounds}");
            System.Math.Abs(offsetBounds.Right - plainBounds.Right - expectedScreenX).ShouldBeLessThanOrEqualTo(1, $"offset {offsetBounds} plain {plainBounds}");
            System.Math.Abs(offsetBounds.Top - plainBounds.Top - expectedScreenY).ShouldBeLessThanOrEqualTo(1, $"offset {offsetBounds} plain {plainBounds}");
            System.Math.Abs(offsetBounds.Bottom - plainBounds.Bottom - expectedScreenY).ShouldBeLessThanOrEqualTo(1, $"offset {offsetBounds} plain {plainBounds}");
        }
        finally
        {
            Text.Customizations.Remove(customizationName);
        }
    }

    private static bool[] RenderLitMask(GraphicsDevice gd, SystemManagers managers, string value, float rotation,
        out float width)
    {
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.X = 100;
        text.Y = 100;
        text.Rotation = rotation;
        text.Text = value;
        text.AddToManagers(managers, null);

        try
        {
            text.UpdateLayout();
            width = text.GetAbsoluteWidth();
            XnaColor[] pixels = Render(gd, managers);
            bool[] lit = new bool[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                lit[i] = pixels[i].R > 0 || pixels[i].G > 0 || pixels[i].B > 0;
            }
            return lit;
        }
        finally
        {
            text.RemoveFromManagers();
        }
    }

    private static (int Left, int Top, int Right, int Bottom) RenderLitBounds(GraphicsDevice gd, SystemManagers managers,
        string value, float rotation)
    {
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.X = 100;
        text.Y = 100;
        text.Rotation = rotation;
        text.Text = value;
        text.AddToManagers(managers, null);

        try
        {
            text.UpdateLayout();
            XnaColor[] pixels = Render(gd, managers);

            int left = int.MaxValue, top = int.MaxValue, right = -1, bottom = -1;
            for (int y = 0; y < CaptureSize; y++)
            {
                for (int x = 0; x < CaptureSize; x++)
                {
                    if (pixels[(y * CaptureSize) + x].R > 0)
                    {
                        left = System.Math.Min(left, x);
                        top = System.Math.Min(top, y);
                        right = System.Math.Max(right, x);
                        bottom = System.Math.Max(bottom, y);
                    }
                }
            }

            right.ShouldBeGreaterThanOrEqualTo(0, "the text should draw at least one lit pixel");
            return (left, top, right, bottom);
        }
        finally
        {
            text.RemoveFromManagers();
        }
    }

    private static XnaColor[] Render(GraphicsDevice gd, SystemManagers managers)
    {
        using RenderTarget2D capture = new(gd, CaptureSize, CaptureSize, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] data = new XnaColor[CaptureSize * CaptureSize];
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
