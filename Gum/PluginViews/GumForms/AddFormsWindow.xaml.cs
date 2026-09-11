using System.Windows.Controls;
using Gum.Services.Dialogs;
using GumFormsPlugin.ViewModels;

namespace Gum.PluginViews.GumForms
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
