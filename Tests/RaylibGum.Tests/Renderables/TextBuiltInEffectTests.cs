using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Wireframe;
using Shouldly;
using Xunit;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Renderables;

// raylib draws built-in effects through the shared BbCodeTextEffect math, from one effect call per letter.
public class TextBuiltInEffectTests : BaseTestClass
{
    [Theory]
    [InlineData("[Wave amp=50 freq=5]abc[/Wave]", BbCodeTextEffectKind.Wave)]
    [InlineData("[Shake]abc[/Shake]", BbCodeTextEffectKind.Shake)]
    [InlineData("[Tornado]abc[/Tornado]", BbCodeTextEffectKind.Tornado)]
    [InlineData("[Fade]abc[/Fade]", BbCodeTextEffectKind.Fade)]
    [InlineData("[Rainbow]abc[/Rainbow]", BbCodeTextEffectKind.Rainbow)]
    [InlineData("[Pulse]abc[/Pulse]", BbCodeTextEffectKind.Pulse)]
    public void Text_EffectTag_AssignsOneEffectCallPerLetterAndDraws(string markup, BbCodeTextEffectKind kind)
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = markup;
        Text internalText = (Text)textRuntime.RenderableComponent!;

        internalText.InlineVariables.Count.ShouldBe(3);
        for (int i = 0; i < 3; i++)
        {
            var call = (ParameterizedLetterCustomizationCall)internalText.InlineVariables[i].Value!;
            call.BuiltInEffect!.Kind.ShouldBe(kind);
            call.CharacterIndex.ShouldBe(i);
        }
        Should.NotThrow(() => internalText.Render(RenderingLibrary.SystemManagers.Default));
    }
}
