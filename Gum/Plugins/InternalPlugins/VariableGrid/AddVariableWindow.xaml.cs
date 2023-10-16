using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Gum.Plugins.VariableGrid
{
    /// <summary>
    /// Interaction logic for AddVariableWindow.xaml
    /// </summary>
    public partial class AddVariableWindow : Window
    {
        public string SelectedType
        {
            get
            {
                return (ListBox.SelectedItem as ListBoxItem)?.Content as string;
            }
            set
            {
                var newItem = ListBox.Items.FirstOrDefault(item => item is ListBoxItem && ((ListBoxItem)item).Content as string == value);
                ListBox.SelectedItem = newItem;
            }
        }

        public string EnteredName
        {
            get => TextBox.Text;
            set => TextBox.Text = value;
        }

        public AddVariableWindow()
        {
            InitializeComponent();

            this.Loaded += AddVariableWindow_Loaded;

            ListBox.SelectedIndex = 0;
        }

        private void AddVariableWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GumCommands.Self.GuiCommands.MoveToCursor(this);

            this.TextBox.Focus();
        }

        private void HandleOkClicked(object sender, RoutedEventArgs e)
        {
            if(this.SelectedType == null)
            {
                MessageBox.Show("You must select a type");
                return;
            }
            if(string.IsNullOrEmpty(this.EnteredName))
            {
                MessageBox.Show("You must enter a name");
                return;
            }
            this.DialogResult = true;
        }

        private void HandleCancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

    }
}
