using System.Windows.Controls;
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
        }
    }
}
