using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.Forms;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.Runtimes;
public class BindableGueTests
{
    [Before(Class)]
    public static void SetUp()
    {
        SystemManagers.Default = new();
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        FormsUtilities.InitializeDefaults();
    }

    [Test]
    public async Task PushToViewModel_ShouldPushToViewModel()
    {
        BindableGueDerived sut = new();
        TestViewModel testViewModel = new();
        sut.BindingContext = testViewModel;
        sut.SetBinding(nameof(sut.IntPropertyOnGue), nameof(testViewModel.IntPropertyOnVm));

        await Assert.That(testViewModel.IntPropertyOnVm).IsEqualTo(0);

        sut.IntPropertyOnGue = 5;
        await Assert.That(testViewModel.IntPropertyOnVm).IsEqualTo(5);
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
