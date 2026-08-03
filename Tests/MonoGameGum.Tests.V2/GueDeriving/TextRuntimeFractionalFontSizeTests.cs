using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V2.GueDeriving;

/// <summary>
/// Issue #4304: TextRuntime.FontSize is float, so a fractional size round-trips and flows through to
/// BmfcSave/the font cache key unrounded — the KernSmith backend rasterizes it natively.
/// </summary>
public class TextRuntimeFractionalFontSizeTests
{
    [Fact]
    public void SettingFontSize_RoundTripsFractionalValue()
    {
        TextRuntime text = new TextRuntime();
        text.FontSize = 18.5f;

        text.FontSize.ShouldBe(18.5f);
    }

    [Fact]
    public void SettingFractionalFontSize_PassesUnroundedValueToInMemoryFontCreator()
    {
        CapturingInMemoryFontCreator creator = new CapturingInMemoryFontCreator();
        IInMemoryFontCreator? previous = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

        try
        {
            TextRuntime text = new TextRuntime();
            text.Font = "Arial";
            text.FontSize = 18.5f;

            creator.LastBmfcSave.ShouldNotBeNull();
            creator.LastBmfcSave!.FontSize.ShouldBe(18.5f);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previous;
        }
    }

    [Fact]
    public void GetFontCacheFileName_WithFractionalFontSize_DiffersFromNeighboringIntegers()
    {
        TextRuntime eighteen = new TextRuntime { Font = "Arial", FontSize = 18 };
        TextRuntime eighteenHalf = new TextRuntime { Font = "Arial", FontSize = 18.5f };
        TextRuntime nineteen = new TextRuntime { Font = "Arial", FontSize = 19 };

        string eighteenKey = eighteen.GetFontCacheFileName(fontFilePath: null);
        string eighteenHalfKey = eighteenHalf.GetFontCacheFileName(fontFilePath: null);
        string nineteenKey = nineteen.GetFontCacheFileName(fontFilePath: null);

        eighteenHalfKey.ShouldNotBe(eighteenKey);
        eighteenHalfKey.ShouldNotBe(nineteenKey);
    }

    private sealed class CapturingInMemoryFontCreator : IInMemoryFontCreator
    {
        public BmfcSave? LastBmfcSave { get; private set; }

        public RenderingLibrary.Graphics.BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            LastBmfcSave = bmfcSave;
            return null;
        }
    }
}
