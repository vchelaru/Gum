using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V2.GueDeriving;

/// <summary>
/// Pins #3703: CustomFontFile pointing at a .ttf/.otf should route through the same bake
/// cascade Font-as-path uses, instead of the .fnt-only BitmapFont load.
/// </summary>
public class TextRuntimeCustomFontFileTtfTests
{
    [Fact]
    public void SettingCustomFontFileToTtf_PassesFontFileToInMemoryFontCreator()
    {
        CapturingInMemoryFontCreator creator = new CapturingInMemoryFontCreator();
        IInMemoryFontCreator? previous = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

        try
        {
            TextRuntime text = new TextRuntime();
            text.UseCustomFont = true;
            text.FontSize = 24;
            text.CustomFontFile = "Fonts/MyCustomFont.ttf";

            creator.LastBmfcSave.ShouldNotBeNull();
            creator.LastBmfcSave!.FontFile.ShouldNotBeNullOrEmpty();
            creator.LastBmfcSave.FontFile.ShouldEndWith("MyCustomFont.ttf");
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previous;
        }
    }

    [Fact]
    public void SettingCustomFontFileToFnt_DoesNotConsultInMemoryFontCreator()
    {
        CapturingInMemoryFontCreator creator = new CapturingInMemoryFontCreator();
        IInMemoryFontCreator? previous = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

        try
        {
            TextRuntime text = new TextRuntime();
            text.UseCustomFont = true;
            text.FontSize = 24;
            text.CustomFontFile = "Fonts/MyPrebakedFont.fnt";

            creator.LastBmfcSave.ShouldBeNull();
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previous;
        }
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
