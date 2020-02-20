using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOutputPlugin.ViewModels
{
    public class CodeWindowViewModel : ViewModel
    {
        public string Code
        {
            get => Get<string>();
            set => Set(value);
        }
    }
}
