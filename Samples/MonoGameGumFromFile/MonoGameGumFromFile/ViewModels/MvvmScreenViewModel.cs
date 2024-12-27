using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGumFromFile.ViewModels;
internal class MvvmScreenViewModel : ViewModel
{
    public float RectangleWidth
    {
        get => Get<float>();
        set => Set(value);
    }

    public float RectangleHeight
    {
        get => Get<float>();
        set => Set(value);
    }
}
