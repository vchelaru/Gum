using Gum.GueDeriving;
using Gum.Renderables;
using KernSmith.Gum;
using Raylib_cs;
using RaylibGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// Raylib side of the MonoGame tests of the same name (#7675): while oversampling pins a measurement
/// font, positions within a line follow the glyphs as drawn, from the display font at the drawn size.
/// Arial 18 oversampled at 1.8 draws forty W's narrower than it measures them.
/// </summary>
public class OversampledTextPositionTests : BaseTestClass
{
    [Fact]
    public void MeasureDrawnString_WhileOversampled_IsTheDisplayFontAtTheDrawnSize()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();
            string text = new string('W', 40);
            TextRuntime textRuntime = new() { Font = "Arial", FontSize = 18 };
            textRuntime.Text = text;
            textRuntime.RegenerateOversampledFont(1.8f).ShouldBeTrue();
            Text contained = (Text)textRuntime.RenderableComponent;
            IFormsText formsText = contained;
            float drawnSize = contained.Font.BaseSize * contained.OversampleCompensationScale;

            for (int index = 0; index <= text.Length; index++)
            {
                string prefix = text.Substring(0, index);
                formsText.MeasureDrawnString(prefix, HorizontalMeasurementStyle.Full)
                    .ShouldBe(Raylib.MeasureTextEx(contained.Font, prefix, drawnSize, 0).X, tolerance: 0.001f, $"pen position before character {index}");
            }
            formsText.GetDrawnCharacterAdvance('W').ShouldBe(Raylib.MeasureTextEx(contained.Font, "W", drawnSize, 0).X, tolerance: 0.001f);
            System.Math.Abs(formsText.MeasureDrawnString(text, HorizontalMeasurementStyle.Full) - contained.MeasureString(text))
                .ShouldBeGreaterThan(formsText.GetDrawnCharacterAdvance('W') / 2,
                    "because the fixture must draw differently from how it measures, or it proves nothing");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void GetCharacterIndexAtPosition_WhileOversampled_ReturnsTheCharacterDrawnAtThePoint()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();
            string text = new string('W', 40);
            TextRuntime textRuntime = new() { Font = "Arial", FontSize = 18 };
            textRuntime.Text = text;
            textRuntime.RegenerateOversampledFont(1.8f).ShouldBeTrue();
            Text contained = (Text)textRuntime.RenderableComponent;
            float drawnSize = contained.Font.BaseSize * contained.OversampleCompensationScale;

            // An unmanaged Text sits at absolute origin (0,0).
            for (int character = 0; character < text.Length; character++)
            {
                float start = Raylib.MeasureTextEx(contained.Font, text.Substring(0, character), drawnSize, 0).X;
                float end = Raylib.MeasureTextEx(contained.Font, text.Substring(0, character + 1), drawnSize, 0).X;

                textRuntime.GetCharacterIndexAtPosition(start + (end - start) * 0.25f, 1)
                    .ShouldBe(character, $"left quarter of character {character}");
                textRuntime.GetCharacterIndexAtPosition(start + (end - start) * 0.75f, 1)
                    .ShouldBe(character + 1, $"right quarter of character {character}");
            }
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }
}
