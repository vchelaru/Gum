using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Gum.Controls
{
    /// <summary>
    /// Interaction logic for MainPanelControl.xaml
    /// </summary>
    public partial class MainPanelControl : UserControl
    {
        public MainPanelControl()
        {
            InitializeComponent();
        }

        private void CenterBottomTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
