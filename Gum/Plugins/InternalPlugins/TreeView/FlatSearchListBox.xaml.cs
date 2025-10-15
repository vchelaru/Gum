﻿using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;

namespace Gum.Plugins.InternalPlugins.TreeView
{
    /// <summary>
    /// Interaction logic for FlatSearchListBox.xaml
    /// </summary>
    public partial class FlatSearchListBox : UserControl
    {

        public event Action<SearchItemViewModel> SelectSearchNode;

        public FlatSearchListBox()
        {
            InitializeComponent();
        }

        private void FlatList_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var objectPushed = e.OriginalSource;
            var frameworkElementPushed = (objectPushed as FrameworkElement);

            var searchNodePushed = frameworkElementPushed?.DataContext as SearchItemViewModel;
            SelectSearchNode(searchNodePushed);
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
}
