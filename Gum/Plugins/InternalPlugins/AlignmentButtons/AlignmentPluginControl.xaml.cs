using Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;
using Gum.Services;
using System.Windows.Controls;

namespace Gum.Plugins.AlignmentButtons
{
    /// <summary>
    /// Interaction logic for AlignmentPluginControl.xaml
    /// </summary>
    public partial class AlignmentPluginControl : UserControl
    {
        public AlignmentViewModel ViewModel { get; }

        public AlignmentPluginControl()
        {
            InitializeComponent();

            ViewModel = Locator.GetRequiredService<AlignmentViewModel>();
            this.DataContext = ViewModel;
        }
    }
}
