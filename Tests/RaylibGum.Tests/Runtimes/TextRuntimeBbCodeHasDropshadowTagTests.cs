using Gum.GueDeriving;
using Gum.Wireframe;
using KernSmith.Gum;
using Raylib_cs;
using RaylibGum.Renderables;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

// #3528 parity (Raylib): mirrors MonoGameGum.Tests.Runtimes.TextRuntimeBbCodeHasDropshadowTagTests.
// A [HasDropshadow] BBCode tag lets a run toggle the shadow independently of the base
// TextRuntime.HasDropshadow, using the same push/pop stack model as [IsBold]/[FontSize]/etc.
public class TextRuntimeBbCodeHasDropshadowTagTests : BaseTestClass
{
    [Fact]
    public void Text_WithHasDropshadowFalseTag_WhenBaseHasDropshadow_ResolvesRunFontWithoutDropshadow()
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

            textRuntime.Text = "normal [IsBold=true][HasDropshadow=false]off[/HasDropshadow][/IsBold] normal";

            // The [IsBold] open (still base HasDropshadow=true), the [HasDropshadow=false] open, and the
            // matching close each re-resolve a bold font - only the middle one has shadow turned off.
            recordingCreator.Requests.ShouldContain(r => r.IsBold && !r.HasDropshadow);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void Text_WithHasDropshadowTrueTag_WhenBaseHasNoDropshadow_ResolvesRunFontWithDropshadow()
    {
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            RecordingRaylibFontCreator recordingCreator = new();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = recordingCreator;

            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 20;
            textRuntime.HasDropshadow = false;

            textRuntime.Text = "normal [IsBold=true][HasDropshadow=true]on[/HasDropshadow][/IsBold] normal";

            recordingCreator.Requests.ShouldContain(r => r.IsBold && r.HasDropshadow);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Wraps the real KernSmithRaylibFontCreator (rather than a hand-built Raylib_cs.Font, whose
    // unmanaged Recs/Glyphs pointers would need to be valid for UnloadFont to safely dispose it later)
    // so it produces a genuinely usable, disposable font while recording every BmfcSave requested -
    // mirrors TextRuntimeBbCodeDropshadowRegressionTests.RecordingRaylibFontCreator.
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
