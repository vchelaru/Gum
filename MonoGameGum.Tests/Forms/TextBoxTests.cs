﻿using Gum.Mvvm;
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