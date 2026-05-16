using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Gum.Services.Dialogs;
using GumFormsPlugin.ViewModels;

namespace GumFormsPlugin.Views
{
    /// <summary>
    /// Interaction logic for AddFormsWindow.xaml
    /// </summary>
    [Dialog(typeof(AddFormsViewModel))]
    public partial class AddFormsWindow : UserControl
    {
        public AddFormsWindow()
        {
            InitializeComponent();

            // The host DialogWindow locks SizeToContent to Manual once loaded,
            // so subsequent visibility changes on the requirements panel don't
            // grow the window. Listen for HasRequirements changing and re-fit
            // the window's size to its content for one frame each time.
            DataContextChanged += (_, e) =>
            {
                if (e.OldValue is INotifyPropertyChanged oldVm)
                {
                    oldVm.PropertyChanged -= OnViewModelPropertyChanged;
                }
                if (e.NewValue is INotifyPropertyChanged newVm)
                {
                    newVm.PropertyChanged += OnViewModelPropertyChanged;
                }
            };
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AddFormsViewModel.HasRequirements))
            {
                RefitHostWindow();
            }
        }

        private void RefitHostWindow()
        {
            // Defer to Loaded priority so the new visibility has been laid out
            // before we resize. Then snap back to Manual so the user can still
            // drag the window edges afterwards if they want.
            Dispatcher.BeginInvoke(() =>
            {
                if (Window.GetWindow(this) is { } window)
                {
                    window.SizeToContent = SizeToContent.WidthAndHeight;
                    Dispatcher.BeginInvoke(
                        () => window.SizeToContent = SizeToContent.Manual,
                        DispatcherPriority.Loaded);
                }
            }, DispatcherPriority.Loaded);
        }
    }
}
