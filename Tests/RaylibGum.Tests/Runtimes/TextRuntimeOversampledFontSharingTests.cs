using Gum.GueDeriving;
using Gum.Renderables;
using KernSmith.Gum;
using RenderingLibrary;
using RaylibGum.Renderables;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

// Issue #6810: Raylib side of the MonoGame tests of the same name -- texts asking for the same
// oversampled font share one generation.
public class TextRuntimeOversampledFontSharingTests : BaseTestClass
{
    [Fact]
    public void RegenerateOversampledFont_IdenticalRequests_GenerateOnceAndShareTheFont()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CountingFontCreator creator = new(new KernSmithRaylibFontCreator());
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
            TextRuntime first = new() { Font = "Arial", FontSize = 20 };
            TextRuntime second = new() { Font = "Arial", FontSize = 20 };

            first.RegenerateOversampledFont(2f).ShouldBeTrue();
            second.RegenerateOversampledFont(2f).ShouldBeTrue();

            creator.RequestsAt40.ShouldBe(1);
            GetText(second).Font.Texture.Id
                .ShouldBe(GetText(first).Font.Texture.Id);
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Issue #6814: moving one text off a shared font must not unload the font another text still draws.
    // The last text to leave unloads it, keeping #4364's leak fix.
    [Fact]
    public void RegenerateOversampledFont_SharedFont_IsUnloadedOnlyWhenTheLastTextLeavesIt()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();
            TextRuntime first = new() { Font = "Arial", FontSize = 20, Text = "AB" };
            TextRuntime second = new() { Font = "Arial", FontSize = 20, Text = "AB" };
            first.RegenerateOversampledFont(2.5f).ShouldBeTrue();
            second.RegenerateOversampledFont(2.5f).ShouldBeTrue();
            uint sharedTextureId = GetText(second).Font.Texture.Id;

            first.RegenerateOversampledFont(3f).ShouldBeTrue();

            RaylibFontMetricsRegistry.IsRegistered(sharedTextureId).ShouldBeTrue("because the second text still draws the shared font");

            second.RegenerateOversampledFont(3f).ShouldBeTrue();

            RaylibFontMetricsRegistry.IsRegistered(sharedTextureId).ShouldBeFalse("because no text uses the font any more");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Issue #6814: the oversampled atlas draws at exactly 1/ratio so each atlas pixel lands on one
    // screen pixel, rather than a fit to measured widths that resamples every glyph.
    [Fact]
    public void RegenerateOversampledFont_CompensatesByExactlyTheInverseRatio()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();
            TextRuntime textRuntime = new() { Font = "Arial", FontSize = 26, Text = "K2 9A" };

            textRuntime.RegenerateOversampledFont(1.3953f).ShouldBeTrue();

            GetText(textRuntime).OversampleCompensationScale.ShouldBe(26f / (26f * 1.3953f), tolerance: 0.00001f);
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    private static Text GetText(TextRuntime textRuntime) => (Text)textRuntime.RenderableComponent!;

    private sealed class CountingFontCreator : IRaylibFontCreator
    {
        private readonly IRaylibFontCreator _inner;

        public CountingFontCreator(IRaylibFontCreator inner) => _inner = inner;

        public int RequestsAt40 { get; private set; }

        public Raylib_cs.Font? TryCreateFont(BmfcSave bmfcSave)
        {
            if (bmfcSave.FontSize == 40f)
            {
                RequestsAt40++;
            }
            return _inner.TryCreateFont(bmfcSave);
        }
    }
}
