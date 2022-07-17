using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
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
    /// Interaction logic for NullableBoolDisplay.xaml
    /// </summary>
    public partial class NullableBoolDisplay : UserControl, IDataUi, INotifyPropertyChanged
    {
        public string TrueText
        {
            get => TrueRadioButton.Content?.ToString();
            set => TrueRadioButton.Content = value;
        }

        public string FalseText
        {
            get => FalseRadioButton.Content?.ToString();
            set => FalseRadioButton.Content = value;
        }

        public string NullText
        {
            get => NullRadioButton.Content?.ToString();
            set => NullRadioButton.Content = value;
        }

        InstanceMember mInstanceMember;
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

        public event PropertyChangedEventHandler PropertyChanged;



        public NullableBoolDisplay()
        {
            InitializeComponent();
        }



        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            SuppressSettingProperty = true;

            bool successfulGet = this.TryGetValueOnInstance(out object valueOnInstance);

            if (successfulGet)
            {
                bool wasSet = false;
                wasSet = TrySetValueOnUi(valueOnInstance) == ApplyValueResult.Success;

            }


            GroupBox.Header = InstanceMember?.DisplayName ?? InstanceMember?.Name;

            SuppressSettingProperty = true;

        }

        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            var value = valueOnInstance as bool?;

            switch(value)
            {
                case true:
                    TrueRadioButton.IsChecked = true;
                    return ApplyValueResult.Success;
                case false:
                    FalseRadioButton.IsChecked = true;
                    return ApplyValueResult.Success;
                case null:
                    NullRadioButton.IsChecked = true;
                    return ApplyValueResult.Success;
            }

            return ApplyValueResult.UnknownError;
        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            if(TrueRadioButton.IsChecked == true)
            {
                value = true;
            }
            else if(FalseRadioButton.IsChecked == true)
            {
                value = false;
            }
            else // if(NullRadioButton.IsChecked == true)
            {
                value = null;
            }
            return ApplyValueResult.Success;
        }
    }
}
