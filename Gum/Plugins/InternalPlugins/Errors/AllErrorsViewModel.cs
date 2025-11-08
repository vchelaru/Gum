using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Security.RightsManagement;
using Gum.Mvvm;

namespace Gum.Plugins.Errors
{
    public class AllErrorsViewModel : ViewModel
    {
        public ObservableCollection<ErrorViewModel> Errors { get; } = [];

        public ErrorViewModel SelectedItem
        {
            get => Get<ErrorViewModel>();
            set => Set(value);
        }

        public string CountDescription => Errors.Count switch
        {
            0 => "0 Errors",
            1 => "1 Error",
            _ => $"{Errors.Count} Errors"
        };

        public AllErrorsViewModel()
        {
            Errors.CollectionChanged += ErrorsOnCollectionChanged;
        }

        private void ErrorsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(CountDescription));
        }
    }
}
