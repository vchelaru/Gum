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
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{
    /// <summary>
    /// Interaction logic for CheckBoxDisplay.xaml
    /// </summary>
    public partial class CheckBoxDisplay : UserControl, IDataUi
    {
        #region Fields

        InstanceMember mInstanceMember;
        Type mInstancePropertyType;

        #endregion

        #region Properties

        public InstanceMember InstanceMember
        {
            get
            {
                return mInstanceMember;
            }
            set
            {
                mInstanceMember = value;
                Refresh();
            }
        }

        public Brush DesiredForegroundBrush
        {
            get
            {
                if (InstanceMember.IsDefault)
                {
                    return Brushes.Green;
                }
                else
                {
                    return Brushes.Black;

                }
            }
        }

        public bool SuppressSettingProperty { get; set; }
        

        #endregion

        public CheckBoxDisplay()
        {
            InitializeComponent();

            CheckBox.DataContext = this;

            this.RefreshContextMenu(CheckBox.ContextMenu);
        }

        public void Refresh()
        {
            SuppressSettingProperty = true;

            if (this.HasEnoughInformationToWork())
            {
                Type type = this.GetPropertyType();

                mInstancePropertyType = type;
            }

            object valueOnInstance;
            bool successfulGet = this.TryGetValueOnInstance(out valueOnInstance);
            if (successfulGet)
            {
                bool wasSet = false;
                if (valueOnInstance != null)
                {
                    wasSet = TrySetValueOnUi(valueOnInstance) == ApplyValueResult.Success;
                }
                if (!wasSet)
                {
                    this.CheckBox.IsChecked = false;
                }
            }
            this.CheckBox.Content = InstanceMember.DisplayName;
            this.RefreshContextMenu(CheckBox.ContextMenu);


            CheckBox.Foreground = DesiredForegroundBrush;

            SuppressSettingProperty = false;
        }

        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            if (valueOnInstance is bool)
            {
                this.CheckBox.IsChecked = (bool)valueOnInstance;
                return ApplyValueResult.Success;
            }
            return ApplyValueResult.NotSupported;
        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            value = CheckBox.IsChecked;

            return ApplyValueResult.Success;
        }

        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (!SuppressSettingProperty)
            {
                this.TrySetValueOnInstance();


                CheckBox.Foreground = DesiredForegroundBrush;

            }
        }

    }
}
