using Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;
using System.Windows.Controls;

namespace Gum.Plugins.AlignmentButtons
{
    /// <summary>
    /// Interaction logic for AlignmentPluginControl.xaml
    /// </summary>
    public partial class AlignmentPluginControl : UserControl
    {
        public AlignmentPluginControl()
        {
            InitializeComponent();
            this.DataContext = new AlignmentViewModel(new CommonControlLogic());

        }
    }
}
