using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ColorPicker.Models;
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
                        ColorPicker.SelectedColor.R,
                        ColorPicker.SelectedColor.G,
                        ColorPicker.SelectedColor.B,
                        ColorPicker.SelectedColor.A);

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

        private void HandleColorChange(object sender, RoutedEventArgs e) {
            if (!(e is ColorRoutedEventArgs colorArgs)) return;

            var prevColor = (Microsoft.Xna.Framework.Color)InstanceMember.Value;

            var colorPack = (uint)(colorArgs.Color.R | (colorArgs.Color.G << 8) | (colorArgs.Color.B << 16) | (colorArgs.Color.A << 24));
            if (colorPack == prevColor.PackedValue) return;

            var settingResult = this.TrySetValueOnInstance();

            if (settingResult == ApplyValueResult.NotSupported) {
                this.IsEnabled = false;
            }
        }
    }
}
