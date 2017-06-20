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
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.VariableGrid
{
    /// <summary>
    /// Interaction logic for VariableRemoveButton.xaml
    /// </summary>
    public partial class VariableRemoveButton : UserControl, IDataUi
    {
        public VariableRemoveButton()
        {
            InitializeComponent();
        }

        InstanceMember instanceMember;
        public InstanceMember InstanceMember
        {
            get { return instanceMember; }
            set
            {
                instanceMember = value;
                if(instanceMember != null)
                {
                    Refresh(true);
                }
            }
        }

        public bool SuppressSettingProperty
        {
            get;
            set;
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            //this.ButtonInstance.Content = InstanceMember.Name;
            TextBlockInstance.Text = InstanceMember.Name;
        }

        public ApplyValueResult TryGetValueOnUi(out object result)
        {
            result = null;
            return ApplyValueResult.Success;
        }

        public ApplyValueResult TrySetValueOnUi(object value)
        {
            return ApplyValueResult.Success;
        }

        private void ButtonInstance_Click(object sender, RoutedEventArgs e)
        {
            // When we click we'll set the value on the instance. The instance member will watch for that and remove itself from the category if so
            this.TrySetValueOnInstance();
        }
    }
}
