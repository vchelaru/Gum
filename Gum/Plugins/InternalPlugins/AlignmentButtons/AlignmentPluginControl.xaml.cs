using Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;
using Gum.Services;
using Gum.ToolStates;
using System.Windows.Controls;
using GumCommon;

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

            var selectedState = Locator.GetRequiredService<ISelectedState>();

            this.DataContext = new AlignmentViewModel(new CommonControlLogic(selectedState));

        }
    }
}
