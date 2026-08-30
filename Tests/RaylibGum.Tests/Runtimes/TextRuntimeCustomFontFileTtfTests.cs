using Gum.GueDeriving;
using Gum.Wireframe;
using RaylibGum.Renderables;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// Pins #4558: on Raylib, a .ttf CustomFontFile (UseCustomFont = true) should route through the
/// same bake cascade (InMemoryFontCreator) that Font-as-path uses, instead of loading natively via
/// LoaderManager.LoadContent -- otherwise UseAutomaticFontGrowth can never track the font's identity
/// and growth silently never engages. Mirrors MonoGame's TextRuntimeCustomFontFileTtfTests (#3703).
/// </summary>
public class TextRuntimeCustomFontFileTtfTests : BaseTestClass
{
    private sealed class CapturingFontCreator : IRaylibFontCreator
    {
        public BmfcSave? LastBmfcSave { get; private set; }

        public Raylib_cs.Font? TryCreateFont(BmfcSave bmfcSave)
        {
            LastBmfcSave = bmfcSave;
            return null;
        }
    }

    [Fact]
    public void SettingCustomFontFileToTtf_PassesFontFileToInMemoryFontCreator()
    {
        CapturingFontCreator creator = new CapturingFontCreator();
        IRaylibFontCreator? previous = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

        try
        {
            TextRuntime text = new TextRuntime();
            text.UseCustomFont = true;
            text.FontSize = 24;
            text.CustomFontFile = "Fonts/MyCustomFont_" + Guid.NewGuid().ToString("N") + ".ttf";

            creator.LastBmfcSave.ShouldNotBeNull();
            creator.LastBmfcSave!.FontFile.ShouldNotBeNullOrEmpty();
            creator.LastBmfcSave.FontFile.ShouldEndWith(".ttf");
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previous;
        }
    }
}
