using System;
using System.ComponentModel;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ColorPicker.Models;
using WpfDataUi;
using WpfDataUi.DataTypes;
using static SkiaSharp.HarfBuzz.SKShaper;

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
        private bool needsToPushFullCommitOnMouseUp;

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
            // When dealing with multi-select, setting a value can result in this refreshing itself
            // This prevents that from happening.
            if (isSetting) return;

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

            RefreshIsEnabled();

            this.Label.Content = InstanceMember.DisplayName;

            this.RefreshContextMenu(MainGrid.ContextMenu);

            // todo - eventually we may want a HintTextBlock. If so, add it here:

            SuppressSettingProperty = false;
        }

        private void RefreshIsEnabled()
        {
            if (InstanceMember?.IsReadOnly == true)
            {
                IsEnabled = false;
            }
            else
            {
                IsEnabled = true;
            }
        }

        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
#if XNA
            if (valueOnInstance is Microsoft.Xna.Framework.Color color)
            {
                var windowsColor = new Color();
                windowsColor.A = color.A;
                windowsColor.R = color.R;
                windowsColor.G = color.G;
                windowsColor.B = color.B;

                this.ColorPicker.SelectedColor = windowsColor;
                // This is beign set from the underlying data object to the UI
                // which means the UI hasn't updated yet, so we don't want to push
                // the value back to the UI
                needsToPushFullCommitOnMouseUp = false;
                 
                return ApplyValueResult.Success;
            }
            else 
#endif
            if(valueOnInstance is System.Drawing.Color drawingColor)
            {
                var windowsColor = new Color();
                windowsColor.A = drawingColor.A;
                windowsColor.R = drawingColor.R;
                windowsColor.G = drawingColor.G;
                windowsColor.B = drawingColor.B;

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
#if XNA
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
#endif         
                if(mInstancePropertyType == typeof(System.Drawing.Color))
                {
                    var toReturn = System.Drawing.Color.FromArgb(
                        ColorPicker.SelectedColor.A,
                        ColorPicker.SelectedColor.R, 
                        ColorPicker.SelectedColor.G, 
                        ColorPicker.SelectedColor.B 
                        );

                    result = ApplyValueResult.Success;

                    value = toReturn;
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

        private void HandleColorChange(object sender, RoutedEventArgs e)
        {
            if (!(e is ColorRoutedEventArgs colorArgs)) return;


            var isColorSame = false;

#if XNA
            if (mInstancePropertyType == typeof(Microsoft.Xna.Framework.Color))
            {
                var colorPack = (uint)(colorArgs.Color.R | (colorArgs.Color.G << 8) | (colorArgs.Color.B << 16) | (colorArgs.Color.A << 24));
                var prevColor = (Microsoft.Xna.Framework.Color)InstanceMember.Value;
                isColorSame = colorPack == prevColor.PackedValue;
            }
            else 
#endif     
            if (mInstancePropertyType == typeof(System.Drawing.Color))
            {
                var prevColor = (System.Drawing.Color)InstanceMember.Value;
                var newColor = colorArgs.Color;

                isColorSame = newColor.A == prevColor.A &&
                    newColor.R == prevColor.R &&
                    newColor.G == prevColor.G &&
                newColor.B == prevColor.B;
            }
            if (isColorSame) return;


            var isMouseDown = Mouse.LeftButton == MouseButtonState.Pressed;
            var commitType = isMouseDown ? SetPropertyCommitType.Intermediate : SetPropertyCommitType.Full;

            if(commitType == SetPropertyCommitType.Intermediate)
            {
                needsToPushFullCommitOnMouseUp = true;
            }

            SetCurrentColorValueOnInstance(commitType);

        }

        bool isSetting = false;
        private void SetCurrentColorValueOnInstance(SetPropertyCommitType commitType)
        {
            isSetting = true;
            var getValueResult = TryGetValueOnUi(out object valueOnUi);

            if (getValueResult == ApplyValueResult.Success)
            {
                var settingResult = this.TrySetValueOnInstance(valueOnUi, commitType);

                if (settingResult == ApplyValueResult.NotSupported)
                {
                    this.IsEnabled = false;
                }
            }
            else
            {
                // do nothing?
            }
            isSetting = false;
        }

        private void ColorPicker_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // This is not raised for all UI as discussed here:
            //https://github.com/PixiEditor/ColorPicker/issues/52
            // However, PreviewMouseUp works well
            //if(needsToPushFullCommitOnMouseUp)
            //{
            //    needsToPushFullCommitOnMouseUp = false;
            //    SetCurrentColorValueOnInstance(SetPropertyCommitType.Full);
            //}
            //System.Diagnostics.Debug.WriteLine($"Mouse up at {DateTime.Now}");
        }

        private void ColorPicker_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (needsToPushFullCommitOnMouseUp)
            {
                needsToPushFullCommitOnMouseUp = false;
                SetCurrentColorValueOnInstance(SetPropertyCommitType.Full);
            }
            System.Diagnostics.Debug.WriteLine($"Preview Mouse up at {DateTime.Now}");

            e.Handled = false;
        }
    }
}
