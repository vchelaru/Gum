using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.Forms;
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
}
