using Gum.GueDeriving;
using Gum.Renderables;
using KernSmith.Gum;
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
