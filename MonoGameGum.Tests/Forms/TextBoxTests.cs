using Gum.Mvvm;
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