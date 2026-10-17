using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #6112: moving a text block by a sub-pixel amount at a non-100% camera zoom must not change
/// how its letters sit relative to each other. Every shifted render has to be the reference render
/// translated by a whole number of screen pixels; any other difference means a letter hopped on its
/// own (uneven spacing or a baseline that no longer lines up).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class TextSubpixelShiftTests : BaseTestClass
{
    private const int CaptureWidth = 240;
    private const int CaptureHeight = 120;
    private const int MaxWholePixelTranslation = 5;
    private const string MultiLineText = "Hamburgefonstiv 0123\nQuick brown fox jumps\nover the lazy dog, Wyg";

    [Theory]
    [InlineData(1f)]
    [InlineData(0.75f)]
    [InlineData(1.5f)]
    public void ShiftedTextBlock_IsWholePixelTranslationOfReference_AtZoom(float zoom)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        managers.Renderer.Camera.Zoom = zoom;

        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Text = MultiLineText;
        text.AddToManagers(managers, null);

        try
        {
            XnaColor[] reference = RenderAt(game.GraphicsDevice, managers, text, 20f, 20f);

            float[] shifts = { 0.1f, 0.25f, 0.4f, 0.5f, 0.6f, 0.75f, 0.9f, 1.1f, 1.33f, 2.7f };
            List<string> failures = new();

            foreach (float shiftX in shifts)
            {
                foreach (float shiftY in shifts)
                {
                    XnaColor[] shifted = RenderAt(game.GraphicsDevice, managers, text, 20f + shiftX, 20f + shiftY);

                    if (!MatchesReferenceTranslatedByWholePixels(reference, shifted))
                    {
                        failures.Add($"({shiftX}, {shiftY})");
                    }
                }
            }

            failures.ShouldBeEmpty(
                $"at zoom {zoom} these shifts changed the letters relative to each other, " +
                $"not just moved the whole block by whole pixels: {string.Join(", ", failures)}");
        }
        finally
        {
            text.RemoveFromManagers();
            managers.Renderer.Camera.Zoom = 1f;
        }
    }

    private static XnaColor[] RenderAt(GraphicsDevice gd, SystemManagers managers, TextRuntime text, float x, float y)
    {
        text.X = x;
        text.Y = y;
        text.UpdateLayout();

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

    private static bool MatchesReferenceTranslatedByWholePixels(XnaColor[] reference, XnaColor[] shifted)
    {
        for (int dy = -MaxWholePixelTranslation; dy <= MaxWholePixelTranslation; dy++)
        {
            for (int dx = -MaxWholePixelTranslation; dx <= MaxWholePixelTranslation; dx++)
            {
                if (IsTranslation(reference, shifted, dx, dy))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsTranslation(XnaColor[] reference, XnaColor[] shifted, int dx, int dy)
    {
        // The text never reaches the border, so skip a margin wider than any translation tried.
        const int margin = MaxWholePixelTranslation + 1;

        for (int y = margin; y < CaptureHeight - margin; y++)
        {
            for (int x = margin; x < CaptureWidth - margin; x++)
            {
                XnaColor expected = reference[((y - dy) * CaptureWidth) + (x - dx)];
                XnaColor actual = shifted[(y * CaptureWidth) + x];

                if (expected != actual)
                {
                    return false;
                }
            }
        }

        return true;
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
