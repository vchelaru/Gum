using System;
using System.Windows;
using System.Windows.Controls;

namespace Gum
{
    /// <summary>
    /// Interaction logic for TestWpfControl.xaml
    /// </summary>
    public partial class MainPropertyGrid : UserControl
    {
        public event EventHandler AddVariableClicked;

        public event EventHandler SelectedBehaviorVariableChanged;

        public object Instance
        {
            get { return DataGrid.Instance; }
            set { DataGrid.Instance = value; }
        }


        public MainPropertyGrid()
        {
            InitializeComponent();
        }

        private void HandleAddVariableClicked(object? sender, RoutedEventArgs e)
        {
            AddVariableClicked?.Invoke(this, null);
        }

        private void ListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            SelectedBehaviorVariableChanged?.Invoke(this, null);
        }
    }
}
