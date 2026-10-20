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

    [Fact]
    public void Visual_ShouldShowPlaceholder_WhenAssignedVisualWithHiddenPlaceholderToEmptyPasswordBox()
    {
        PasswordBox passwordBox = new();
        Gum.Forms.DefaultVisuals.V3.PasswordBoxVisual newVisual = new(tryCreateFormsObject: false);
        newVisual.PlaceholderTextInstance.Visible = false;

        passwordBox.Visual = newVisual;

        newVisual.PlaceholderTextInstance.Visible.ShouldBeTrue(
            "because the password is empty, regardless of the placeholder state the visual was authored with");
    }

    [Fact]
    public void Visual_ShouldHidePlaceholder_WhenAssignedVisualToPasswordBoxWithPassword()
    {
        PasswordBox passwordBox = new();
        passwordBox.Password = "secret";
        Gum.Forms.DefaultVisuals.V3.PasswordBoxVisual newVisual = new(tryCreateFormsObject: false);
        newVisual.PlaceholderTextInstance.Visible = true;

        passwordBox.Visual = newVisual;

        newVisual.PlaceholderTextInstance.Visible.ShouldBeFalse(
            "because the password box has a password, so the placeholder must not draw over it");
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

    [Fact]
    public void Visual_ShouldTakeAuthoredPlaceholderAndHideAuthoredSelection_WhenAssignedToFreshPasswordBox()
    {
        Gum.Forms.DefaultVisuals.V3.PasswordBoxVisual authoredVisual = new(tryCreateFormsObject: false);
        authoredVisual.PlaceholderTextInstance.Text = "authored placeholder";
        authoredVisual.SelectionInstance.Visible = true;

        PasswordBox passwordBox = new(authoredVisual);

        passwordBox.Placeholder.ShouldBe("authored placeholder");
        passwordBox.CaretIndex.ShouldBe(0);
        passwordBox.SelectionLength.ShouldBe(0);
        authoredVisual.SelectionInstance.Visible.ShouldBeFalse();
    }

    [Fact]
    public void Visual_ShouldKeepCaretAndSelection_WhenReassignedWithPassword()
    {
        PasswordBox passwordBox = new();
        passwordBox.Password = "secret";
        passwordBox.SelectionStart = 1;
        passwordBox.SelectionLength = 3;
        passwordBox.CaretIndex = 4;

        passwordBox.Visual = new Gum.Forms.DefaultVisuals.V3.PasswordBoxVisual(tryCreateFormsObject: false);

        passwordBox.CaretIndex.ShouldBe(4);
        passwordBox.SelectionStart.ShouldBe(1);
        passwordBox.SelectionLength.ShouldBe(3);
    }
}
