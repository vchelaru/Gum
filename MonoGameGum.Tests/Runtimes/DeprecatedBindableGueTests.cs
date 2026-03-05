using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using Shouldly;
using Xunit;

#pragma warning disable CS0618 // BindableGue is obsolete

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Verifies that the deprecated BindableGue class continues to function correctly
/// via its inheritance from GraphicalUiElement during the deprecation period.
/// </summary>
public class DeprecatedBindableGueTests
{
    [Fact]
    public void BindableGue_BindingContext_ShouldUpdateBoundProperty()
    {
        // arrange
        BindableGue gue = new ContainerRuntime();
        TestViewModel testViewModel = new();
        gue.SetBinding(nameof(gue.X), nameof(testViewModel.IntPropertyOnVm));

        // act
        gue.BindingContext = testViewModel;
        testViewModel.IntPropertyOnVm = 99;

        // assert
        gue.X.ShouldBe(99);
    }

    [Fact]
    public void BindableGue_IsAssignableTo_GraphicalUiElement()
    {
        BindableGue gue = new ContainerRuntime();
        (gue is GraphicalUiElement).ShouldBeTrue();
    }

    [Fact]
    public void BindableGue_SetBinding_ShouldWork()
    {
        // arrange
        BindableGue gue = new ContainerRuntime();
        TestViewModel testViewModel = new();
        gue.BindingContext = testViewModel;

        // act
        gue.SetBinding(nameof(gue.X), nameof(testViewModel.IntPropertyOnVm));
        testViewModel.IntPropertyOnVm = 7;

        // assert
        gue.X.ShouldBe(7);
    }

    #region ViewModels

    class TestViewModel : ViewModel
    {
        public int IntPropertyOnVm
        {
            get => Get<int>();
            set => Set(value);
        }
    }

    #endregion
}

#pragma warning restore CS0618
