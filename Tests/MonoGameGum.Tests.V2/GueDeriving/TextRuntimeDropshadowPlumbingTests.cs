using Gum.GueDeriving;
using Gum.Wireframe;
using MonoGameGum.Renderables;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V2.GueDeriving;

/// <summary>
/// Pins #2724 runtime plumbing: TextRuntime dropshadow properties flow into BmfcSave and the font cache key
/// when UpdateToFontValues runs via the registered in-memory font creator.
/// </summary>
public class TextRuntimeDropshadowPlumbingTests
{
    [Fact]
    public void SettingHasDropshadow_PassesShadowFieldsToInMemoryFontCreator()
    {
        CapturingInMemoryFontCreator creator = new CapturingInMemoryFontCreator();
        IInMemoryFontCreator? previous = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

        try
        {
            TextRuntime text = new TextRuntime();
            text.Font = "Arial";
            text.FontSize = 24;
            text.HasDropshadow = true;
            text.DropshadowOffsetX = 2f;
            text.DropshadowOffsetY = 3f;
            text.DropshadowBlur = 4f;
            text.DropshadowRed = 10;
            text.DropshadowGreen = 20;
            text.DropshadowBlue = 30;
            text.DropshadowAlpha = 128;

            creator.LastBmfcSave.ShouldNotBeNull();
            creator.LastBmfcSave!.HasDropshadow.ShouldBeTrue();
            creator.LastBmfcSave.DropshadowOffsetX.ShouldBe(2f);
            creator.LastBmfcSave.DropshadowOffsetY.ShouldBe(3f);
            creator.LastBmfcSave.DropshadowBlur.ShouldBe(4f);
            creator.LastBmfcSave.DropshadowRed.ShouldBe((byte)10);
            creator.LastBmfcSave.DropshadowGreen.ShouldBe((byte)20);
            creator.LastBmfcSave.DropshadowBlue.ShouldBe((byte)30);
            creator.LastBmfcSave.DropshadowAlpha.ShouldBe((byte)128);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previous;
        }
    }

    [Fact]
    public void SettingHasDropshadowWithoutColor_UsesBlackSemiTransparentDefaults()
    {
        CapturingInMemoryFontCreator creator = new CapturingInMemoryFontCreator();
        IInMemoryFontCreator? previous = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

        try
        {
            TextRuntime text = new TextRuntime();
            text.Font = "Arial";
            text.FontSize = 24;
            text.HasDropshadow = true;

            creator.LastBmfcSave.ShouldNotBeNull();
            creator.LastBmfcSave!.HasDropshadow.ShouldBeTrue();
            creator.LastBmfcSave.DropshadowRed.ShouldBe((byte)0);
            creator.LastBmfcSave.DropshadowGreen.ShouldBe((byte)0);
            creator.LastBmfcSave.DropshadowBlue.ShouldBe((byte)0);
            creator.LastBmfcSave.DropshadowAlpha.ShouldBe((byte)180);
            creator.LastBmfcSave.DropshadowOffsetY.ShouldBe(3f);
            creator.LastBmfcSave.DropshadowBlur.ShouldBe(2f);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previous;
        }
    }

    [Fact]
    public void GetFontCacheFileName_WhenHasDropshadowDiffersFromPlainKey()
    {
        TextRuntime text = new TextRuntime();
        text.Font = "Arial";
        text.FontSize = 24;

        string plainKey = text.GetFontCacheFileName(fontFilePath: null);

        text.HasDropshadow = true;
        text.DropshadowOffsetX = 1f;
        text.DropshadowOffsetY = 2f;
        text.DropshadowBlur = 3f;
        text.DropshadowAlpha = 4;

        text.GetFontCacheFileName(fontFilePath: null).ShouldNotBe(plainKey);
        text.GetFontCacheFileName(fontFilePath: null).ShouldContain("_ds");
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
