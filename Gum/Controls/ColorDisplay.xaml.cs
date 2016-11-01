using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Controls.DataUi
{
    /// <summary>
    /// Interaction logic for ColorDisplay.xaml
    /// </summary>
    public partial class ColorDisplay : UserControl, IDataUi
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
                bool valueChanged = mInstanceMember != value;

                mInstanceMember = value;

                if (mInstanceMember != null && valueChanged)
                {
                    mInstanceMember.PropertyChanged -= HandlePropertyChange;
                }
                mInstanceMember = value;

                if (mInstanceMember != null && valueChanged)
                {
                    mInstanceMember.PropertyChanged += HandlePropertyChange;
                }

                Refresh();
            }
        }


        public bool SuppressSettingProperty { get; set; }


        #endregion

        public ColorDisplay()
        {
            InitializeComponent();
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
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
            }
            this.Label.Content = InstanceMember.DisplayName;
            SuppressSettingProperty = false;
        }

        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            if (valueOnInstance is Microsoft.Xna.Framework.Color)
            {
                Microsoft.Xna.Framework.Color color =
                    (Microsoft.Xna.Framework.Color)valueOnInstance;

                var windowsColor = new Color();
                windowsColor.A = color.A;
                windowsColor.R = color.R;
                windowsColor.G = color.G;
                windowsColor.B = color.B;

                this.ColorPicker.SelectedColor = windowsColor;
                return ApplyValueResult.Success;
            }
            return ApplyValueResult.NotSupported;
        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            var result = ApplyValueResult.UnknownError;
            value = null;
            if (!this.HasEnoughInformationToWork() || mInstancePropertyType == null)
            {
                result = ApplyValueResult.NotEnoughInformation;
            }
            else
            {
                if (mInstancePropertyType == typeof(Microsoft.Xna.Framework.Color))
                {
                    Microsoft.Xna.Framework.Color colorToReturn = new Microsoft.Xna.Framework.Color(
                        ColorPicker.SelectedColor.Value.R,
                        ColorPicker.SelectedColor.Value.G,
                        ColorPicker.SelectedColor.Value.B,
                        ColorPicker.SelectedColor.Value.A);

                    result = ApplyValueResult.Success;

                    value = colorToReturn;
                }
                else
                {
                    result = ApplyValueResult.NotSupported;
                }

            }

            return result;
        }

        private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
            {
                this.Refresh();

            }
        }

        private void HandleColorChange(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            var settingResult = this.TrySetValueOnInstance();

            if (settingResult == ApplyValueResult.NotSupported)
            {
                this.IsEnabled = false;
            }
        }
    }
}
