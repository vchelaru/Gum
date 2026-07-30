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

        public event Action<SearchItemViewModel> SelectSearchNode;

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
            SelectSearchNode(searchNodePushed);
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

            var data = new DataObject(CreateDragNode(dragged));
            // TreeNode.Tag does not survive the WPF -> WinForms drag boundary when its type isn't
            // [Serializable] (Gum's *Save types aren't) -- SearchResultDragPayload carries the real
            // object across in-process instead; MainEditorTabPlugin.OnWireframeDrop falls back to it
            // when Tag comes back null.
            SearchResultDragPayload.Current = dragged.BackingObject;
            try
            {
                DragDrop.DoDragDrop(FlatList, data, DragDropEffects.Copy);
            }
            catch
            {
                // Swallow, mirroring MultiSelectTreeView.Theming's own drag-start guard so a
                // failed/canceled OLE drag never destabilizes the host app.
            }
            finally
            {
                SearchResultDragPayload.Current = null;
            }
        }

        // Produces the same drag payload shape a real tree node does (a TreeNode tagged with the
        // underlying element/instance/behavior), so the Wireframe drop side
        // (MultiSelectTreeView.ExtractDraggedNodes, which reads TreeNode.Tag) handles a dragged
        // search result the same way it handles a dragged tree node.
        internal static System.Windows.Forms.TreeNode CreateDragNode(SearchItemViewModel item) =>
            new System.Windows.Forms.TreeNode { Tag = item.BackingObject };
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
