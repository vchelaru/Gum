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
public class PasswordBoxTests
{
    [Fact]
    public void HandleCharEntered_ShouldUpdateBindingSource_OnEnter()
    {
        PasswordBox passwordBox = new();
        TestViewModel vm = new()
        {
            Password = null
        };
        passwordBox.BindingContext = vm;
        Binding binding = new(() => passwordBox.Password)
        {
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
        };
        passwordBox.SetBinding(nameof(passwordBox.Password), binding);
        passwordBox.Password = "TestPassword";
        passwordBox.HandleCharEntered('\n'); // Simulate Enter key press
        vm.Password.ShouldBe("TestPassword");
    }


    class TestViewModel : ViewModel
    {
        public string? Password
        {
            get => Get<string?>();
            set => Set(value);
        }
    }
}