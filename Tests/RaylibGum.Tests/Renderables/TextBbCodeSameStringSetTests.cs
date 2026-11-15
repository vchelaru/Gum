using Gum.DataTypes;
using Gum.GueDeriving;
using Shouldly;
using System;
using System.Linq;
using Xunit;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Setting a BBCode text to the string it already shows must not re-parse or allocate (#7779); the shared
/// setter in CustomSetPropertyOnRenderable is compiled into RaylibGum, so this pins the Raylib build of it.
/// </summary>
public class TextBbCodeSameStringSetTests : BaseTestClass
{
    const string Tagged = "Press [Link=a][Color=Red]Start[/Color][/Link] to begin, or [Color=Blue]Help[/Color] to read more.";

    private static TextRuntime Make(string value)
    {
        TextRuntime runtime = new();
        runtime.WidthUnits = DimensionUnitType.Absolute;
        runtime.Width = 300;
        runtime.HeightUnits = DimensionUnitType.RelativeToChildren;
        runtime.Height = 0;
        runtime.Text = value;
        runtime.UpdateLayout();
        return runtime;
    }

    [Fact]
    public void Text_SameTaggedString_AllocatesNothing()
    {
        TextRuntime runtime = Make(Tagged);
        runtime.Text = Tagged;
        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < 300; i++)
        {
            runtime.Text = Tagged;
        }

        ((GC.GetAllocatedBytesForCurrentThread() - before) / 300).ShouldBeLessThan(64);
    }

    [Fact]
    public void Text_SameTaggedString_KeepsInlineVariableInstances()
    {
        TextRuntime runtime = Make(Tagged);
        Text text = (Text)runtime.RenderableComponent!;
        object[] before = text.InlineVariables.Cast<object>().ToArray();
        before.Length.ShouldBe(2);

        runtime.Text = Tagged;

        text.InlineVariables.Count.ShouldBe(before.Length);
        for (int i = 0; i < before.Length; i++)
        {
            text.InlineVariables[i].ShouldBeSameAs(before[i]);
        }
    }

    [Fact]
    public void Text_ChangedTaggedString_Reparses()
    {
        TextRuntime runtime = Make(Tagged);

        runtime.Text = "[Color=Green]x[/Color] plain";

        Text text = (Text)runtime.RenderableComponent!;
        text.RawText.ShouldBe("x plain");
        text.InlineVariables.Single().StartIndex.ShouldBe(0);
    }

    [Fact]
    public void Text_SameTaggedString_ReflectsFontSizeChange()
    {
        const string value = "a [Color=Red]red[/Color] b";
        TextRuntime runtime = Make(value);
        runtime.FontSize = 40;
        runtime.Text = value;
        TextRuntime fresh = Make("");
        fresh.FontSize = 40;
        fresh.Text = value;
        fresh.UpdateLayout();

        runtime.AbsoluteWidth.ShouldBe(fresh.AbsoluteWidth);
        runtime.AbsoluteHeight.ShouldBe(fresh.AbsoluteHeight);
    }

}
