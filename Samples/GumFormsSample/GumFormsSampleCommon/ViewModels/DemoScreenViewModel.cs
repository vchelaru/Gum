using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.ViewModels;
internal class DemoScreenViewModel : ViewModel
{

    public bool IsButtonEnabled
    {
        get => Get<bool>();
        set => Set(value);
    }
}
