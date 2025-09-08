using GumFormsPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Gum.Services.Dialogs;

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
