using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Issue #6816: across GlobalFontScale, FontScale and zoom, oversampling must keep drawn and measured size
// unchanged, draw 1:1 when it regenerates, and revert fully when turned off.
public class TextRuntimeFontOversamplingSweepTests : BaseTestClass
{
    sealed class RoundingCreator : IInMemoryFontCreator
    {
        public float LastSize;
        public int Calls;
        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            Calls++;
            LastSize = bmfcSave.FontSize;
            int s = (int)System.MathF.Round(bmfcSave.FontSize);
            string fontData =
$@"info face=""Arial"" size=-{s} bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight={s} base={s} scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=3
char id=32 x=0 y=0 width={s} height=13 xoffset=0 yoffset=4 xadvance={s} page=0 chnl=15
char id=65 x=0 y=0 width={s} height=13 xoffset=0 yoffset=4 xadvance={s} page=0 chnl=15
char id=66 x=0 y=0 width={s} height=13 xoffset=0 yoffset=4 xadvance={s} page=0 chnl=15
";
            BitmapFont font = new BitmapFont((Texture2D)null!, fontData);
            font.SetFontPattern(256, 256);
            return font;
        }
    }

    [Fact]
    public void Sweep_DrawnAndMeasuredSize_Invariant_AndDisableReverts()
    {
        bool savedUse = TextRuntime.UseFontOversampling;
        var savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        float savedGlobal = SystemManagers.GlobalFontScale;
        var failures = new System.Collections.Generic.List<string>();
        int cases = 0;
        try
        {
            foreach (float global in new[] { 1f, 1.5f })
            foreach (float fontScale in new[] { 0.5f, 1f, 1.5f, 2f, 3f })
            foreach (float zoom in new[] { 0.5f, 1f, 1.25f, 1.5f, 2f, 2.93f, 4f })
            {
                cases++;
                SystemManagers.GlobalFontScale = global;
                TextRuntime.UseFontOversampling = true;
                var creator = new RoundingCreator();
                CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

                TextRuntime t = new();
                t.WidthUnits = DimensionUnitType.RelativeToChildren;
                t.HeightUnits = DimensionUnitType.RelativeToChildren;
                t.FontSize = 20;
                t.FontScale = fontScale;
                t.Text = "AB AB";
                var text = (Text)t.RenderableComponent;
                float w0 = t.AbsoluteWidth, h0 = t.AbsoluteHeight;
                float drawn0 = ((IText)text).FontScale * text.BitmapFont!.LineHeightInPixels;
                creator.Calls = 0;

                bool regen = t.UpdateAutomaticFontOversampling(zoom);
                float expectedRaster = 20 * System.MathF.Max(1f, fontScale * global * zoom);
                bool shouldRegen = System.MathF.Abs(expectedRaster - 20) >= 1;
                string id = $"g{global} s{fontScale} z{zoom}";
                if (regen != shouldRegen) failures.Add($"{id}: regen={regen}");
                if (regen && System.MathF.Abs(creator.LastSize - expectedRaster) > 0.001f) failures.Add($"{id}: raster {creator.LastSize} != {expectedRaster}");
                // drawn size in world units: atlas line height times composed scale (rounding of int lineHeight allowed)
                float drawn = ((IText)text).FontScale * text.BitmapFont!.LineHeightInPixels;
                if (System.MathF.Abs(drawn - drawn0) > 0.5f * fontScale * global + 0.01f) failures.Add($"{id}: drawn {drawn} vs {drawn0}");
                if (regen && System.MathF.Abs(((IText)text).FontScale * zoom - 1f) > 0.0001f)
                    failures.Add($"{id}: IText.FontScale {((IText)text).FontScale}");
                if (t.AbsoluteWidth != w0 || t.AbsoluteHeight != h0) failures.Add($"{id}: layout {t.AbsoluteWidth}x{t.AbsoluteHeight} vs {w0}x{h0}");

                // disable reverts
                TextRuntime.UseFontOversampling = false;
                t.UpdateAutomaticFontOversampling(zoom);
                if (text.OversampleCompensationScale != 1f) failures.Add($"{id}: comp after disable {text.OversampleCompensationScale}");
                if (System.MathF.Abs(((IText)text).FontScale - fontScale * global) > 0.0001f) failures.Add($"{id}: scale after disable {((IText)text).FontScale}");
                if (t.AbsoluteWidth != w0 || t.AbsoluteHeight != h0) failures.Add($"{id}: layout after disable {t.AbsoluteWidth}x{t.AbsoluteHeight}");
                if (text.BitmapFont!.LineHeightInPixels != 20) failures.Add($"{id}: font after disable lh {text.BitmapFont.LineHeightInPixels}");
                // second update with oversampling off does not regenerate
                creator.Calls = 0;
                t.UpdateAutomaticFontOversampling(zoom);
                if (creator.Calls != 0) failures.Add($"{id}: creator called after disable");
            }
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUse;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
            SystemManagers.GlobalFontScale = savedGlobal;
        }
        cases.ShouldBe(70);
        failures.ShouldBeEmpty(string.Join("\n", failures));
    }
}
