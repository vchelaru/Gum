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
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace WpfDataUiCore.Controls
{
    /// <summary>
    /// Interaction logic for PlusMinusTextBox.xaml
    /// </summary>
    public partial class PlusMinusTextBox : UserControl, IDataUi, ISetDefaultable
    {
        TextBoxDisplayLogic mTextBoxLogic;
        InstanceMember mInstanceMember;

        ApplyValueResult? lastApplyValueResult = null;


        public InstanceMember InstanceMember
        {
            get => mInstanceMember;
            set
            {
                mTextBoxLogic.InstanceMember = value;

                bool instanceMemberChanged = mInstanceMember != value;
                if (mInstanceMember != null && instanceMemberChanged)
                {
                    mInstanceMember.PropertyChanged -= HandlePropertyChange;
                }
                mInstanceMember = value;

                if (mInstanceMember != null && instanceMemberChanged)
                {
                    mInstanceMember.PropertyChanged += HandlePropertyChange;
                }


                //if (mInstanceMember != null)
                //{
                //    mInstanceMember.DebugInformation = "TextBoxDisplay " + mInstanceMember.Name;
                //}


                Refresh();
            }
        }

        public bool SuppressSettingProperty { get; set; }


        public PlusMinusTextBox()
        {
            InitializeComponent();

            mTextBoxLogic = new TextBoxDisplayLogic(this, TextBox);


            this.ContextMenu = TextBox.ContextMenu;

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
                this.RefreshContextMenu(TextBox.ContextMenu);
                //this.RefreshContextMenu(StackPanel.ContextMenu);

                //HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
                //HintTextBlock.Text = InstanceMember?.DetailText;

                RefreshIsEnabled();

                SuppressSettingProperty = false;
            }
        }

        public virtual ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            this.TextBox.Text = mTextBoxLogic.ConvertNumberToString(valueOnInstance);

            RefreshPlaceholderText();

            return ApplyValueResult.Success;
        }


        private void RefreshPlaceholderText()
        {
            //if (this.IsFocused || this.TextBox.IsFocused)
            //{
            //    PlaceholderText.Visibility = Visibility.Collapsed;
            //}
            //else
            //{
            //    TryGetValueOnUi(out object valueOnInstance);
            //    if (valueOnInstance == null)
            //    {
            //        PlaceholderText.Visibility = Visibility.Visible;
            //        PlaceholderText.Text = "<NULL>";
            //    }
            //    else
            //    {
            //        PlaceholderText.Visibility = Visibility.Collapsed;
            //    }

            //}
        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            return mTextBoxLogic.TryGetValueOnUi(out value);
        }

        private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value))
            {
                this.Refresh();

            }
        }

        private void TextBox_LostFocus_1(object sender, RoutedEventArgs e)
        {
            RefreshPlaceholderText();

            lastApplyValueResult = mTextBoxLogic.TryApplyToInstance();

            RefreshIsEnabled();
        }

        private void RefreshIsEnabled()
        {
            if (lastApplyValueResult == ApplyValueResult.NotSupported)
            {
                this.IsEnabled = false;
            }
            else if (mTextBoxLogic.InstanceMember?.IsReadOnly == true)
            {
                this.IsEnabled = false;
            }
            else
            {
                this.IsEnabled = true;
            }
        }

        public void SetToDefault()
        {
            // So we don't exlicitly set values when losing focus
            this.mTextBoxLogic.HasUserChangedAnything = false;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            RefreshPlaceholderText();
        }

        private void MinusButtonClicked(object sender, RoutedEventArgs e)
        {
            if( TryGetValueOnUi(out object value) == ApplyValueResult.Success)
            {
                if(value is int asInt)
                {
                    TrySetValueOnUi(asInt-1);
                    lastApplyValueResult = mTextBoxLogic.TryApplyToInstance();
                }
                else if(value is long asLong)
                {
                    TrySetValueOnUi(asLong-1);
                    lastApplyValueResult = mTextBoxLogic.TryApplyToInstance();
                }

            }

        }

        private void PlusButtonClicked(object sender, RoutedEventArgs e)
        {
            if (TryGetValueOnUi(out object value) == ApplyValueResult.Success)
            {
                if (value is int asInt)
                {
                    TrySetValueOnUi(asInt+1);
                    lastApplyValueResult = mTextBoxLogic.TryApplyToInstance();

                }
                else if (value is long asLong)
                {
                    TrySetValueOnUi(asLong+1);
                    lastApplyValueResult = mTextBoxLogic.TryApplyToInstance();

                }

            }
        }
    }
}
