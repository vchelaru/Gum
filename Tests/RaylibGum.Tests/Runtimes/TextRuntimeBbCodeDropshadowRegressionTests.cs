using Gum.GueDeriving;
using Gum.Wireframe;
using KernSmith.Gum;
using Raylib_cs;
using RaylibGum.Renderables;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

// #3625 parity (Raylib): mirrors MonoGameGum.Tests.Runtimes.TextRuntimeBbCodeDropshadowRegressionTests.
// GetAndCreateFontIfNecessary's Raylib branch shares BuildInlineRunBmfcSave with the MonoGame branch
// (Gum/Wireframe/CustomSetPropertyOnRenderable.cs) - a base TextRuntime with HasDropshadow=true must
// propagate its dropshadow fields onto an inline BBCode font run's BmfcSave here too, not just on
// MonoGame. Previously untested on this platform.
public class TextRuntimeBbCodeDropshadowRegressionTests : BaseTestClass
{
    [Fact]
    public void Text_WithBbCodeBoldRun_WhenBaseHasDropshadow_PropagatesDropshadowToInlineRunFont()
    {
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            RecordingRaylibFontCreator recordingCreator = new();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = recordingCreator;

            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 20;
            textRuntime.HasDropshadow = true;
            textRuntime.DropshadowOffsetX = 2f;
            textRuntime.DropshadowOffsetY = 3f;
            textRuntime.DropshadowBlur = 1.5f;
            textRuntime.DropshadowRed = 10;
            textRuntime.DropshadowGreen = 20;
            textRuntime.DropshadowBlue = 30;
            textRuntime.DropshadowAlpha = 200;

            textRuntime.Text = "normal [IsBold=true]bold[/IsBold] normal";

            // The base-font resolution and the inline-bold-run resolution both call the creator; only
            // the bold request exercises the per-run path this issue is about.
            BmfcSave boldRequest = recordingCreator.Requests.Single(r => r.IsBold);
            boldRequest.HasDropshadow.ShouldBeTrue();
            boldRequest.DropshadowOffsetX.ShouldBe(2f);
            boldRequest.DropshadowOffsetY.ShouldBe(3f);
            boldRequest.DropshadowBlur.ShouldBe(1.5f);
            boldRequest.DropshadowRed.ShouldBe((byte)10);
            boldRequest.DropshadowGreen.ShouldBe((byte)20);
            boldRequest.DropshadowBlue.ShouldBe((byte)30);
            boldRequest.DropshadowAlpha.ShouldBe((byte)200);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Wraps the real KernSmithRaylibFontCreator (rather than a hand-built Raylib_cs.Font, whose
    // unmanaged Recs/Glyphs pointers would need to be valid for UnloadFont to safely dispose it later)
    // so it produces a genuinely usable, disposable font while recording every BmfcSave requested -
    // mirrors TextMarkupTests.RecordingRaylibFontCreator.
    private sealed class RecordingRaylibFontCreator : IRaylibFontCreator
    {
        private readonly KernSmithRaylibFontCreator _inner = new();

        public List<BmfcSave> Requests { get; } = new();

        public Font? TryCreateFont(BmfcSave bmfcSave)
        {
            Requests.Add(bmfcSave);
            return _inner.TryCreateFont(bmfcSave);
        }
    }
}
