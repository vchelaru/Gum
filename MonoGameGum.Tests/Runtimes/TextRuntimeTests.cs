using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class TextRuntimeTests
{
    [Fact]
    public void TextRuntime_ShouldWrap_WithFixedWidth()
    {
        Text.IsMidWordLineBreakEnabled = false;

        TextRuntime textRuntime = new ();
        textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        textRuntime.Width = 100; // Set a fixed width

        textRuntime.Text = "This is a long text that should wrap within the fixed width of 100 units.";

        var innerText = (Text)textRuntime.RenderableComponent;

        innerText.WrappedText.Count.ShouldBeGreaterThan(1);

        innerText.WrappedText[0].ShouldStartWith("This is a");
        innerText.WrappedText[1].ShouldNotStartWith("This is a");
    }

    [Fact]
    public void TextRuntime_ShouldNotBreakWords_IfBreakWordsWithNoWhitespaceIsFalse()
    {
        Text.IsMidWordLineBreakEnabled = false;
        TextRuntime textRuntime = new();
        textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        textRuntime.Width = 100; // Set a fixed width
        textRuntime.Text = "abcdefghijklmnopqrstuvwxyz 1abcdefghijklmnopqrstuvwxyz 12abcdefghijklmnopqrstuvwxyz";
        var innerText = (Text)textRuntime.RenderableComponent;
        innerText.WrappedText.Count.ShouldBe(3);
        innerText.WrappedText[0].ShouldBe("abcdefghijklmnopqrstuvwxyz ");
        innerText.WrappedText[1].ShouldBe("1abcdefghijklmnopqrstuvwxyz ");
        innerText.WrappedText[2].ShouldBe("12abcdefghijklmnopqrstuvwxyz");
    }

    [Fact]
    public void TextRuntime_ShouldWrap_IfOnlyLettersExist()
    {
        Text.IsMidWordLineBreakEnabled = true;
        TextRuntime textRuntime = new();
        textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        textRuntime.Width = 100; // Set a fixed width

        textRuntime.Text = "abcdefghijklmnopqrstuvwxyz";

        var innerText = (Text)textRuntime.RenderableComponent;

        innerText.WrappedText.Count.ShouldBeGreaterThan(1);
        innerText.WrappedText[0].ShouldStartWith("abc");
        innerText.WrappedText[1].ShouldNotStartWith("abc");
        char lastLine0 = innerText.WrappedText[0].Last();
        char firstCharacterInSecondLine = innerText.WrappedText[1][0];
        firstCharacterInSecondLine.ShouldBe((char)(lastLine0 + 1));
    }
//Hyphens and dashes - These are natural break points where you can wrap
//After punctuation - Periods, commas, semicolons, etc. (though be careful with decimal points)
//Between different character types - Like between letters and numbers, or letters and symbols
//Zero-width spaces - HTML &zwj; or Unicode characters specifically for this purpose

}
