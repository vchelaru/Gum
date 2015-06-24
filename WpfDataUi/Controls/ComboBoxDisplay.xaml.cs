using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for ComboBoxDisplay.xaml
    /// </summary>
    public partial class ComboBoxDisplay : UserControl, IDataUi, INotifyPropertyChanged
    {
        #region Fields


        InstanceMember mInstanceMember;


        Type mInstancePropertyType;

        static Brush mUnmodifiedBrush = null;

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

        public bool SuppressSettingProperty { get; set; }

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

        #endregion

        public ComboBoxDisplay()
        {
            InitializeComponent();


            if (mUnmodifiedBrush == null)
            {
                mUnmodifiedBrush = ComboBox.Background;
            }

            this.ComboBox.DataContext = this;

            this.RefreshContextMenu(ComboBox.ContextMenu);
        }





        public void Refresh()
        {
            if (this.HasEnoughInformationToWork())
            {
                Type type = this.GetPropertyType();

                mInstancePropertyType = type;

                PopulateItems();
            }

            object valueOnInstance;
            bool successfulGet = this.TryGetValueOnInstance(out valueOnInstance);
            if (successfulGet)
            {
                if (valueOnInstance != null)
                {
                    TrySetValueOnUi(valueOnInstance);
                }
                else
                {
                    this.ComboBox.Text = null;
                }
            }
            else
            {

            }

            this.RefreshContextMenu(ComboBox.ContextMenu);

            this.Label.Content = InstanceMember.DisplayName;
        }

        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            this.SuppressSettingProperty = true;
            this.ComboBox.SelectedItem = valueOnInstance;
            this.ComboBox.Text = valueOnInstance.ToString();
            this.SuppressSettingProperty = false;

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("DesiredForegroundBrush"));
            }

            return ApplyValueResult.Success;
        }

        private void PopulateItems()
        {
            this.SuppressSettingProperty = true;
            this.ComboBox.Items.Clear();
            
            // We want to check the CustomOptions first
            // because we may have an enum that has been
            // reduced by the converter.  In that case we 
            // want to show the reduced set instead of the
            // entire enum
            if (InstanceMember.CustomOptions != null && InstanceMember.CustomOptions.Count != 0)
            {

                foreach (var item in InstanceMember.CustomOptions)
                {
                    this.ComboBox.Items.Add(item);
                }

            }
            else if (mInstancePropertyType.IsEnum)
            {
                foreach (var item in Enum.GetValues(mInstancePropertyType))
                {
                    this.ComboBox.Items.Add(item);
                }

            }
            else
            {
                throw new NotImplementedException();
            }
            this.SuppressSettingProperty = false;
        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            value = this.ComboBox.SelectedItem;

            return ApplyValueResult.Success;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            this.TrySetValueOnInstance();

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("DesiredForegroundBrush"));
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
    }
}
