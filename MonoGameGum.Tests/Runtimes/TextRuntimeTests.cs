using Gum.DataTypes;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;
using Gum.Localization;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class TextRuntimeTests : BaseTestClass
{

    const string fontPattern =
$"info face=\"Arial\" size=-18 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
$"common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\r\n" +
$"chars count=223\r\n";
    Mock<ILocalizationService> _localizationService;


    public TextRuntimeTests()
    {
        _localizationService = new();

    }

    #region AbsoluteWidth

    [Fact]
    public void AbsoluteWidth_ShouldBeChangedByText_IfRelativeToChildren()
    {
        TextRuntime sut = new();
        sut.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        sut.Width = 0;
        sut.Text = "Short";
        float shortWidth = sut.GetAbsoluteWidth();

        sut.Text = "This is much longer";
        float longWidth = sut.GetAbsoluteWidth();

        longWidth.ShouldBeGreaterThan(shortWidth);
    }

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

    #endregion

    #region GetStyledSubstrings

    [Fact]
    public void GetStyledSubstrings_ShouldReturnTwoEntries_IfTextHasCodeAtEnd()
    {
        // Arrange
        var text = new Text();
        text.InlineVariables.Add(new InlineVariable
        {
            VariableName = "IsBold",
            Value = true,
            StartIndex = 6,
            CharacterCount = 5
        });

        // Act
        var substrings = text.GetStyledSubstrings(0, "Hello World", System.Drawing.Color.White);
        // Assert
        substrings.Count.ShouldBe(2);
        substrings[0].Substring.ShouldBe("Hello ");
        substrings[0].Variables.Count.ShouldBe(0);

        substrings[1].Substring.ShouldBe("World");
        substrings[1].Variables.Count.ShouldBe(1);
        substrings[1].Variables[0].VariableName.ShouldBe("IsBold");
        substrings[1].Variables[0].Value.ShouldBe(true);
    }

    [Fact]
    public void GetStyledSubstrings_ShouldReturnThreeEntries_IfTextHasCodeInMiddle()
    {
        // Arrange
        var text = new Text();
        text.InlineVariables.Add(new InlineVariable
        {
            VariableName = "IsBold",
            Value = true,
            StartIndex = 1,
            CharacterCount = 1
        });

        // Act
        var substrings = text.GetStyledSubstrings(0, "012", System.Drawing.Color.White);
        // Assert
        substrings.Count.ShouldBe(3);
        substrings[0].Substring.ShouldBe("0");
        substrings[0].Variables.Count.ShouldBe(0);

        substrings[1].Substring.ShouldBe("1");
        substrings[1].Variables.Count.ShouldBe(1);
        substrings[1].Variables[0].VariableName.ShouldBe("IsBold");
        substrings[1].Variables[0].Value.ShouldBe(true);

        substrings[2].Substring.ShouldBe("2");
        substrings[2].Variables.Count.ShouldBe(0);
    }

    [Fact]
    public void GetStyledSubstrings_ShouldRespectOverlappingCodes()
    {
        var text = new Text();
        text.InlineVariables.Add(new InlineVariable
        {
            VariableName = "IsBold",
            Value = true,
            StartIndex = 1,
            CharacterCount = 3
        });

        text.InlineVariables.Add(new InlineVariable
        {
            VariableName = "FontScale",
            Value = 2,
            StartIndex = 2,
            CharacterCount = 1
        });

        // Act
        var substrings = text.GetStyledSubstrings(0, "01234", System.Drawing.Color.White);
        // Assert
        substrings.Count.ShouldBe(5);

        substrings[0].Substring.ShouldBe("0");
        substrings[0].Variables.Count.ShouldBe(0);

        substrings[1].Substring.ShouldBe("1");
        substrings[1].Variables.Count.ShouldBe(1);
        substrings[1].Variables[0].VariableName.ShouldBe("IsBold");
        substrings[1].Variables[0].Value.ShouldBe(true);

        substrings[2].Substring.ShouldBe("2");
        substrings[2].Variables.Count.ShouldBe(2);
        substrings[2].Variables[0].VariableName.ShouldBe("IsBold");
        substrings[2].Variables[0].Value.ShouldBe(true);
        substrings[2].Variables[1].VariableName.ShouldBe("FontScale");
        substrings[2].Variables[1].Value.ShouldBe(2);

        substrings[3].Substring.ShouldBe("3");
        substrings[3].Variables.Count.ShouldBe(1);
        substrings[3].Variables[0].VariableName.ShouldBe("IsBold");
        substrings[3].Variables[0].Value.ShouldBe(true);

        substrings[4].Substring.ShouldBe("4");
        substrings[4].Variables.Count.ShouldBe(0);
    }

    [Fact]
    public void GetStyledSubstrings_ShouldRespectOverlappingCodes_OfSameVariable()
    {
        var text = new Text();
        text.InlineVariables.Add(new InlineVariable
        {
            VariableName = "FontScale",
            Value = 2,
            StartIndex = 1,
            CharacterCount = 3
        });

        text.InlineVariables.Add(new InlineVariable
        {
            VariableName = "FontScale",
            Value = 3,
            StartIndex = 2,
            CharacterCount = 1
        });

        // Act
        var substrings = text.GetStyledSubstrings(0, "01234", System.Drawing.Color.White);
        // Assert
        substrings.Count.ShouldBe(5);

        substrings[0].Substring.ShouldBe("0");
        substrings[0].Variables.Count.ShouldBe(0);

        substrings[1].Substring.ShouldBe("1");
        substrings[1].Variables.Count.ShouldBe(1);
        substrings[1].Variables[0].VariableName.ShouldBe("FontScale");
        substrings[1].Variables[0].Value.ShouldBe(2);

        substrings[2].Substring.ShouldBe("2");
        substrings[2].Variables.Count.ShouldBe(1);
        substrings[2].Variables[0].VariableName.ShouldBe("FontScale");
        substrings[2].Variables[0].Value.ShouldBe(3);

        substrings[3].Substring.ShouldBe("3");
        substrings[3].Variables.Count.ShouldBe(1);
        substrings[3].Variables[0].VariableName.ShouldBe("FontScale");
        substrings[3].Variables[0].Value.ShouldBe(2);

        substrings[4].Substring.ShouldBe("4");
        substrings[4].Variables.Count.ShouldBe(0);
    }

    #endregion

    #region HasEvents

    [Fact]
    public void HasEvents_ShouldDefaultToFalse()
    {
        TextRuntime sut = new();
        sut.HasEvents.ShouldBeFalse();
    }

    #endregion

    #region Text (including bbcode and localization)

    [Fact]
    public void Text_WithSlashRSlashN_ShouldSetBbCodeCorrectly()
    {
        string text = $"[Color=Green]0[/Color]1\r\n[Color=Green]0[/Color]1";

        TextRuntime textRuntime = new ();
        textRuntime.Text = text;

        var internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        var inlineVariables = internalText.InlineVariables;

        inlineVariables.Count.ShouldBe(2);
        inlineVariables[0].StartIndex.ShouldBe(0);
        inlineVariables[0].CharacterCount.ShouldBe(1);

        inlineVariables[1].StartIndex.ShouldBe(3, "Because \\r character should not be included, so the newline 0 character starts at index 3");
        inlineVariables[1].CharacterCount.ShouldBe(1);
    }

    [Fact]
    public void Text_WithCustom_ShouldAssignMethodCall()
    {
        var method = (int index, string block) =>
        {
            return new LetterCustomization();
        };
        Text.Customizations["CustomMethod"] = method;

        string text = $"Hello [Custom=CustomMethod]custom[/Custom]";

        TextRuntime textRuntime = new();
        textRuntime.Text = text;

        var internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        var inlineVariables = internalText.InlineVariables;

        inlineVariables.Count.ShouldBe(6);
        foreach(var variable in inlineVariables)
        {
            ParameterizedLetterCustomizationCall asCall = (ParameterizedLetterCustomizationCall)variable.Value;
            asCall.Function.ShouldBe(method);
        }
    }

    [Fact]
    public void Text_WithCustom_ShouldAssignMethods()
    {
        string text = $"Hello [Custom=CustomMethod]custom[/Custom]";

        TextRuntime textRuntime = new();
        textRuntime.Text = text;

        var method = (int index, string block) =>
        {
            return new LetterCustomization();
        };
        Text.Customizations["CustomMethod"] = method;

        var internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        var inlineVariables = internalText.InlineVariables;

        inlineVariables.Count.ShouldBe(6);
        foreach (var variable in inlineVariables)
        {
            ParameterizedLetterCustomizationCall asCall = (ParameterizedLetterCustomizationCall)variable.Value;
            asCall.Function.ShouldBe(method);
        }
    }


    [Fact]
    public void Text_ShouldUseLocalization()
    {
        TextRuntime textRuntime = new();

        CustomSetPropertyOnRenderable.LocalizationService = _localizationService.Object;
        _localizationService.Setup(x => x.Translate("T_StringId")).Returns("This is a localized string");
        textRuntime.Text = "T_StringId";

        textRuntime.Text.ShouldBe("This is a localized string");
    }

    [Fact]
    public void Text_WithLocalization_ShouldSetBbCodeCorrectly()
    {
        CustomSetPropertyOnRenderable.LocalizationService = _localizationService.Object;
        _localizationService
            .Setup(x => x.Translate("T_StringId"))
            .Returns("[Color=Green]0[/Color]1\r\n[Color=Green]0[/Color]1");

        TextRuntime textRuntime = new();
        textRuntime.Text = "T_StringId";

        var internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        var inlineVariables = internalText.InlineVariables;

        inlineVariables.Count.ShouldBe(2);
        inlineVariables[0].StartIndex.ShouldBe(0);
        inlineVariables[0].CharacterCount.ShouldBe(1);

        inlineVariables[1].StartIndex.ShouldBe(3, "Because \\r character should not be included, so the newline 0 character starts at index 3");
        inlineVariables[1].CharacterCount.ShouldBe(1);
    }

    #endregion

    #region WrappedText

    [Fact]
    public void WrappedText_ShouldWrap_WithFixedWidth()
    {
        Text.IsMidWordLineBreakEnabled = false;

        TextRuntime textRuntime = new ();
        textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        textRuntime.Width = 100; // Set a fixed width

        textRuntime.Text = "This is a long text that should wrap within the fixed width of 100 units.";

        textRuntime.WrappedText.Count.ShouldBeGreaterThan(1);

        textRuntime.WrappedText[0].ShouldStartWith("This is a");
        textRuntime.WrappedText[1].ShouldNotStartWith("This is a");
    }

    [Fact]
    public void WrappedText_ShouldNotBreakWords_IfBreakWordsWithNoWhitespaceIsFalse()
    {
        Text.IsMidWordLineBreakEnabled = false;
        TextRuntime textRuntime = new();
        textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        textRuntime.Width = 100; // Set a fixed width
        textRuntime.Text = "abcdefghijklmnopqrstuvwxyz 1abcdefghijklmnopqrstuvwxyz 12abcdefghijklmnopqrstuvwxyz";
        
        textRuntime.WrappedText.Count.ShouldBe(3);
        textRuntime.WrappedText[0].ShouldBe("abcdefghijklmnopqrstuvwxyz ");
        textRuntime.WrappedText[1].ShouldBe("1abcdefghijklmnopqrstuvwxyz ");
        textRuntime.WrappedText[2].ShouldBe("12abcdefghijklmnopqrstuvwxyz");
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

    #endregion

    #region MaxWidth

    [Fact]
    public void MaxWidth_ShouldWrapText_IfTextExceedsMaxWidth()
    {
        TextRuntime textRuntime = new();
        textRuntime.Width = 0;
        textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        textRuntime.MaxWidth = 50; // Set a max width
        textRuntime.Text = "a a a a a a a a a a a a a a a a a";

        textRuntime.GetAbsoluteWidth().ShouldBeLessThanOrEqualTo(50);
        var innerText = (Text)textRuntime.RenderableComponent;
        innerText.WrappedText.Count.ShouldBeGreaterThan(1);
        var lineCount = innerText.WrappedText.Count;

        var absoluteHeight = textRuntime.GetAbsoluteHeight();
        absoluteHeight.ShouldBe(lineCount * textRuntime.BitmapFont.LineHeightInPixels);
    }

    #endregion

    #region Clone
    [Fact]
    public void Clone_ShouldCreateClonedText()
    {
        Text sut = new();
        var clone = sut.Clone();
        clone.ShouldNotBeNull();
    }

    #endregion

    #region PropertyChanged

    [Fact]
    public void PropertyChanged_ShouldRaise_WhenTextChanges()
    {
        bool wasChanged = false;
        TextRuntime textRuntime = new();
        textRuntime.PropertyChanged += (_, _) =>
        {
            wasChanged = true;
        };

        textRuntime.Text = "Hello 1234";

        wasChanged.ShouldBeTrue();
    }

    #endregion

    [Fact]
    public void IsBold_ShouldChangeFont_OnFontPropertiesSet()
    {
        // file name is:
        // FontCache\Font18SomeFont_Italic_Bold.fnt
        var italicBoldFont = new BitmapFont((Texture2D)null!, fontPattern);
        var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
        string fileName = FileManager.Standardize("FontCache\\Font18SomeFont_Italic_Bold.fnt", preserveCase: true, makeAbsolute: true);
        loaderManager.AddDisposable(fileName, italicBoldFont);

        TextRuntime sut = new();
        sut.UseCustomFont = true;
        // set up all the properties:
        sut.FontSize = 18;
        sut.Font = "SomeFont";
        sut.IsItalic = true;

        sut.UseCustomFont = false;

        sut.IsBold = true;

        sut.BitmapFont.ShouldBe(italicBoldFont);
    }

    [Fact]
    public void UseCustomFont_ShouldChangeFont_OnFontPropertiesSet()
    {
        // file name is:
        // FontCache\Font18SomeFont_Italic_Bold.fnt
        var italicBoldFont = new BitmapFont((Texture2D)null!, fontPattern);
        var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
        string fileName = FileManager.Standardize("FontCache\\Font18SomeFont_Italic_Bold.fnt", preserveCase:true, makeAbsolute:true);
        loaderManager.AddDisposable(fileName, italicBoldFont);

        TextRuntime sut = new();
        sut.UseCustomFont = true;
        // set up all the properties:
        sut.FontSize = 18;
        sut.Font = "SomeFont";
        sut.IsBold = true;
        sut.IsItalic = true;

        sut.UseCustomFont = false;

        sut.BitmapFont.ShouldBe(italicBoldFont);
    }

    [Fact]
    public void MaxNumberOfLetters_ShouldNotChangeDimensions()
    {
        TextRuntime textRuntime = new TextRuntime();
        textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        textRuntime.Width = 0;
        textRuntime.Text = "This is some sample text";

        textRuntime.GetAbsoluteWidth().ShouldBeGreaterThan(0);
        var absoluteWidth = textRuntime.GetAbsoluteWidth();

        textRuntime.MaxLettersToShow = 0;

        textRuntime.GetAbsoluteWidth().ShouldBe(absoluteWidth);


    }

    [Fact]
    public void Anchor_CenterHorizontally_ShouldSetCorrectValues()
    {
        TextRuntime textRuntime = new TextRuntime();
        textRuntime.HorizontalAlignment = HorizontalAlignment.Right;
        textRuntime.VerticalAlignment = VerticalAlignment.Bottom;
        textRuntime.Text = "This is some sample text";
        textRuntime.Anchor(Anchor.CenterHorizontally);

        // Should set center
        textRuntime.HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);

        // Should not change vertical
        textRuntime.VerticalAlignment.ShouldBe(VerticalAlignment.Bottom);
    }

    [Fact]
    public void Anchor_CenterVertically_ShouldSetCorrectValues()
    {
        TextRuntime textRuntime = new TextRuntime();
        textRuntime.HorizontalAlignment = HorizontalAlignment.Right;
        textRuntime.VerticalAlignment = VerticalAlignment.Bottom;
        textRuntime.Text = "This is some sample text";
        textRuntime.Anchor(Anchor.CenterVertically);

        // Should not change Horizontal
        textRuntime.HorizontalAlignment.ShouldBe(HorizontalAlignment.Right);

        // Should set Center
        textRuntime.VerticalAlignment.ShouldBe(VerticalAlignment.Center);
    }


}
