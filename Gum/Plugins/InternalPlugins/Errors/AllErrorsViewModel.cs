using System.Collections.ObjectModel;

namespace Gum.Plugins.Errors
{
    public class AllErrorsViewModel
    {
        public ObservableCollection<ErrorViewModel> Errors { get; set; } = new ObservableCollection<ErrorViewModel>();


    }
}
