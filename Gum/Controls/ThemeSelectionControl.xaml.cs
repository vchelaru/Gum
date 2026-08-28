using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using GumFormsPlugin.ViewModels;

namespace Gum.Controls
{
    /// <summary>
    /// Theme picker (combo box + project-change requirements panel), shared between the Add Forms
    /// dialog and the New Project dialog. Bind this control's own DataContext to a
    /// <see cref="ThemeSelectionViewModel"/> (e.g. <c>DataContext="{Binding ThemeSelection}"</c>);
    /// its own bindings are relative to that.
    /// </summary>
    public partial class ThemeSelectionControl : UserControl
    {
        public ThemeSelectionControl()
        {
            InitializeComponent();

            // The host DialogWindow locks SizeToContent to Manual once loaded, so subsequent
            // visibility changes on the requirements panel don't grow the window. Listen for
            // HasRequirements changing and re-fit the window's size to its content for one frame
            // each time.
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
            if (e.PropertyName == nameof(ThemeSelectionViewModel.HasRequirements))
            {
                RefitHostWindow();
            }
        }

        private void RefitHostWindow()
        {
            // Defer to Loaded priority so the new visibility has been laid out before we resize.
            // Then snap back to Manual so the user can still drag the window edges afterwards if
            // they want.
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
