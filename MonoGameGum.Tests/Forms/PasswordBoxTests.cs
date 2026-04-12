using Gum.Mvvm;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class PasswordBoxTests : BaseTestClass
{
    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        PasswordBox sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

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

    [Fact]
    public void Password_ShouldUpdateBinding()
    {
        PasswordBox passwordBox = new();
        TestViewModel vm = new()
        {
            Password = null
        };

        passwordBox.BindingContext = vm;


        passwordBox.SetBinding(nameof(passwordBox.Password), nameof(vm.Password));

        passwordBox.Password = "NewPassword";


        vm.Password.ShouldBe("NewPassword");
    }


    [Fact]
    public void NativeKeyboardInput_ShouldSetPassword()
    {
        var passwordBox = new NativeKeyboardAccessPasswordBox();
        passwordBox.InvokeSetTextFromNativeKeyboardInput("hunter2");
        passwordBox.Password.ShouldBe("hunter2");
    }

    [Fact]
    public void NativeKeyboardPasswordMode_ShouldBeTrue_ForPasswordBox()
    {
        var passwordBox = new NativeKeyboardAccessPasswordBox();
        passwordBox.GetUseNativeKeyboardPasswordMode().ShouldBeTrue();
    }

    class NativeKeyboardAccessPasswordBox : PasswordBox
    {
        public void InvokeSetTextFromNativeKeyboardInput(string value)
            => SetTextFromNativeKeyboardInput(value);

        public bool GetUseNativeKeyboardPasswordMode() => UseNativeKeyboardPasswordMode;
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