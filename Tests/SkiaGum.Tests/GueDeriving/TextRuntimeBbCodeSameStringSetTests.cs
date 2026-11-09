using Gum.DataTypes;
using Gum.GueDeriving;
using Shouldly;
using System;
using Xunit;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// Setting a BBCode text to the string it already shows must not allocate (#7779). Skia parses markup
/// lazily and its Text.RawText setter ignores an equal string, so this pins that it stays that way.
/// </summary>
public class TextRuntimeBbCodeSameStringSetTests : BaseTestClass
{
    const string Tagged = "Press [Link=a][Color=Red]Start[/Color][/Link] to begin, or [Color=Blue]Help[/Color] to read more.";

    public TextRuntimeBbCodeSameStringSetTests()
    {
        new RenderingLibrary.SystemManagers().Initialize();
    }

    [Fact]
    public void Text_SameTaggedString_AllocatesNothing()
    {
        TextRuntime runtime = new();
        runtime.WidthUnits = DimensionUnitType.Absolute;
        runtime.Width = 300;
        runtime.Text = Tagged;
        runtime.Text = Tagged;
        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < 300; i++)
        {
            runtime.Text = Tagged;
        }

        ((GC.GetAllocatedBytesForCurrentThread() - before) / 300).ShouldBeLessThan(256);
    }

    [Fact]
    public void Text_ChangedTaggedString_ShowsNewMarkup()
    {
        TextRuntime runtime = new();
        runtime.Text = Tagged;

        runtime.Text = "[Color=Green]x[/Color] plain";

        ((SkiaGum.Text)runtime.RenderableComponent!).StoredMarkupText.ShouldBe("[Color=Green]x[/Color] plain");
    }
}
