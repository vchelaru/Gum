using System;
using Gum.GueDeriving;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Binding;

/// <summary>
/// A visual's SetBinding fails at the SetBinding call when the UI name is neither a property nor a method of
/// the visual, as the Forms SetBinding already does, instead of when a BindingContext arrives.
/// </summary>
public class GraphicalUiElementBindingTargetTests : BaseTestClass
{
    public class ValueViewModel : ViewModel
    {
        public string? Value
        {
            get => Get<string?>();
            set => Set(value);
        }
    }

    [Fact]
    public void SetBinding_NameThatIsNotAMember_ThrowsAtSetBinding()
    {
        TextRuntime text = new();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => text.SetBinding("DoesNotExist", nameof(ValueViewModel.Value)));

        exception.Message.ShouldContain("DoesNotExist");
    }
}
