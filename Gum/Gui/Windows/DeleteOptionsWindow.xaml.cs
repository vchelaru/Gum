using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Gum.Gui.Windows
{
    /// <summary>
    /// Interaction logic for DeleteOptionsWindow.xaml
    /// </summary>
    public partial class DeleteOptionsWindow : System.Windows.Window
    {
        public StackPanel MainStackPanel
        {
            get { return StackPanelInstance; }
        }

        public string Message
        {
            get
            {
                return (string)LabelInstance.Content;
            }
            set
            {
                LabelInstance.Content = value;
            }
        }

        public object ObjectToDelete
        {
            get; set;
        }

        public DeleteOptionsWindow()
        {
            InitializeComponent();
        }

        private void YesButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void NoButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                this.DialogResult = true;
            }
            else if(e.Key == Key.Escape)
            {
                e.Handled = true;
                this.DialogResult = false;
            }
        }
    }
}
