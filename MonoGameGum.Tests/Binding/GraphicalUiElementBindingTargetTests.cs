using System;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.BindingTarget;

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

        public event EventHandler? Raised;

        public void Raise() => Raised?.Invoke(this, EventArgs.Empty);
    }

    public class MethodTargetRuntime : ContainerRuntime
    {
        public int Calls { get; private set; }

        private void OnRaised(object? sender, EventArgs args) => Calls++;
    }

    [Fact]
    public void SetBinding_NonPublicMethodName_BindsVmEvent()
    {
        MethodTargetRuntime sut = new();
        ValueViewModel viewModel = new();
        sut.SetBinding("OnRaised", nameof(ValueViewModel.Raised));
        sut.BindingContext = viewModel;

        viewModel.Raise();

        sut.Calls.ShouldBe(1);
    }

    [Fact]
    public void SetBinding_StateCategoryName_ThrowsAtSetBinding()
    {
        // A category state is set through SetProperty, but a binding writes only CLR properties, so the
        // name never bound; it now fails at the call instead of when the context arrives.
        ContainerRuntime sut = new();
        StateSaveCategory category = new() { Name = "Size" };
        category.States.Add(new StateSave { Name = "Big" });
        sut.AddCategory(category);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => sut.SetBinding("SizeState", nameof(ValueViewModel.Value)));

        exception.Message.ShouldContain(nameof(ContainerRuntime));
        exception.Message.ShouldContain("SizeState");
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
