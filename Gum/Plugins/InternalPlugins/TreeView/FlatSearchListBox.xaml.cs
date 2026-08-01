using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Gum.Plugins.InternalPlugins.TreeView
{
    /// <summary>
    /// Interaction logic for FlatSearchListBox.xaml
    /// </summary>
    public partial class FlatSearchListBox : UserControl
    {

        public event Action<SearchItemViewModel?>? SelectSearchNode;

        private Point _mouseDownPoint;
        private SearchItemViewModel? _dragCandidate;

        public FlatSearchListBox()
        {
            InitializeComponent();
        }

        private void FlatList_MouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            _dragCandidate = null;

            var objectPushed = e.OriginalSource;
            var frameworkElementPushed = (objectPushed as FrameworkElement);

            var searchNodePushed = frameworkElementPushed?.DataContext as SearchItemViewModel;
            SelectSearchNode?.Invoke(searchNodePushed);
            e.Handled = true;
        }

        private void FlatList_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            _mouseDownPoint = e.GetPosition(null);
            _dragCandidate = (e.OriginalSource as FrameworkElement)?.DataContext as SearchItemViewModel;
        }

        private void FlatList_PreviewMouseMove(object? sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _dragCandidate == null)
            {
                return;
            }

            Point current = e.GetPosition(null);
            if (Math.Abs(current.X - _mouseDownPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(current.Y - _mouseDownPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            SearchItemViewModel dragged = _dragCandidate;
            _dragCandidate = null;

            try
            {
                DragDrop.DoDragDrop(FlatList, CreateDragData(dragged), DragDropEffects.Copy);
            }
            catch
            {
                // Swallow so a failed/canceled OLE drag never destabilizes the host app.
            }
            finally
            {
                TreeDragPayload.Clear();
            }
        }

        // Publishes the dragged search result the same way the element tree publishes dragged nodes:
        // a marker format on the data object, with the item itself in TreeDragPayload. A search
        // result stands for an element/instance/behavior that may not be realized in the tree, so it
        // travels as a tag with no node behind it. The caller owns clearing the payload afterwards.
        internal static DataObject CreateDragData(SearchItemViewModel item)
        {
            TreeDragPayload.SetTags(new[] { item.BackingObject });

            DataObject data = new DataObject();
            data.SetData(TreeDragPayload.DataFormat, true);
            return data;
        }
    }

    public class ObjectToFluentIconConverter : IValueConverter
    {
        // Converts from source → target (e.g., ViewModel → View)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                ScreenSave => "Tv",
                ComponentSave => "Shapes",
                InstanceSave { Locked: true } => "LockClosed",
                InstanceSave => "Cube",
                BehaviorSave => "PuzzlePiece",
                StandardElementSave => "BoxToolbox",
                _ => "QuestionCircle"
            };
        }

        // Converts from target → source (e.g., View → ViewModel)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }
    }
    class TypeIsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value != null && parameter is Type t && t.IsInstanceOfType(value);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
