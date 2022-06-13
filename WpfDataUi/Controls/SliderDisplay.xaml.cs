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

                mTextBoxLogic.RefreshDisplay();

                this.Label.Text = InstanceMember.DisplayName;

                SuppressSettingProperty = false;
            }
        }

        public void SetToDefault()
        {

        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            var result = mTextBoxLogic.TryGetValueOnUi(out value);

            if(displayedValueMultiplier != 1 && value != null)
            {
                if (value is float asFloat)
                {
                    value = (float)(asFloat / displayedValueMultiplier);
                }
                else if (value is int asInt)
                {
                    value = (int)(asInt / displayedValueMultiplier);
                }
                else if (value is double asDouble)
                {
                    value = (double)(asDouble / displayedValueMultiplier);
                }
                else if (value is decimal asDecimal)
                {
                    value = (decimal)(asDecimal / (decimal)displayedValueMultiplier);
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
                    if(valueOnInstance is float asFloat)
                    {
                        multipliedValue = asFloat * displayedValueMultiplier;
                    }
                    else if(valueOnInstance is int asInt)
                    {
                        multipliedValue = asInt * displayedValueMultiplier;
                    }
                    else if(valueOnInstance is double asDouble)
                    {
                        multipliedValue = asDouble * displayedValueMultiplier;
                    }
                    else if(valueOnInstance is decimal asDecimal)
                    {
                        multipliedValue = asDecimal * (decimal)displayedValueMultiplier;
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
            // todo: support int...
        }

        DateTime lastSliderTime = new DateTime();
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // This is required to prevent weird flickering on the slider. Putting 100 ms frequency limiter makes everything work just fine.
            // It's a hack but...not sure what else to do. I also have Slider_DragCompleted so the last value is always pushed.
            // Update 2 - this causes all kinds of problems if we update in realtime. 
            var timeSince = DateTime.Now - lastSliderTime;
            if (timeSince.TotalMilliseconds > 100)
            {
                //HandleValueChanged();
                //lastSliderTime = DateTime.Now;
                // display the value, but don't push it until the drag is complete:
                var value = Slider.Value;
                this.TextBox.Text = value.ToString($"f{DecimalPointsFromSlider}");
            }
        }

        private void HandleValueChanged()
        {
            if (!SuppressSettingProperty)
            {

                var value = Slider.Value;
                this.TextBox.Text = value.ToString($"f{DecimalPointsFromSlider}");

                // don't use this method, we want to control the decimals
                //SetTextBoxValue(value);

                mTextBoxLogic.TryApplyToInstance();
            }
        }

        private void TextBox_LostFocus_1(object sender, RoutedEventArgs e)
        {
            mTextBoxLogic.ClampTextBoxValuesToMinMax();

            mTextBoxLogic.TryApplyToInstance();
        }

        private void Slider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            HandleValueChanged();
        }

        private void RefreshMinAndMaxValues()
        {
            Slider.Maximum = this.maxValue * DisplayedValueMultiplier;
            mTextBoxLogic.MaxValue = (decimal)(this.maxValue * DisplayedValueMultiplier);
            Slider.Minimum = this.minValue * DisplayedValueMultiplier;
            mTextBoxLogic.MinValue = (decimal)(this.minValue * DisplayedValueMultiplier);
        }

    }
}
