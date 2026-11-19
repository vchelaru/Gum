using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// [State=Name] BBCode tag: decomposes a GraphicalUiElement's named state (States/AddStates, the
// same runtime concept ApplyState(string) already uses) into the substring the tag wraps, but only
// for variables already wired for per-run application - Color/Red/Green/Blue/FontScale (direct
// InlineVariable) and Font/FontSize/OutlineThickness/IsItalic/IsBold/UseCustomFont/HasDropshadow (the
// font-stack path in ApplyFontVariables). Anything else in the state (X, Y, Width, etc.) is silently skipped,
// and an unknown state name is a no-op - neither is an error.
public class TextRuntimeBbCodeStateTests : BaseTestClass
{
    [Fact]
    public void Text_WithStateBbCodeTag_AppliesAllowlistedColorAndSkipsDisallowedVariable()
    {
        TextRuntime textRuntime = new();

        StateSave state = new() { Name = "Highlighted" };
        state.Variables.Add(new VariableSave { Name = "Color", Value = System.Drawing.Color.FromArgb(255, 10, 20, 30) });
        // Disallowed: X has no per-run meaning and must never be applied to the whole element from
        // inside a text span.
        state.Variables.Add(new VariableSave { Name = "X", Value = 999f });
        textRuntime.AddStates(new List<StateSave> { state });

        textRuntime.Text = "before [State=Highlighted]middle[/State] after";

        Text text = (Text)textRuntime.RenderableComponent;
        text.RawText.ShouldBe("before middle after");

        InlineVariable colorVariable = text.InlineVariables.Single(v => v.VariableName == "Color");
        colorVariable.Value.ShouldBe(System.Drawing.Color.FromArgb(255, 10, 20, 30));
        colorVariable.StartIndex.ShouldBe(7);
        colorVariable.CharacterCount.ShouldBe(6);

        text.InlineVariables.ShouldNotContain(v => v.VariableName == "X");
        textRuntime.X.ShouldBe(0);
    }

    [Fact]
    public void Text_WithStateBbCodeTag_AppliesAlpha()
    {
        TextRuntime textRuntime = new();

        StateSave state = new() { Name = "Faded" };
        state.Variables.Add(new VariableSave { Name = "Alpha", Value = 128 });
        textRuntime.AddStates(new List<StateSave> { state });

        textRuntime.Text = "a[State=Faded]b[/State]";

        Text text = (Text)textRuntime.RenderableComponent;
        InlineVariable alphaVariable = text.InlineVariables.Single(v => v.VariableName == "Alpha");
        alphaVariable.Value.ShouldBe((byte)128);
        (alphaVariable.StartIndex, alphaVariable.CharacterCount).ShouldBe((1, 1));
    }

    // Regression: a state defined inside a StateSaveCategory (e.g. the Gum tool's States tab,
    // categorized states) - not just an uncategorized AddStates entry - must still resolve by bare
    // name, mirroring GraphicalUiElement.ApplyState(string)'s own lookup (States, then every
    // Category's States). Found via a real project where H1/H2/H3 lived under a "StyleCategory"
    // category and [State=H3] silently no-opped because the lookup only checked the flat dictionary.
    [Fact]
    public void Text_WithStateBbCodeTag_ResolvesStateDefinedInsideCategory()
    {
        TextRuntime textRuntime = new();

        StateSaveCategory styleCategory = new() { Name = "StyleCategory" };
        StateSave h3 = new() { Name = "H3" };
        h3.Variables.Add(new VariableSave { Name = "Color", Value = System.Drawing.Color.FromArgb(255, 10, 20, 30) });
        styleCategory.States.Add(h3);
        textRuntime.AddCategory(styleCategory);

        textRuntime.Text = "before [State=H3]middle[/State] after";

        Text text = (Text)textRuntime.RenderableComponent;
        text.RawText.ShouldBe("before middle after");

        InlineVariable colorVariable = text.InlineVariables.Single(v => v.VariableName == "Color");
        colorVariable.Value.ShouldBe(System.Drawing.Color.FromArgb(255, 10, 20, 30));
    }

    [Fact]
    public void Text_WithStateBbCodeTag_AppliesFontStackVariableLikeIndividualTag()
    {
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            RecordingInMemoryFontCreator recordingCreator = new();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = recordingCreator;

            TextRuntime textRuntime = new();
            // A font/size NOT in the test harness's stubbed embedded resources, so resolution
            // actually reaches the in-memory creator instead of being satisfied by an embedded font
            // (matches the convention in TextRuntimeBbCodeDropshadowRegressionTests).
            textRuntime.Font = "Garet";
            textRuntime.FontSize = 12;

            StateSave state = new() { Name = "Bold" };
            state.Variables.Add(new VariableSave { Name = "IsBold", Value = true });
            textRuntime.AddStates(new List<StateSave> { state });

            textRuntime.Text = "normal [State=Bold]bold[/State] normal";

            // The base-font resolution requests IsBold=false; only a state-decomposed [IsBold=true]
            // run produces a bold request.
            recordingCreator.Requests.ShouldContain(r => r.IsBold);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void Text_WithUnknownStateBbCodeTag_StripsTagAndDoesNotThrow()
    {
        TextRuntime textRuntime = new();

        Should.NotThrow(() =>
            textRuntime.Text = "before [State=DoesNotExist]middle[/State] after");

        Text text = (Text)textRuntime.RenderableComponent;
        text.RawText.ShouldBe("before middle after");
        text.InlineVariables.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Red", (byte)200)]
    [InlineData("Green", (byte)100)]
    [InlineData("Blue", (byte)50)]
    [InlineData("FontScale", 2f)]
    public void Text_WithStateBbCodeTag_AppliesEachDirectVariableToTheWrappedRange(string variableName, object value)
    {
        TextRuntime textRuntime = new();
        StateSave state = new() { Name = "Styled" };
        state.Variables.Add(new VariableSave { Name = variableName, Value = value });
        textRuntime.AddStates(new List<StateSave> { state });

        textRuntime.Text = "ab[State=Styled]cd[/State]e";

        Text text = (Text)textRuntime.RenderableComponent;
        text.RawText.ShouldBe("abcde");
        InlineVariable variable = text.InlineVariables.Single(v => v.VariableName == variableName);
        variable.Value.ShouldBe(value);
        variable.StartIndex.ShouldBe(2);
        variable.CharacterCount.ShouldBe(2);
    }

    [Fact]
    public void Text_WithStateBbCodeTagHoldingSeveralVariables_AppliesEveryOneToTheSameRange()
    {
        TextRuntime textRuntime = new();
        StateSave state = new() { Name = "Styled" };
        state.Variables.Add(new VariableSave { Name = "Color", Value = System.Drawing.Color.FromArgb(255, 1, 2, 3) });
        state.Variables.Add(new VariableSave { Name = "FontScale", Value = 2f });
        textRuntime.AddStates(new List<StateSave> { state });

        textRuntime.Text = "a[State=Styled]bc[/State]";

        Text text = (Text)textRuntime.RenderableComponent;
        text.InlineVariables.Select(v => (v.VariableName, v.StartIndex, v.CharacterCount))
            .OrderBy(v => v.VariableName)
            .ShouldBe(new[] { ("Color", 1, 2), ("FontScale", 1, 2) });
    }

    [Fact]
    public void Text_WithStateBbCodeTagNestedInColor_KeepsBothOnTheInnerLetters()
    {
        TextRuntime textRuntime = new();
        StateSave state = new() { Name = "Big" };
        state.Variables.Add(new VariableSave { Name = "FontScale", Value = 2f });
        textRuntime.AddStates(new List<StateSave> { state });

        textRuntime.Text = "[Color=Red]a[State=Big]b[/State]c[/Color]";

        Text text = (Text)textRuntime.RenderableComponent;
        text.RawText.ShouldBe("abc");
        InlineVariable color = text.InlineVariables.Single(v => v.VariableName == "Color");
        (color.StartIndex, color.CharacterCount).ShouldBe((0, 3));
        InlineVariable scale = text.InlineVariables.Single(v => v.VariableName == "FontScale");
        (scale.StartIndex, scale.CharacterCount).ShouldBe((1, 1));
    }

    [Fact]
    public void Text_WithStateBbCodeTagBeforeCustom_DoesNotShiftTheCustomIndex()
    {
        const string name = "StateIndexProbe";
        Text.Customizations[name] = (index, block) => new LetterCustomization();
        try
        {
            TextRuntime textRuntime = new();
            StateSave state = new() { Name = "Styled" };
            state.Variables.Add(new VariableSave { Name = "Color", Value = System.Drawing.Color.FromArgb(255, 1, 2, 3) });
            textRuntime.AddStates(new List<StateSave> { state });

            textRuntime.Text = $"[State=Styled]ab[/State][Custom={name}]c[/Custom]";

            Text text = (Text)textRuntime.RenderableComponent;
            ParameterizedLetterCustomizationCall call = text.InlineVariables
                .Select(v => v.Value).OfType<ParameterizedLetterCustomizationCall>().Single();
            call.CharacterIndex.ShouldBe(2);
        }
        finally
        {
            Text.Customizations.Remove(name);
        }
    }

    // Records every BmfcSave a per-request font resolution asks for, and returns a minimal valid
    // BitmapFont (only the space glyph is needed - measurement isn't asserted).
    private sealed class RecordingInMemoryFontCreator : IInMemoryFontCreator
    {
        public List<BmfcSave> Requests { get; } = new();

        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            Requests.Add(bmfcSave);
            BitmapFont font = new BitmapFont((Texture2D)null!, FontData);
            font.SetFontPattern(256, 256);
            return font;
        }

        private const string FontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=18 base=18 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=1
char id=32 x=0 y=0 width=9 height=13 xoffset=0 yoffset=4 xadvance=9 page=0 chnl=15
";
    }
}
