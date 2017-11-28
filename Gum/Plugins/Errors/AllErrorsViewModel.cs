using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.Errors
{
    public class AllErrorsViewModel
    {
        public ObservableCollection<ErrorViewModel> Errors { get; set; } = new ObservableCollection<ErrorViewModel>();


    }
}
