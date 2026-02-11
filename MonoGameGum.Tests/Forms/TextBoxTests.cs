using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextCopy;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class TextBoxTests : BaseTestClass
{

    [Fact]
    public void CaretIndex_ShouldAdjustCaretPosition()
    {
        TextBox textBox = new();
        textBox.Text = "Hello";

        GraphicalUiElement caret = 
            (GraphicalUiElement)textBox.Visual.GetChildByNameRecursively("CaretInstance")!;

        textBox.CaretIndex = 0;
        float absolutePosition = caret.AbsoluteLeft;

        textBox.CaretIndex = 2;
        caret.AbsoluteLeft.ShouldBeGreaterThan(absolutePosition);
        float positionAt2 = caret.AbsoluteLeft;

        textBox.CaretIndex = 4;
        caret.AbsoluteLeft.ShouldBeGreaterThan(positionAt2);

        textBox.CaretIndex = 5;
        float positionAt5 = caret.AbsoluteLeft;
        textBox.CaretIndex = 6;
        caret.AbsoluteLeft.ShouldBe(positionAt5);
    }

    [Fact]
    public void CaretIndex_ShouldAdjustCaretPosition_Multiline()
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;

        textBox.HandleCharEntered('\n');
        textBox.HandleCharEntered('\n');
        textBox.HandleCharEntered('\n');

        // give it focus so that the caret is visible:
        textBox.IsFocused = true;

        GraphicalUiElement caret =
            (GraphicalUiElement)textBox.Visual.GetChildByNameRecursively("CaretInstance")!;

        textBox.CaretIndex = 0;
        float absolutePosition = caret.AbsoluteTop;

        textBox.CaretIndex = 1;
        caret.AbsoluteTop.ShouldBeGreaterThan(absolutePosition);
        float positionAt1 = caret.AbsoluteTop;

        textBox.CaretIndex = 2;
        caret.AbsoluteTop.ShouldBeGreaterThan(positionAt1);
        float positionAt2 = caret.AbsoluteTop;

        textBox.CaretIndex = 3;
        caret.AbsoluteTop.ShouldBeGreaterThan(positionAt2);
        float positionAt3 = caret.AbsoluteTop;

        textBox.CaretIndex = 4;
        caret.AbsoluteTop.ShouldBe(positionAt3); // Should not move past the last line
    }

    [Fact]
    public void Click_ShouldSetIsFocused()
    {
        TextBox textBox = new();
        textBox.Visual.CallClick();
        textBox.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleCharEntered_ShouldUpdateBindingSource_OnEnterNoAcceptsReturn()
    {
        TextBox textBox = new();
        TestViewModel vm = new ()
        {
            Text = null
        };
        textBox.BindingContext = vm;
        Binding binding = new (() => textBox.Text)
        {
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
        };

        textBox.SetBinding(nameof(textBox.Text), binding);

        textBox.AcceptsReturn = false;
        textBox.Text = "Test text";
        textBox.HandleCharEntered('\n'); // Simulate Enter key press

        vm.Text.ShouldBe("Test text");
    }

    [Fact]
    public void HandleCharEntered_ShouldNotUpdateBindingSource_OnEnterAcceptsReturn()
    {

        TextBox textBox = new();
        TestViewModel vm = new ()
        {
            Text = null
        };
        textBox.BindingContext = vm;
        Binding binding = new (() => textBox.Text)
        {
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
        };

        textBox.SetBinding(nameof(textBox.Text), binding);

        textBox.IsFocused = true;
        textBox.AcceptsReturn = true;
        textBox.Text = "Test text";
        textBox.CaretIndex = 0;
        textBox.HandleCharEntered('\n'); // Simulate Enter key press

        vm.Text.ShouldBe(null);

        textBox.IsFocused = false;

        vm.Text.ShouldBe("\nTest text");
    }

    [Fact]
    public void HandleCharEntered_ShouldAddMultipleLines_IfWrap()
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.IsFocused = true;
        textBox.Width = 50;
        textBox.AcceptsReturn = true;

        for(int i = 0; i < 10; i++)
        {
            textBox.HandleCharEntered('a');
            textBox.HandleCharEntered(' ');
        }

        var textInstance = (TextRuntime)textBox.Visual.GetChildByNameRecursively("TextInstance")!;
        var innerTextObject = (RenderingLibrary.Graphics.Text)textInstance.RenderableComponent;
        innerTextObject.WrappedText.Count.ShouldBeGreaterThan(1);
    }

    [Fact]
    public void HandleKeyDown_Paste_ShouldReplaceSlashRSlashN_WithSlashN()
    {
        Gum.Clipboard.ClipboardImplementation.PushStringToClipboard("Line1\r\nLine2");

        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.IsFocused = true;
        textBox.HandleKeyDown(Microsoft.Xna.Framework.Input.Keys.V, false, false, isCtrlDown: true);
        textBox.Text.ShouldBe("Line1\nLine2", "because TextBox expects a single-character newline for proper caret positioning");
    }

    [Fact]
    public void asdf()
    {
        TextBox textBox = new();
        DefaultTextBoxRuntime visual = (DefaultTextBoxRuntime)textBox.Visual;

    }


    [Fact]
    public void IsFocused_ShouldRaiseLostFocus_IfIsFocusedSetToFalse()
    {
        TextBox textBox = new();
        bool lostFocusRaised = false;
        textBox.LostFocus += (s, e) => lostFocusRaised = true;
        textBox.IsFocused = true;
        textBox.IsFocused = false;
        lostFocusRaised.ShouldBeTrue();
    }

    [Fact]
    public void TextBox_ShouldHaveSelectionInstance()
    {
        var textBox = new TextBox();
        var selection = (ColoredRectangleRuntime)textBox
            .Visual.GetChildByNameRecursively("SelectionInstance")!;

        selection.Color = Microsoft.Xna.Framework.Color.Blue;

        selection.ShouldNotBeNull();
    }

    #region Visual
    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        TextBox sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    #endregion

    [Fact]
    public void TextWrapping_NoWrap_ShouldRenderCorrectlyWithAcceptsReturn()
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.NoWrap;
        textBox.AcceptsReturn = true;
        textBox.IsFocused = true;

        DefaultTextBoxBaseRuntime visual =
            (DefaultTextBoxBaseRuntime)textBox.Visual;

        float originalCaretX = visual.CaretInstance.X;
        float originalCaretY = visual.CaretInstance.Y;

        float textInstanceX = visual.TextInstance.X;


        for(int i = 0; i < 40; i++)
        {
            textBox.HandleCharEntered('a');
        }

        visual.CaretInstance.X.ShouldBeGreaterThan(originalCaretX);
        visual.CaretInstance.Y.ShouldBe(originalCaretY);
        visual.TextInstance.X.ShouldBeLessThan(textInstanceX, "because the text scrolled to the left");

        textBox.HandleCharEntered('\n');
        visual.CaretInstance.X.ShouldBe(originalCaretX, 
            // Add some tolerance because text positioning is based on caret size, and this is different than the
            // starting Text value. Oh well...
            tolerance:2, 
            customMessage:"because the caret should reset to the start of the line after a newline");
        visual.CaretInstance.Y.ShouldBeGreaterThan(originalCaretY, "because the caret should move down after a newline");
        visual.TextInstance.X.ShouldBe(textInstanceX, 
            // See above why we add tolerance:
            tolerance:2,
            customMessage:"because the text should not scroll to the left after a newline in NoWrap mode");
    }


    [Fact]
    public void AcceptsReturn_ShouldAddMultipleLines_OnEnterPress()
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;

        textBox.HandleCharEntered('1');
        textBox.HandleCharEntered('\n');
        textBox.HandleCharEntered('2');
        textBox.HandleCharEntered('\n');
        textBox.HandleCharEntered('3');


        var textInstance =
            (TextRuntime)textBox.Visual.GetChildByNameRecursively("TextInstance")!;

        var innerTextObject = (RenderingLibrary.Graphics.Text)textInstance.RenderableComponent;

        innerTextObject.WrappedText.Count.ShouldBe(3);
        innerTextObject.WrappedText[0].Trim().ShouldBe("1");
        innerTextObject.WrappedText[1].Trim().ShouldBe("2");
        innerTextObject.WrappedText[2].Trim().ShouldBe("3");
    }

    [Fact]
    public void TextBox_ShouldRestrictLinesWhenMaxLinesSetBefore()
    {
        var textBox = new TextBox();
        textBox.MaxNumberOfLines = 3;
        textBox.Text = "line1\nline2\nline3\nline4\nline5";
        
        var textInstance = (TextRuntime)textBox
            .Visual.GetChildByNameRecursively("TextInstance")!;

        var innerTextObject = (RenderingLibrary.Graphics.Text)textInstance.RenderableComponent;

        innerTextObject.WrappedText.Count.ShouldBe(3);
        innerTextObject.WrappedText[0].ShouldBe("line1\n");
        innerTextObject.WrappedText[1].ShouldBe("line2\n");
        innerTextObject.WrappedText[2].ShouldBe("line3\n");
    }

    [Fact]
    public void TextBox_ShouldRestrictLinesWhenMaxLinesSetAfter()
    {
        var textBox = new TextBox();
        textBox.Text = "line1\nline2\nline3\nline4\nline5";
        textBox.MaxNumberOfLines = 3;

        var textInstance = (TextRuntime)textBox
            .Visual.GetChildByNameRecursively("TextInstance")!;

        var innerTextObject = (RenderingLibrary.Graphics.Text)textInstance.RenderableComponent;

        innerTextObject.WrappedText.Count.ShouldBe(3);
        innerTextObject.WrappedText[0].ShouldBe("line1\n");
        innerTextObject.WrappedText[1].ShouldBe("line2\n");
        innerTextObject.WrappedText[2].ShouldBe("line3\n");
    }

    [Fact]
    public void TextBox_ShouldNotRestrictLinesWhenMaxLinesIsNull()
    {
        var textBox = new TextBox();
        textBox.Text = "line1\nline2\nline3\nline4\nline5";
        textBox.MaxNumberOfLines = null;

        var textInstance = (TextRuntime)textBox
            .Visual.GetChildByNameRecursively("TextInstance")!;

        var innerTextObject = (RenderingLibrary.Graphics.Text)textInstance.RenderableComponent;

        innerTextObject.WrappedText.Count.ShouldBe(5);
        innerTextObject.WrappedText[0].ShouldBe("line1\n");
        innerTextObject.WrappedText[1].ShouldBe("line2\n");
        innerTextObject.WrappedText[2].ShouldBe("line3\n");
        innerTextObject.WrappedText[3].ShouldBe("line4\n");
        innerTextObject.WrappedText[4].ShouldBe("line5");
    }

    [Fact]
    public void TextBox_ShouldPassDownMaxLettersToShowToInnerTextInstance()
    {
        var textBox = new TextBox();

        // Unfortuentely the MaxLettersToShow is only used during the RENDERING 
        // in the Text.DrawWithInlineVariables method.  This means that we
        // have no way to get the actual "Displayed" or "Drawn" text in a unit test (yet?).
        //textBox.Text = "abcdefghijklmnopqrstuvwxyz";
        textBox.MaxLettersToShow = 3;

        var textInstance = (TextRuntime)textBox
            .Visual.GetChildByNameRecursively("TextInstance")!;

        textInstance.MaxLettersToShow.ShouldBe(3);

        var innerTextObject = (RenderingLibrary.Graphics.Text)textInstance.RenderableComponent;

        innerTextObject.MaxLettersToShow.ShouldBe(3);
    }


    #region ViewModels

    class TestViewModel : ViewModel
    {
        public string? Text
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    #endregion
}