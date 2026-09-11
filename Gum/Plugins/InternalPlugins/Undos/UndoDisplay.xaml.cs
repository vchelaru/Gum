using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace Gum.Plugins.Undos
{
    /// <summary>
    /// Interaction logic for UndoDisplay.xaml
    /// </summary>
    public partial class UndoDisplay : UserControl
    {
        public UndoDisplay()
        {
            InitializeComponent();
            DataContextChanged += HandleDataContextChanged;
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is UndosViewModel oldViewModel)
            {
                oldViewModel.FocusCurrentItemRequested -= FocusCurrentItem;
            }
            if (e.NewValue is UndosViewModel newViewModel)
            {
                newViewModel.FocusCurrentItemRequested += FocusCurrentItem;
            }
        }

        // The tab got focus: select the current history entry and scroll it into view.
        private void FocusCurrentItem()
        {
            if (DataContext is UndosViewModel viewModel && viewModel.CurrentItem is { } current)
            {
                ListBoxInstance.SelectedItem = current;
                ListBoxInstance.ScrollIntoView(current);
            }
        }

        private void ListBox_PreviewMouseDown(object? sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Check if the click is on a scrollbar
            var hitTestResult = VisualTreeHelper.HitTest(ListBoxInstance, e.GetPosition(ListBoxInstance));
            if (hitTestResult != null)
            {
                var clickedElement = hitTestResult.VisualHit;

                // Allow clicks on the scrollbar
                if (clickedElement is DependencyObject dependencyObject &&
                    IsScrollbarElement(dependencyObject))
                {
                    return; // Let the event propagate normally
                }
            }

            // Prevent selection changes for clicks outside the scrollbar
            e.Handled = true;
        }

        private bool IsScrollbarElement(DependencyObject element)
        {
            while (element != null)
            {
                if (element is ScrollBar)
                    return true;
                element = VisualTreeHelper.GetParent(element);
            }
            return false;
        }

        private void HandleSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            // This brings the selection into view:
            var listBox = sender as ListBox;
            if (listBox != null && listBox.SelectedItem != null)
            {
                listBox.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                    (Action)(() =>listBox.ScrollIntoView(listBox.SelectedItem)));
            }
        }
    }
}
