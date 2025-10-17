using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{
    /// <summary>
    /// Interaction logic for SliderDisplay.xaml
    /// </summary>
    public partial class SliderDisplay : UserControl, IDataUi, ISetDefaultable
    {
        #region Fields/Properties

        double maxValue;
        public double MaxValue
        {
            get => this.maxValue;
            set
            {
                this.maxValue = value;
                RefreshMinAndMaxValues();
            }
        }

        
        double minValue;
        public double MinValue
        {
            get => minValue;
            set
            {
                this.minValue = value;
                RefreshMinAndMaxValues();
            }
        }

        /// <summary>
        /// The number of decimal points to show on the text box when dragging the slider.
        /// </summary>
        public int DecimalPointsFromSlider { get; set; } = 2;

        InstanceMember mInstanceMember;
        public InstanceMember InstanceMember
        {
            get
            {
                return mInstanceMember;
            }
            set
            {
                mTextBoxLogic.InstanceMember = value;

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

        TextBoxDisplayLogic mTextBoxLogic;

        public bool SuppressSettingProperty { get; set; }

        /// <summary>
        /// Can be used to multiply the underlying value to modify how it is displayed. By default this
        /// value is 1, which means that the displayed value will match the underlying value. A value of 2 would
        /// make the displayed value be double the underlying value. This value applies to the min and max values too.
        /// </summary>
        double displayedValueMultiplier = 1;
        public double DisplayedValueMultiplier
        {
            get => displayedValueMultiplier;
            set
            {
                displayedValueMultiplier = value;

                RefreshMinAndMaxValues();
            }
        }

        public bool IsShowingMinAndMax
        {
            get => MinValueText.Visibility == Visibility.Visible;
            set
            {
                if(value)
                {
                    MinValueText.Visibility = Visibility.Visible;
                    MaxValueText.Visibility = Visibility.Visible;
                }
                else
                {
                    MinValueText.Visibility = Visibility.Collapsed;
                    MaxValueText.Visibility = Visibility.Collapsed;
                }
            }
        }

        #endregion

        private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value))
            {
                this.Refresh();

            }
        }

        public SliderDisplay()
        {
            InitializeComponent();

            mTextBoxLogic = new TextBoxDisplayLogic(this, TextBox);
            mTextBoxLogic.MinValue = 0;
            mTextBoxLogic.MaxValue = (decimal)this.MaxValue;

            this.RefreshContextMenu(TextBox.ContextMenu);
            Slider.ContextMenu = Slider.ContextMenu ?? new ContextMenu();
            this.RefreshContextMenu(Slider.ContextMenu);
            Label.ContextMenu = Label.ContextMenu ?? new ContextMenu();
            this.RefreshContextMenu(Label.ContextMenu);
            this.ContextMenu = TextBox.ContextMenu;
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            // If the user is editing a value, we don't want to change
            // the value under the cursor
            // If we're default, then go ahead and change the value

            var isFocused = this.TextBox.IsFocused || this.Slider.IsFocused;

            bool canRefresh =
                isFocused == false || forceRefreshEvenIfFocused || mTextBoxLogic.InstanceMember.IsDefault;

            if (canRefresh)
            {

                SuppressSettingProperty = true;

                mTextBoxLogic.RefreshDisplay(out object _);

                this.Label.Text = InstanceMember.DisplayName;

                RefreshMinMaxAndTickDisplay();

                RefreshIsEnabled();

                SuppressSettingProperty = false;

                HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
                HintTextBlock.Text = InstanceMember?.DetailText;

                this.RefreshContextMenu(TextBox.ContextMenu);
                this.RefreshContextMenu(Slider.ContextMenu);
                this.RefreshContextMenu(Label.ContextMenu);
            }
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

        private void RefreshMinMaxAndTickDisplay()
        {
            MinValueText.Text = (this.MinValue * DisplayedValueMultiplier).ToString();
            MaxValueText.Text = (this.MaxValue * DisplayedValueMultiplier).ToString();

            // Ticks are only visible if the styles defined in these below files
            // Contain a Slider with at least 1 TickBar entry
            // ..\Gum\Gum\Themes\Frb.Styles.Defaults.xaml
            // ..\FlatRedBall\FRBDK\Glue\Glue\Themes\Frb.Styles.xaml
            this.Slider.TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight;
            this.Slider.TickFrequency = (this.MaxValue - this.MinValue) / 4.0;

            this.Slider.Ticks.Clear();

            var range = this.MaxValue - this.MinValue;
            var quarterRange = range / 4.0;
            this.Slider.Ticks.Add(MinValue);
            this.Slider.Ticks.Add(MinValue + quarterRange);
            this.Slider.Ticks.Add(MinValue + 2 * quarterRange);
            this.Slider.Ticks.Add(MinValue + 3 * quarterRange);
            this.Slider.Ticks.Add(MaxValue);
        }

        public void SetToDefault()
        {
            mTextBoxLogic.HasUserChangedAnything = false;
        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            var result = mTextBoxLogic.TryGetValueOnUi(out value);

            if (displayedValueMultiplier != 1 && value != null)
            {
                if (value is float asFloat)
                {
                    value = (float)(asFloat / displayedValueMultiplier);
                }
                else if (value is double asDouble)
                {
                    value = (double)(asDouble / displayedValueMultiplier);
                }
                else if (value is int asInt)
                {
                    value = (int)(asInt / displayedValueMultiplier);
                }
                else if (value is decimal asDecimal)
                {
                    value = (decimal)(asDecimal / (decimal)displayedValueMultiplier);
                }
                else if (value is long asLong)
                {
                    value = (long)(asLong / displayedValueMultiplier);
                }
                else if (value is byte asByte)
                {
                    value = (byte)(asByte / displayedValueMultiplier);
                }

            }

            return result;
        }

        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            if(valueOnInstance != null)
            {
                object multipliedValue = valueOnInstance;
                if(displayedValueMultiplier != 1)
                {
                    if (valueOnInstance is float asFloat)
                    {
                        multipliedValue = asFloat * displayedValueMultiplier;
                    }
                    else if (valueOnInstance is double asDouble)
                    {
                        multipliedValue = asDouble * displayedValueMultiplier;
                    }
                    else if (valueOnInstance is int asInt)
                    {
                        multipliedValue = asInt * displayedValueMultiplier;
                    }
                    else if (valueOnInstance is decimal asDecimal)
                    {
                        multipliedValue = asDecimal * (decimal)displayedValueMultiplier;
                    }
                    else if (valueOnInstance is long asLong)
                    {
                        multipliedValue = asLong * (long)displayedValueMultiplier;
                    }
                    else if(valueOnInstance is byte asByte)
                    {
                        multipliedValue = asByte * (byte)displayedValueMultiplier;
                    }
                }

                SetTextBoxValue(multipliedValue);

                SetSliderValue(multipliedValue);


                return ApplyValueResult.Success;
            }
            else
            {
                return ApplyValueResult.NotSupported;
            }
        }

        private void SetTextBoxValue(object valueOnInstance)
        {
            this.TextBox.Text = mTextBoxLogic.ConvertNumberToString(valueOnInstance, DecimalPointsFromSlider);
        }

        private void SetSliderValue(object valueOnInstance)
        {
            if (valueOnInstance is float)
            {
                this.Slider.Value = (float)valueOnInstance;
            }
            else if (valueOnInstance is double)
            {
                this.Slider.Value = (double)valueOnInstance;
            }
            else if(valueOnInstance is int)
            {
                this.Slider.Value = (int)valueOnInstance;
            }
            else if(valueOnInstance is decimal)
            {
                this.Slider.Value = (double)(decimal)valueOnInstance;
            }
            else if(valueOnInstance is long)
            {
                this.Slider.Value = (long)valueOnInstance;
            }
            else if(valueOnInstance is byte)
            {
                this.Slider.Value = (byte)valueOnInstance;
            }
        }

        DateTime lastSliderTime = new DateTime();
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // This is required to prevent weird flickering on the slider. Putting 100 ms frequency limiter makes everything work just fine.
            // It's a hack but...not sure what else to do. I also have Slider_DragCompleted so the last value is always pushed.
            // Update 2 - this causes all kinds of problems if we update in realtime. 
            // Update 3 - we should update in relatime unless the thumb is grabbed

            var timeSince = DateTime.Now - lastSliderTime;
            if (timeSince.TotalMilliseconds > 100)
            {
                //HandleValueChanged();
                //lastSliderTime = DateTime.Now;
                // display the value, but don't push it until the drag is complete:
                ApplySliderValueToTextBox();
            }
        }

        private void HandleValueChanged()
        {
            if (!SuppressSettingProperty)
            {
                ApplySliderValueToTextBox();

                // don't use this method, we want to control the decimals
                //SetTextBoxValue(value);

                mTextBoxLogic.TryApplyToInstance();
            }
        }

        private void ApplySliderValueToTextBox()
        {
            var value = Slider.Value;

            var propertyType = mInstanceMember?.PropertyType;
            if (propertyType == typeof(int) || 
                propertyType == typeof(uint) || 
                propertyType == typeof(long) || 
                propertyType == typeof(ulong) ||
                propertyType == typeof(byte) ||
                propertyType == typeof(short))
            {
                this.TextBox.Text = ((int)value).ToString();
            }
            else
            {

                this.TextBox.Text = value.ToString($"f{DecimalPointsFromSlider}");
            }
        }

        private void TextBox_LostFocus_1(object sender, RoutedEventArgs e)
        {

            mTextBoxLogic.ClampTextBoxValuesToMinMax();

            if(mTextBoxLogic.HasUserChangedAnything)
            {
                mTextBoxLogic.TryApplyToInstance();
            }
        }

        private void Slider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            HandleValueChanged();

            mTextBoxLogic.RefreshBackgroundColor();
        }

        private void RefreshMinAndMaxValues()
        {
            Slider.Maximum = this.maxValue * DisplayedValueMultiplier;
            mTextBoxLogic.MaxValue = (decimal)(this.maxValue * DisplayedValueMultiplier);
            Slider.Minimum = this.minValue * DisplayedValueMultiplier;
            mTextBoxLogic.MinValue = (decimal)(this.minValue * DisplayedValueMultiplier);

            RefreshMinMaxAndTickDisplay();
        }

        private void Slider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            HandleValueChanged();
        }
    }
}
