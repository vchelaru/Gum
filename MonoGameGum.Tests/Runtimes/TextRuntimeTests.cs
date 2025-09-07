using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class TextRuntimeTests
{
    [Fact]
    public void AbsoluteWidth_ShouldNotIncludeNewlines()
    {
        TextRuntime textRuntime = new();

        var character = textRuntime.BitmapFont.Characters['\n'];
        character.XAdvance = 10;

        textRuntime.Text = "Hello";

        var widthBefore = textRuntime.GetAbsoluteWidth();

        textRuntime.Text = "Hello\na";

        var widthAfter = textRuntime.GetAbsoluteWidth();

        widthBefore.ShouldBe(widthAfter, "Because a trailing newline should not affect the width of a text, regardless of its XAdavance");
    }

    [Fact]
    public void WrappedText_ShouldWrap_WithFixedWidth()
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
    public void WrappedText_ShouldNotBreakWords_IfBreakWordsWithNoWhitespaceIsFalse()
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
    public void WrappedText_ShouldWrap_IfOnlyLettersExist()
    {
        Text text = new();
        Text.IsMidWordLineBreakEnabled = true;
        text.Width = 100;

        text.RawText = "abcdefghijklmnopqrstuvwxyz";

        text.WrappedText.Count.ShouldBeGreaterThan(1);
        text.WrappedText[0].ShouldStartWith("abc");
        text.WrappedText[1].ShouldNotStartWith("abc");
        text.WrappedText[1].ShouldStartWith("mno");
        char lastLine0 = text.WrappedText[0].Last();
        char firstCharacterInSecondLine = text.WrappedText[1][0];
        firstCharacterInSecondLine.ShouldBe((char)(lastLine0 + 1));
    }
    //Hyphens and dashes - These are natural break points where you can wrap
    //After punctuation - Periods, commas, semicolons, etc. (though be careful with decimal points)
    //Between different character types - Like between letters and numbers, or letters and symbols
    //Zero-width spaces - HTML &zwj; or Unicode characters specifically for this purpose

    [Fact]
    public void WrappedText_ShouldWrapMidWord_WithMultipleLines()
    {
        // bypassing TextRuntime to test this directly:
        var text = new Text();
        text.Width = 14;
        Text.IsMidWordLineBreakEnabled = true;

        text.RawText = "01\n01";

        text.WrappedText.Count.ShouldBe(4);
        text.WrappedText[0].ShouldBe("0");
        text.WrappedText[1].ShouldBe("1\n");
        text.WrappedText[2].ShouldBe("0");
        text.WrappedText[3].ShouldBe("1");
    }

    [Fact]
    public void WrappedText_ShouldWrapMidWord_WithMultipleWords()
    {
        // bypassing TextRuntime to test this directly:
        var text = new Text();
        text.Width = 14;
        Text.IsMidWordLineBreakEnabled = true;

        text.RawText = "01 01";

        text.WrappedText.Count.ShouldBe(4);
        text.WrappedText[0].ShouldBe("0");
        text.WrappedText[1].ShouldBe("1 ");
        text.WrappedText[2].ShouldBe("0");
        text.WrappedText[3].ShouldBe("1");
    }

    [Fact]
    public void WrappedText_ShouldWrapMidWord_IfWidthMatchesLetterWidthExactly()
    {
        // each letter is 10 wide, so let's set a width that is a multiple of that:
        Text text = new();
        Text.IsMidWordLineBreakEnabled = true;
        text.Width = 30;

        text.RawText = "abcdefghijklmnopqrstuvwxyz";

        text.WrappedText.Count.ShouldBe(9);
        text.WrappedText[0].ShouldNotBeEmpty("abc");
        text.WrappedText[1].ShouldNotBeEmpty("def");
        text.WrappedText[2].ShouldNotBeEmpty("ghi");
        text.WrappedText[3].ShouldNotBeEmpty("jkl");
    }

}
