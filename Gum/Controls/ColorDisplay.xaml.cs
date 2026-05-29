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

        // Set in the constructor so the invalid-hex border can be cleared back to the control's themed default.
        private readonly Brush _defaultHexBorderBrush;
        private readonly Brush _invalidHexBorderBrush;
        // Guards the hex text box against re-validating / re-committing while we sync it programmatically.
        private bool _isSyncingHexText;

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

            _defaultHexBorderBrush = HexTextBox.BorderBrush;
            _invalidHexBorderBrush = Brushes.Red;
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
                SyncHexTextFromPicker();
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
                SyncHexTextFromPicker();
                return ApplyValueResult.Success;
            }
            return ApplyValueResult.NotSupported;
        }

        public ApplyValueResult TryGetValueOnUi(out object? value)
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

        private void HandlePropertyChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
            {
                this.Refresh();

            }
        }

        private void HandleColorChange(object? sender, RoutedEventArgs e)
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

            SyncHexTextFromPicker();
        }

        bool isSetting = false;
        private void SetCurrentColorValueOnInstance(SetPropertyCommitType commitType)
        {
            isSetting = true;
            var getValueResult = TryGetValueOnUi(out object? valueOnUi);

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

        private void ColorPicker_MouseUp(object? sender, MouseButtonEventArgs e)
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

        private void ColorPicker_PreviewMouseUp(object? sender, MouseButtonEventArgs e)
        {
            if (needsToPushFullCommitOnMouseUp)
            {
                needsToPushFullCommitOnMouseUp = false;
                SetCurrentColorValueOnInstance(SetPropertyCommitType.Full);
            }
            System.Diagnostics.Debug.WriteLine($"Preview Mouse up at {DateTime.Now}");

            e.Handled = false;
        }

        private void HexTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommitHexText();
                e.Handled = true;
            }
        }

        private void HexTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitHexText();
        }

        private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingHexText)
            {
                return;
            }
            UpdateHexValidationVisual();
        }

        /// <summary>
        /// Parses the hex text box and, if valid, applies it to the picker (preserving the current
        /// alpha) so the existing color-change path performs a single Full commit (one undo). The
        /// text box is then normalized to the canonical hex of the applied color; invalid input is
        /// reverted without committing.
        /// </summary>
        private void CommitHexText()
        {
            if (HexColorParser.TryParse(HexTextBox.Text, out byte r, out byte g, out byte b))
            {
                Color current = ColorPicker.SelectedColor;
                Color newColor = Color.FromArgb(current.A, r, g, b);
                if (newColor != current)
                {
                    // Raises ColorChanged -> HandleColorChange, which commits Full because the mouse
                    // is not pressed during a keyboard/focus commit -> a single undo entry.
                    ColorPicker.SelectedColor = newColor;
                }
            }

            // Normalize (on success) or revert (on invalid input) to the currently applied color.
            Color applied = ColorPicker.SelectedColor;
            SetHexText(HexColorParser.ToHexRgb(applied.R, applied.G, applied.B));
            UpdateHexValidationVisual();
        }

        /// <summary>
        /// Updates the hex text box to match the picker's current color, unless the user is actively
        /// editing it (so their in-progress typing is never clobbered).
        /// </summary>
        private void SyncHexTextFromPicker()
        {
            if (HexTextBox.IsKeyboardFocusWithin)
            {
                return;
            }

            Color color = ColorPicker.SelectedColor;
            SetHexText(HexColorParser.ToHexRgb(color.R, color.G, color.B));
            UpdateHexValidationVisual();
        }

        private void SetHexText(string text)
        {
            _isSyncingHexText = true;
            HexTextBox.Text = text;
            _isSyncingHexText = false;
        }

        private void UpdateHexValidationVisual()
        {
            // Empty input is treated as neutral (not an error) so clearing the box to retype isn't jarring.
            bool isNeutral = string.IsNullOrWhiteSpace(HexTextBox.Text)
                || HexColorParser.TryParse(HexTextBox.Text, out _, out _, out _);
            HexTextBox.BorderBrush = isNeutral ? _defaultHexBorderBrush : _invalidHexBorderBrush;
        }
    }
}
