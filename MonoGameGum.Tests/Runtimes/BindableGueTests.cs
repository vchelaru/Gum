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

    class TestViewModel : ViewModel
    {
        public int IntPropertyOnVm
        {
            get => Get<int>();
            set => Set(value);
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
