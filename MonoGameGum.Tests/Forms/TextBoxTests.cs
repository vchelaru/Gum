using Gum.Mvvm;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.Data;
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

        textBox.AcceptsReturn = false;
        textBox.Text = "Test text";
        textBox.HandleCharEntered('\n'); // Simulate Enter key press

        vm.Text.ShouldBe(null);

        textBox.IsFocused = false;

        vm.Text.ShouldBe("Test text\n");
    }


    class TestViewModel : ViewModel
    {
        public string? Text
        {
            get => Get<string>();
            set => Set(value);
        }
    }
}