using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Localization;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// #7398: FontReferenceCollector bakes the fonts of every translation of a localization key, so the union
// over the languages must be what the renderer asks for as each language is selected. Gum.Bundle.Tests
// pins the same list for the collector (FontReferenceCollectorLocalizationTests, state-tag test).
public class TextRuntimeLocalizedFontTests : BaseTestClass
{
    private const string FontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=20 base=20 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=2
char id=32 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15
char id=97 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15
";

    private class RecordingFontCreator : IInMemoryFontCreator
    {
        public HashSet<string> RequestedFiles { get; } = new HashSet<string>();

        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            RequestedFiles.Add(bmfcSave.FontCacheFileName);
            BitmapFont font = new BitmapFont((Texture2D)null!, FontData);
            font.SetFontPattern(256, 256);
            return font;
        }
    }

    [Fact]
    public void Text_WithLocalizationKey_RequestsTheFontsTheCollectorBakesForEveryLanguage()
    {
        IInMemoryFontCreator? previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        ILocalizationService? previousLocalization = CustomSetPropertyOnRenderable.LocalizationService;
        RecordingFontCreator creator = new RecordingFontCreator();
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        try
        {
            LocalizationService localization = new LocalizationService();
            string csv = "id,English,Spanish\nMENU_PLAY,[state=Big]Play[/state],[Font=Verdana]Jugar[/Font]\n";
            localization.AddCsvDatabase(new MemoryStream(Encoding.UTF8.GetBytes(csv)));
            CustomSetPropertyOnRenderable.LocalizationService = localization;

            TextRuntime textRuntime = new();
            StateSave big = new() { Name = "Big" };
            big.Variables.Add(new VariableSave { Name = "Font", Type = "string", Value = "StateFaceB", SetsValue = true });
            big.Variables.Add(new VariableSave { Name = "FontSize", Type = "int", Value = 40, SetsValue = true });
            textRuntime.AddStates(new List<StateSave> { big });
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 18;
            creator.RequestedFiles.Clear();

            for (int language = 1; language <= localization.Languages.Count; language++)
            {
                localization.CurrentLanguage = language;
                textRuntime.Text = "MENU_PLAY";
            }

            // The base font (Arial 18) was loaded when it was set.
            creator.RequestedFiles.ShouldBe(
                new[]
                {
                    CacheFileName(18, "StateFaceB"),
                    CacheFileName(40, "StateFaceB"),
                    CacheFileName(40, "Arial"),
                    CacheFileName(18, "Verdana"),
                },
                ignoreOrder: true);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previousCreator;
            CustomSetPropertyOnRenderable.LocalizationService = previousLocalization;
        }
    }

    private static string CacheFileName(int size, string name) =>
        BmfcSave.GetFontCacheFileNameFor(
            fontSize: size, fontName: name, outline: 0, useFontSmoothing: true,
            isItalic: false, isBold: false, fontFilePath: null);
}
