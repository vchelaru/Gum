using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.Forms;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class BindableGueTests
{
    [Fact]
    public async Task BindingContext_ReAssignments_ShouldClearOldSubscriptions()
    {
        TestViewModel testViewModel = new();
        ContainerRuntime parentContainer = new();
        parentContainer.BindingContext = testViewModel;
        parentContainer.SetBinding(nameof(parentContainer.X), nameof(testViewModel.IntPropertyOnVm));
        testViewModel.GetPropertyChangeCount().ShouldBe(1);
        for (int i = 0; i < 10; i++)
        {
            ContainerRuntime child = new();
            parentContainer.AddChild(child);
            child.SetBinding(nameof(child.X), nameof(testViewModel.IntPropertyOnVm));

            ContainerRuntime grandChild = new();
            grandChild.SetBinding(nameof(grandChild.X), nameof(testViewModel.IntPropertyOnVm));
            child.AddChild(grandChild);
        }
        testViewModel.GetPropertyChangeCount().ShouldBe(1 + 10 * 2);

        TestViewModel secondViewModel = new TestViewModel();
        parentContainer.BindingContext = secondViewModel;
        
        testViewModel.GetPropertyChangeCount().ShouldBe(0);
        secondViewModel.GetPropertyChangeCount().ShouldBe(1 + 10*2);

        parentContainer.BindingContext = null;

        secondViewModel.GetPropertyChangeCount().ShouldBe(0);
    }

    [Fact]
    public void BindingContext_ShouldNotSubscribe_IfNoBinding()
    {
        // arrange
        ContainerRuntime container = new();
        TestViewModel testViewModel = new();

        // act
        container.BindingContext = testViewModel;

        // assert
        testViewModel.GetPropertyChangeCount().ShouldBe(0);
    }

    [Fact]
    public void BindingContext_ShouldSubscribe_IfBindingExistsBefore()
    {
        // arrange
        ContainerRuntime container = new();
        TestViewModel testViewModel = new();

        container.SetBinding(nameof(container.X), nameof(testViewModel.IntPropertyOnVm));

        // act
        testViewModel.GetPropertyChangeCount().ShouldBe(0);
        container.BindingContext = testViewModel;

        // assert
        testViewModel.GetPropertyChangeCount().ShouldBe(1);
    }

    [Fact]
    public void BindingContext_ShouldSubscribe_IfBindingIsSetAfter()
    {
        // arrange
        ContainerRuntime container = new();
        TestViewModel testViewModel = new();
        container.BindingContext = testViewModel;
        // act
        testViewModel.GetPropertyChangeCount().ShouldBe(0);
        container.SetBinding(nameof(container.X), nameof(testViewModel.IntPropertyOnVm));
        // assert
        testViewModel.GetPropertyChangeCount().ShouldBe(1);
    }


    [Fact]
    public async Task PushToViewModel_ShouldPushToViewModel()
    {
        BindableGueDerived sut = new();
        TestViewModel testViewModel = new();
        sut.BindingContext = testViewModel;
        sut.SetBinding(nameof(sut.IntPropertyOnGue), nameof(testViewModel.IntPropertyOnVm));

        testViewModel.IntPropertyOnVm.ShouldBe(0);

        sut.IntPropertyOnGue = 5;
        testViewModel.IntPropertyOnVm.ShouldBe(5);
    }

    [Fact(Skip = "For Vic K - we need to fix this!")]
    public async Task SetBinding_NestedProperties_ShouldUpdate()
    {
        BindableGueDerived sut = new();
        ParentViewModel viewModel = new();
        sut.BindingContext = viewModel;
        sut.SetBinding(nameof(sut.IntPropertyOnGue), "TestViewModel.IntPropertyOnVm");

        viewModel.TestViewModel.IntPropertyOnVm = 1234;

        sut.IntPropertyOnGue.ShouldBe(1234);
    }

    [Fact]
    public async Task RemoveFromParent_ShouldUnsubscribeViewModelPropertyChange()
    {
        TestViewModel testViewModel = new();

        ContainerRuntime parentContainer = new();
        parentContainer.BindingContext = testViewModel;
        parentContainer.SetBinding(nameof(parentContainer.X), nameof(testViewModel.IntPropertyOnVm));
        testViewModel.GetPropertyChangeCount().ShouldBe(1);

        for(int i = 0; i < 10; i++)
        {
            ContainerRuntime child = new();
            parentContainer.AddChild(child);
            child.SetBinding(nameof(child.X), nameof(testViewModel.IntPropertyOnVm));
            ContainerRuntime grandChild = new();
            child.AddChild(grandChild);
            grandChild.SetBinding(nameof(child.X), nameof(testViewModel.IntPropertyOnVm));
            testViewModel.GetPropertyChangeCount().ShouldBe(3);

            child.Parent = null;
            testViewModel.GetPropertyChangeCount().ShouldBe(1);
        }
    }

    [Fact]
    public async Task Clear_ShouldUnsubscribeViewModelPropertyChange()
    {
        TestViewModel testViewModel = new();

        ContainerRuntime parentContainer = new();
        parentContainer.BindingContext = testViewModel;
        parentContainer.SetBinding(nameof(parentContainer.X), nameof(testViewModel.IntPropertyOnVm));

        testViewModel.GetPropertyChangeCount().ShouldBe(1);

        for (int i = 0; i < 10; i++)
        {
            ContainerRuntime child = new();
            parentContainer.AddChild(child);
            child.SetBinding(nameof(child.X), nameof(testViewModel.IntPropertyOnVm));

            ContainerRuntime grandChild = new();
            child.AddChild(grandChild);
            grandChild.SetBinding(nameof(grandChild.X), nameof(testViewModel.IntPropertyOnVm));
        }

        testViewModel.GetPropertyChangeCount().ShouldBe(1 + 10*2);

        parentContainer.Children!.Clear();

        testViewModel.GetPropertyChangeCount().ShouldBe(1);
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

    class ParentViewModel : ViewModel
    {
        public TestViewModel TestViewModel
        {
            get => Get<TestViewModel>();
            set => Set(value);
        }

        public ParentViewModel()
        {
            TestViewModel = new TestViewModel();
        }
    }

    class BindableGueDerived : BindableGue
    {
        int intProperty;
        public int IntPropertyOnGue
        {
            get => intProperty;
            set
            {
                intProperty = value;
                PushValueToViewModel();
            }
        }

    }

    #endregion
}
