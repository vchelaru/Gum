using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Shouldly;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// #8176: the [State=Name] tag expands a GraphicalUiElement's named state into the per-run variables
/// SkiaGum applies, as on MonoGame/Raylib. Mirrors RaylibGum.Tests TextMarkupTests and
/// MonoGameGum.Tests TextRuntimeBbCodeStateTests.
/// </summary>
public class TextBbCodeStateTests
{
    private static IReadOnlyList<Text.StyledTextRun> RunsOf(TextRuntime textRuntime) =>
        ((Text)textRuntime.RenderableComponent).GetStyledRuns();

    private static StateSave ColorState(string name, int r, int g, int b)
    {
        StateSave state = new() { Name = name };
        state.Variables.Add(new VariableSave { Name = "Color", Value = System.Drawing.Color.FromArgb(255, r, g, b) });
        return state;
    }

    [Fact]
    public void Text_WithStateTag_AppliesAllowlistedColorAndSkipsDisallowedVariable()
    {
        TextRuntime textRuntime = new();
        StateSave state = ColorState("Highlighted", 10, 20, 30);
        state.Variables.Add(new VariableSave { Name = "X", Value = 999f });
        textRuntime.AddStates(new List<StateSave> { state });

        textRuntime.Text = "before [State=Highlighted]middle[/State] after";

        RunsOf(textRuntime).Select(r => r.Text).ShouldBe(new[] { "before ", "middle", " after" });
        RunsOf(textRuntime)[1].Style.TextColor.ShouldBe(new SKColor(10, 20, 30, 255));
        textRuntime.X.ShouldBe(0);
    }

    [Fact]
    public void Text_WithStateTagInsideCategory_ResolvesByBareName()
    {
        TextRuntime textRuntime = new();
        StateSaveCategory category = new() { Name = "StyleCategory" };
        category.States.Add(ColorState("H3", 10, 20, 30));
        textRuntime.AddCategory(category);

        textRuntime.Text = "before [State=H3]middle[/State] after";

        RunsOf(textRuntime)[1].Style.TextColor.ShouldBe(new SKColor(10, 20, 30, 255));
    }

    [Fact]
    public void Text_WithStateTagInDifferentCase_AppliesLikeCanonicalCase()
    {
        TextRuntime textRuntime = new();
        textRuntime.AddStates(new List<StateSave> { ColorState("Highlighted", 10, 20, 30) });

        textRuntime.Text = "a[state=Highlighted]b[/state]c";

        RunsOf(textRuntime)[1].Style.TextColor.ShouldBe(new SKColor(10, 20, 30, 255));
    }

    [Fact]
    public void Text_WithStateTagFontStackVariable_AppliesBold()
    {
        TextRuntime textRuntime = new();
        StateSave state = new() { Name = "Bold" };
        state.Variables.Add(new VariableSave { Name = "IsBold", Value = true });
        textRuntime.AddStates(new List<StateSave> { state });

        textRuntime.Text = "normal [State=Bold]bold[/State] normal";

        RunsOf(textRuntime)[1].Style.FontWeight.ShouldBeGreaterThan(RunsOf(textRuntime)[0].Style.FontWeight);
    }

    [Fact]
    public void Text_OnClonedRuntime_AppliesStateTag()
    {
        TextRuntime original = new();
        original.AddStates(new List<StateSave> { ColorState("Highlighted", 10, 20, 30) });
        TextRuntime clone = (TextRuntime)original.Clone();

        clone.Text = "a[State=Highlighted]b[/State]c";

        RunsOf(clone)[1].Style.TextColor.ShouldBe(new SKColor(10, 20, 30, 255));
    }

    [Fact]
    public void Text_OnRuntimeWithoutFullInstantiation_AppliesStateTagOnceRenderableAttached()
    {
        TextRuntime runtime = new(fullInstantiation: false);
        runtime.SetContainedObject(new Text());
        runtime.AddStates(new List<StateSave> { ColorState("Highlighted", 10, 20, 30) });

        runtime.Text = "a[State=Highlighted]b[/State]c";

        RunsOf(runtime)[1].Style.TextColor.ShouldBe(new SKColor(10, 20, 30, 255));
    }

    [Fact]
    public void Text_WithUnknownStateTag_StripsTagAndDoesNotThrow()
    {
        TextRuntime textRuntime = new();

        Should.NotThrow(() => textRuntime.Text = "before [State=DoesNotExist]middle[/State] after");

        RunsOf(textRuntime).Select(r => r.Text).ShouldBe(new[] { "before middle after" });
    }
}
