using System.Windows.Controls;

namespace Gum.Plugins.Behaviors
{
    /// <summary>
    /// The Behaviors tab: the component's behaviors, and a checklist to edit them. Its buttons bind to
    /// <see cref="BehaviorsViewModel"/>'s commands.
    /// </summary>
    public partial class BehaviorsControl : UserControl
    {
        public BehaviorsControl()
        {
            InitializeComponent();
        }
    }
}
