using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Localization;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// #7779: setting a BBCode text to the string it already shows must not re-parse, re-allocate or re-wrap
// (plain text already skips an equal string in Text.RawText). Everything that does change the parse - the
// string, the font, the dropshadow, the width - must still refresh. The "fresh" oracle builds a second
// TextRuntime with the final settings and compares what the renderable ended up with.
public class TextRuntimeBbCodeSameStringSetTests : BaseTestClass
{
    const string Tagged =
        "Press [Link=start][Color=0xFFE0C080]Start[/Color][/Link] to begin. Open [Link=settings][Color=0xFFB0B0B0]Settings[/Color][/Link] to change " +
        "the [Color=0xFF80C0FF]volume[/Color], or [Link=help][Color=0xFFFFD24A]Help[/Color][/Link] to read more about the options.";

    private static TextRuntime Make(string? text, bool relativeToChildren = false, float width = 300)
    {
        TextRuntime runtime = new();
        if (relativeToChildren)
        {
            runtime.WidthUnits = DimensionUnitType.RelativeToChildren;
            runtime.Width = 0;
        }
        else
        {
            runtime.WidthUnits = DimensionUnitType.Absolute;
            runtime.Width = width;
        }
        runtime.HeightUnits = DimensionUnitType.RelativeToChildren;
        runtime.Height = 0;
        runtime.Text = text;
        runtime.UpdateLayout();
        return runtime;
    }

    private static Text Renderable(TextRuntime runtime) => (Text)runtime.RenderableComponent!;

    private static string Snapshot(TextRuntime runtime)
    {
        Text text = Renderable(runtime);
        IEnumerable<string> runs = text.InlineVariables.Select(v =>
            v.VariableName + "@" + v.StartIndex + "x" + v.CharacterCount +
            (v.VariableName == "BitmapFont" ? "" : "=" + v.Value));
        return text.RawText + "|" + text.StoredMarkupText + "|" + string.Join("/", text.WrappedText) + "|" +
            string.Join(",", runs) + "|" + text.ParsedBbCodeTags?.Count + "|" +
            runtime.AbsoluteWidth + "x" + runtime.AbsoluteHeight;
    }

    private static long AllocatedBySets(TextRuntime runtime, string value, int count = 300)
    {
        runtime.Text = value;
        runtime.Text = value;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < count; i++)
        {
            runtime.Text = value;
        }
        return (GC.GetAllocatedBytesForCurrentThread() - before) / count;
    }

    #region The fix: same string

    [Fact]
    public void Text_SameTaggedString_AllocatesNothing()
    {
        TextRuntime runtime = Make(Tagged);

        AllocatedBySets(runtime, Tagged).ShouldBeLessThan(64);
    }

    [Fact]
    public void Text_SameTaggedString_AllocatesNothing_WhenRelativeToChildren()
    {
        TextRuntime runtime = Make(Tagged, relativeToChildren: true);

        AllocatedBySets(runtime, Tagged).ShouldBeLessThan(64);
    }

    [Fact]
    public void Text_SameTaggedString_DoesNotReparse()
    {
        TextRuntime runtime = Make(Tagged);
        List<InlineVariable> before = Renderable(runtime).InlineVariables.ToList();
        before.ShouldNotBeEmpty();

        runtime.Text = Tagged;

        Renderable(runtime).InlineVariables.Count.ShouldBe(before.Count);
        for (int i = 0; i < before.Count; i++)
        {
            Renderable(runtime).InlineVariables[i].ShouldBeSameAs(before[i]);
        }
    }

    [Fact]
    public void Text_SameTaggedString_DoesNotLayOutAgain()
    {
        TextRuntime runtime = Make(Tagged, relativeToChildren: true);
        int layoutsBefore = GraphicalUiElement.UpdateLayoutCallCount;

        for (int i = 0; i < 20; i++)
        {
            runtime.Text = Tagged;
        }

        GraphicalUiElement.UpdateLayoutCallCount.ShouldBe(layoutsBefore);
    }

    [Fact]
    public void SetTextNoTranslate_SameTaggedString_DoesNotReparse()
    {
        TextRuntime runtime = Make(null);
        runtime.SetTextNoTranslate(Tagged);
        InlineVariable first = Renderable(runtime).InlineVariables[0];

        runtime.SetTextNoTranslate(Tagged);

        Renderable(runtime).InlineVariables[0].ShouldBeSameAs(first);
    }

    [Fact]
    public void Text_SameTaggedStringWithCrLf_DoesNotReparse()
    {
        string crlf = "[Color=Red]one[/Color]\r\ntwo";
        TextRuntime runtime = Make(crlf);
        InlineVariable first = Renderable(runtime).InlineVariables[0];

        runtime.Text = crlf;

        Renderable(runtime).InlineVariables[0].ShouldBeSameAs(first);
    }

    [Fact]
    public void Text_DifferentTaggedString_Reparses()
    {
        TextRuntime runtime = Make("[Color=Red]one[/Color] two");

        runtime.Text = "one [Color=Blue]two[/Color]";

        Snapshot(runtime).ShouldBe(Snapshot(Make("one [Color=Blue]two[/Color]")));
        Renderable(runtime).InlineVariables.Single().StartIndex.ShouldBe(4);
    }

    [Fact]
    public void Text_SameTaggedString_ReparsesAfterRawTextChangedBehindItsBack()
    {
        TextRuntime runtime = Make("[Color=Red]one[/Color]");
        Renderable(runtime).RawText = "other";

        runtime.Text = "[Color=Red]one[/Color]";

        Renderable(runtime).RawText.ShouldBe("one");
    }

    #endregion

    #region The fix: things that affect the parse still refresh

    [Fact]
    public void Text_SameTaggedString_ReflectsFontSizeChange()
    {
        TextRuntime runtime = Make("a [FontSize=30]big[/FontSize] [Color=Red]red[/Color] b");
        runtime.FontSize = 40;
        runtime.Text = "a [FontSize=30]big[/FontSize] [Color=Red]red[/Color] b";

        TextRuntime fresh = Make(null);
        fresh.FontSize = 40;
        fresh.Text = "a [FontSize=30]big[/FontSize] [Color=Red]red[/Color] b";
        fresh.UpdateLayout();

        Snapshot(runtime).ShouldBe(Snapshot(fresh));
    }

    [Fact]
    public void Text_SameTaggedString_ReflectsBoldChange()
    {
        const string value = "a [FontSize=30]big[/FontSize] b";
        TextRuntime runtime = Make(value);
        runtime.IsBold = true;
        runtime.Text = value;

        TextRuntime fresh = Make(null);
        fresh.IsBold = true;
        fresh.Text = value;
        fresh.UpdateLayout();

        Snapshot(runtime).ShouldBe(Snapshot(fresh));
    }

    [Fact]
    public void Text_SameTaggedString_ReflectsFontChange()
    {
        const string value = "a [FontSize=30]big[/FontSize] b";
        TextRuntime runtime = Make(value);
        runtime.Font = "Verdana";
        runtime.Text = value;

        TextRuntime fresh = Make(null);
        fresh.Font = "Verdana";
        fresh.Text = value;
        fresh.UpdateLayout();

        Snapshot(runtime).ShouldBe(Snapshot(fresh));
    }

    [Fact]
    public void Text_SameTaggedString_ReflectsOutlineChange()
    {
        const string value = "a [Color=Red]red[/Color] [FontSize=30]big[/FontSize] b";
        TextRuntime runtime = Make(value);
        runtime.OutlineThickness = 2;
        runtime.Text = value;

        TextRuntime fresh = Make(null);
        fresh.OutlineThickness = 2;
        fresh.Text = value;
        fresh.UpdateLayout();

        Snapshot(runtime).ShouldBe(Snapshot(fresh));
    }

    [Fact]
    public void Text_SameTaggedString_ReflectsDropshadowChange()
    {
        const string value = "a [HasDropshadow=true]shadow[/HasDropshadow] b";
        TextRuntime runtime = Make(value);
        runtime.DropshadowOffsetX = 3;
        runtime.DropshadowOffsetY = 3;
        runtime.Text = value;

        TextRuntime fresh = Make(null);
        fresh.DropshadowOffsetX = 3;
        fresh.DropshadowOffsetY = 3;
        fresh.Text = value;
        fresh.UpdateLayout();

        Snapshot(runtime).ShouldBe(Snapshot(fresh));
    }

    [Fact]
    public void Text_SameTaggedString_ReflectsUseCustomFontChange()
    {
        const string value = "a [FontSize=30]big[/FontSize] b";
        TextRuntime runtime = Make(value);
        runtime.UseCustomFont = true;
        runtime.CustomFontFile = "Arial.fnt";
        runtime.Text = value;

        TextRuntime fresh = Make(null);
        fresh.UseCustomFont = true;
        fresh.CustomFontFile = "Arial.fnt";
        fresh.Text = value;
        fresh.UpdateLayout();

        Snapshot(runtime).ShouldBe(Snapshot(fresh));
    }

    [Fact]
    public void Text_SameTaggedString_ReflectsNarrowerWidth()
    {
        TextRuntime runtime = Make(Tagged, width: 300);
        int linesBefore = Renderable(runtime).WrappedText.Count;
        runtime.Width = 120;
        runtime.Text = Tagged;

        Renderable(runtime).WrappedText.Count.ShouldBeGreaterThan(linesBefore);
        Snapshot(runtime).ShouldBe(Snapshot(Make(Tagged, width: 120)));
    }

    [Fact]
    public void Text_SameTaggedString_ReflectsWidthUnitsChange()
    {
        TextRuntime runtime = Make(Tagged, width: 300);
        runtime.WidthUnits = DimensionUnitType.RelativeToChildren;
        runtime.Width = 0;
        runtime.Text = Tagged;
        runtime.UpdateLayout();

        Snapshot(runtime).ShouldBe(Snapshot(Make(Tagged, relativeToChildren: true)));
    }

    #endregion

    #region Neighbors: string transitions

    [Fact]
    public void Text_TaggedThenPlainThenTagged_ParsesAgain()
    {
        TextRuntime runtime = Make("[Color=Red]one[/Color] two");

        runtime.Text = "plain";
        Renderable(runtime).InlineVariables.ShouldBeEmpty();
        Renderable(runtime).StoredMarkupText.ShouldBeNull();

        runtime.Text = "[Color=Red]one[/Color] two";

        Snapshot(runtime).ShouldBe(Snapshot(Make("[Color=Red]one[/Color] two")));
        Renderable(runtime).InlineVariables.Count.ShouldBe(1);
    }

    [Fact]
    public void Text_TaggedThenNullThenTagged_ParsesAgain()
    {
        TextRuntime runtime = Make("[Color=Red]one[/Color] two");

        runtime.Text = null;
        runtime.Text.ShouldBeNull();
        Renderable(runtime).InlineVariables.ShouldBeEmpty();

        runtime.Text = "[Color=Red]one[/Color] two";

        Snapshot(runtime).ShouldBe(Snapshot(Make("[Color=Red]one[/Color] two")));
    }

    [Fact]
    public void Text_TaggedThenEmptyThenTagged_ParsesAgain()
    {
        TextRuntime runtime = Make("[Color=Red]one[/Color] two");

        runtime.Text = "";
        runtime.Text.ShouldBe("");
        Renderable(runtime).InlineVariables.ShouldBeEmpty();

        runtime.Text = "[Color=Red]one[/Color] two";

        Snapshot(runtime).ShouldBe(Snapshot(Make("[Color=Red]one[/Color] two")));
    }

    [Fact]
    public void Text_TaggedToTaggedWithFewerTags_ClearsOldInlineVariables()
    {
        TextRuntime runtime = Make("[Color=Red]a[/Color] [Color=Blue]b[/Color] [Color=Green]c[/Color]");
        Renderable(runtime).InlineVariables.Count.ShouldBe(3);

        runtime.Text = "[Color=Red]a[/Color] b c";

        Renderable(runtime).InlineVariables.Count.ShouldBe(1);
    }

    [Fact]
    public void Text_TaggedToSameLengthTagged_UsesNewColors()
    {
        TextRuntime runtime = Make("[Color=Red]abc[/Color]");

        runtime.Text = "[Color=Blue]abc[/Color]";

        Renderable(runtime).InlineVariables.Single().Value.ShouldBe(System.Drawing.Color.Blue);
    }

    [Fact]
    public void Text_SameTaggedStringAfterPlain_DoesNotKeepStaleSkipState()
    {
        TextRuntime runtime = Make("[Color=Red]one[/Color]");
        runtime.Text = "plain";
        runtime.Text = "[Color=Red]one[/Color]";
        InlineVariable first = Renderable(runtime).InlineVariables[0];

        runtime.Text = "[Color=Red]one[/Color]";

        Renderable(runtime).InlineVariables[0].ShouldBeSameAs(first);
        Renderable(runtime).InlineVariables.Count.ShouldBe(1);
    }

    [Fact]
    public void Text_SameStringWithoutClosingTag_StaysStable()
    {
        const string value = "[Color=Red]never closed";
        TextRuntime runtime = Make(value);
        string before = Snapshot(runtime);

        runtime.Text = value;

        Snapshot(runtime).ShouldBe(before);
    }

    [Fact]
    public void Text_SetThenTextNoTranslateSameString_StillLooksTagged()
    {
        TextRuntime runtime = Make("[Color=Red]one[/Color]");

        runtime.SetTextNoTranslate("[Color=Red]one[/Color]");

        Renderable(runtime).StoredMarkupText.ShouldBe("[Color=Red]one[/Color]");
        runtime.Text.ShouldBe("one");
    }

    #endregion

    #region Neighbors: link and layout

    [Fact]
    public void GetTagAtIndex_AfterSameStringSet_StillFindsLinks()
    {
        TextRuntime runtime = Make(Tagged);

        runtime.Text = Tagged;

        runtime.GetTagAtIndex("Link", 6)!.Value.Argument.ShouldBe("start");
    }

    [Fact]
    public void GetTagAtIndex_AfterChangingString_UsesNewLinks()
    {
        TextRuntime runtime = Make(Tagged);

        runtime.Text = "[Link=other]x[/Link] plain";

        runtime.GetTagAtIndex("Link", 0)!.Value.Argument.ShouldBe("other");
        runtime.GetTagAtIndex("Link", 10).ShouldBeNull();
    }

    [Fact]
    public void Text_SameTaggedString_KeepsRelativeToChildrenWidth()
    {
        TextRuntime runtime = Make(Tagged, relativeToChildren: true);
        float width = runtime.AbsoluteWidth;
        float height = runtime.AbsoluteHeight;

        runtime.Text = Tagged;
        runtime.Text = Tagged;

        runtime.AbsoluteWidth.ShouldBe(width);
        runtime.AbsoluteHeight.ShouldBe(height);
    }

    [Fact]
    public void Text_ChangedTaggedStringWhenRelativeToChildren_ResizesOnce()
    {
        TextRuntime runtime = Make("[Color=Red]short[/Color]", relativeToChildren: true);
        float shortWidth = runtime.AbsoluteWidth;

        runtime.Text = "[Color=Red]a considerably longer string[/Color]";

        runtime.AbsoluteWidth.ShouldBeGreaterThan(shortWidth);
        Snapshot(runtime).ShouldBe(Snapshot(Make("[Color=Red]a considerably longer string[/Color]", relativeToChildren: true)));
    }

    [Fact]
    public void Text_SameTaggedStringInStackedParent_DoesNotMoveSiblings()
    {
        ContainerRuntime parent = new();
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        TextRuntime first = Make(Tagged);
        TextRuntime second = Make("below");
        parent.AddChild(first);
        parent.AddChild(second);
        parent.UpdateLayout();
        float y = second.AbsoluteTop;

        first.Text = Tagged;

        second.AbsoluteTop.ShouldBe(y);
    }

    #endregion

    #region Neighbors: localization

    private class Setup : IDisposable
    {
        public LocalizationService Service = new();

        public Setup()
        {
            Dictionary<string, string[]> entries = new()
            {
                { "Tagged", new[] { "[Color=Red]English red[/Color]", "[Color=Blue]Spanish blue[/Color] mas" } },
                { "Plain", new[] { "plain English", "[Color=Green]tagged Spanish[/Color]" } },
            };
            Service.AddDatabase(entries, new List<string> { "English", "Spanish" });
            Service.CurrentLanguage = 0;
            CustomSetPropertyOnRenderable.LocalizationService = Service;
        }

        public void Dispose() => CustomSetPropertyOnRenderable.LocalizationService = null;
    }

    [Fact]
    public void Text_SameTranslatedTaggedKey_StillShowsTranslation()
    {
        using Setup setup = new();
        TextRuntime runtime = Make("Tagged");

        runtime.Text = "Tagged";

        runtime.Text.ShouldBe("English red");
        Renderable(runtime).InlineVariables.Single().Value.ShouldBe(System.Drawing.Color.Red);
    }

    [Fact]
    public void RefreshLocalization_AfterSameTaggedStringSet_RetranslatesAndReparses()
    {
        using Setup setup = new();
        TextRuntime runtime = Make("Tagged");
        runtime.AddToRoot();
        runtime.Text = "Tagged";

        setup.Service.CurrentLanguage = 1;
        Gum.GumService.Default.RefreshLocalization();

        runtime.Text.ShouldBe("Spanish blue mas");
        Renderable(runtime).InlineVariables.Single().Value.ShouldBe(System.Drawing.Color.Blue);
    }

    [Fact]
    public void RefreshLocalization_ForPlainKeyThatTranslatesToTagged_Reparses()
    {
        using Setup setup = new();
        TextRuntime runtime = Make("Plain");
        runtime.AddToRoot();

        setup.Service.CurrentLanguage = 1;
        Gum.GumService.Default.RefreshLocalization();

        runtime.Text.ShouldBe("tagged Spanish");
        Renderable(runtime).InlineVariables.Single().Value.ShouldBe(System.Drawing.Color.Green);
    }

    [Fact]
    public void RefreshLocalization_ForDirectTaggedString_KeepsTags()
    {
        using Setup setup = new();
        TextRuntime runtime = Make("[Color=Red]direct[/Color]");
        runtime.AddToRoot();
        runtime.Text = "[Color=Red]direct[/Color]";

        setup.Service.CurrentLanguage = 1;
        Gum.GumService.Default.RefreshLocalization();

        runtime.Text.ShouldBe("direct");
        Renderable(runtime).InlineVariables.Count.ShouldBe(1);
    }

    [Fact]
    public void Text_SameTaggedString_KeepsLocalizationKey()
    {
        using Setup setup = new();
        TextRuntime runtime = Make("[Color=Red]direct[/Color]");

        runtime.Text = "[Color=Red]direct[/Color]";

        CustomSetPropertyOnRenderable.TryGetLocalizationKey(runtime).ShouldBe("[Color=Red]direct[/Color]");
    }

    [Fact]
    public void SetTextNoTranslate_SameTaggedStringAfterText_ClearsLocalizationKey()
    {
        using Setup setup = new();
        TextRuntime runtime = Make("[Color=Red]direct[/Color]");
        CustomSetPropertyOnRenderable.TryGetLocalizationKey(runtime).ShouldNotBeNull();

        runtime.SetTextNoTranslate("[Color=Red]direct[/Color]");

        CustomSetPropertyOnRenderable.TryGetLocalizationKey(runtime).ShouldBeNull();
    }

    [Fact]
    public void Text_SameTaggedString_AfterLocalizationServiceRemoved_DropsLocalizationKey()
    {
        TextRuntime runtime;
        using (Setup setup = new())
        {
            runtime = Make("[Color=Red]direct[/Color]");
        }

        runtime.Text = "[Color=Red]direct[/Color]";

        CustomSetPropertyOnRenderable.TryGetLocalizationKey(runtime).ShouldBeNull();
    }

    #endregion

}
