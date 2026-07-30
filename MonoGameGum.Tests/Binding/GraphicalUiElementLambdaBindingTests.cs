using Gum.GueDeriving;
using Gum.Mvvm;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.LambdaBinding;

public class GraphicalUiElementLambdaBindingTests
{
    [Fact]
    public void SetBinding_LambdaExpression_DottedPath_ResolvesNestedValue()
    {
        // Arrange
        TextRuntime sut = new();
        ParentViewModel viewModel = new();
        viewModel.Child!.Name = "nested-value";

        // Act
        sut.SetBinding<ParentViewModel>(nameof(sut.Text), vm => vm.Child!.Name);
        sut.BindingContext = viewModel;

        // Assert
        sut.Text.ShouldBe("nested-value");
    }

    [Fact]
    public void SetBinding_LambdaExpression_FlatPath_ResolvesValue()
    {
        // Arrange
        TextRuntime sut = new();
        TestViewModel viewModel = new() { StringValue = "hello" };

        // Act
        sut.SetBinding<TestViewModel>(nameof(sut.Text), vm => vm.StringValue);
        sut.BindingContext = viewModel;

        // Assert
        sut.Text.ShouldBe("hello");
    }

    [Fact]
    public void SetBinding_LambdaExpression_ToStringFormat_IsApplied()
    {
        // Arrange
        TextRuntime sut = new();
        TestViewModel viewModel = new() { IntValue = 5 };

        // Act
        sut.SetBinding<TestViewModel>(nameof(sut.Text), vm => vm.IntValue, "N2");
        sut.BindingContext = viewModel;

        // Assert
        sut.Text.ShouldBe("5.00");
    }

    #region View models

    private class TestViewModel : ViewModel
    {
        public string StringValue
        {
            get => Get<string>();
            set => Set(value);
        }

        public int IntValue
        {
            get => Get<int>();
            set => Set(value);
        }
    }

    private class NamedChild : ViewModel
    {
        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    private class ParentViewModel : ViewModel
    {
        public NamedChild? Child
        {
            get => Get<NamedChild?>();
            set => Set(value);
        }

        public ParentViewModel()
        {
            Child = new NamedChild();
        }
    }

    #endregion
}
