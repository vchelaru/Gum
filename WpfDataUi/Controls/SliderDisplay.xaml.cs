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
        public double MaxValue
        {
            get
            {
                return Slider.Maximum;
            }
            set
            {
                Slider.Maximum = value;
                mTextBoxLogic.MaxValue = (decimal)this.MaxValue;
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

        private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value))
            {
                this.Refresh();

            }
        }
        public bool SuppressSettingProperty { get; set; }

        public SliderDisplay()
        {
            InitializeComponent();

            mTextBoxLogic = new TextBoxDisplayLogic(this, TextBox);
            mTextBoxLogic.MinValue = 0;
            mTextBoxLogic.MaxValue = (decimal)this.MaxValue;
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            // If the user is editing a value, we don't want to change
            // the value under the cursor
            // If we're default, then go ahead and change the value
            bool canRefresh =
                this.TextBox.IsFocused == false || forceRefreshEvenIfFocused || mTextBoxLogic.InstanceMember.IsDefault;

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
            return mTextBoxLogic.TryGetValueOnUi(out value);
        }

        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            SetTextBoxValue(valueOnInstance);

            SetSliderValue(valueOnInstance);


            // todo: set it on the slider too

            return ApplyValueResult.Success;
        }

        private void SetTextBoxValue(object valueOnInstance)
        {
            this.TextBox.Text = mTextBoxLogic.ConvertNumberToString(valueOnInstance);
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
            // todo: support int...
        }

        private void ApplyTextBoxText()
        {
            //todo: set the slider value==


            // This also applies to instance, but it stores
            // the value in the text box logic so ESC works properly
            mTextBoxLogic.TryApplyToInstance();
        }


        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(!SuppressSettingProperty)
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

    }
}
