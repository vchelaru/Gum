﻿using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.Data;
using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class TextBoxTests
{

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
    public void TextBox_ShouldHaveSelectionInstance()
    {
        var textBox = new TextBox();
        var selection = (ColoredRectangleRuntime)textBox
            .Visual.GetChildByNameRecursively("SelectionInstance")!;

        selection.Color = Microsoft.Xna.Framework.Color.Blue;

        selection.ShouldNotBeNull();
    }

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
        textBox.TextWrapping = MonoGameGum.Forms.TextWrapping.Wrap;
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
    public void AcceptsReturn_ShouldAddMultipleLines_OnEnterPress()
    {
        TextBox textBox = new();
        textBox.TextWrapping = MonoGameGum.Forms.TextWrapping.Wrap;
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