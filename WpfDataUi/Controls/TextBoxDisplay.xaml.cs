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
    /// Interaction logic for TextBoxDisplay.xaml
    /// </summary>
    public partial class TextBoxDisplay : UserControl, IDataUi, ISetDefaultable
    {
        #region Fields

        TextBoxDisplayLogic mTextBoxLogic;

        InstanceMember mInstanceMember;

        ApplyValueResult? lastApplyValueResult = null;

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

        #endregion

        public TextBoxDisplay()
        {
            InitializeComponent();

            mTextBoxLogic = new TextBoxDisplayLogic(this, TextBox);

            this.RefreshContextMenu(TextBox.ContextMenu);
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

                RefreshIsEnabled();

                SuppressSettingProperty = false;
            }
        }

        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            this.TextBox.Text = mTextBoxLogic.ConvertNumberToString(valueOnInstance);

            return ApplyValueResult.Success;
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

        public void MakeMultiline()
        {
            this.Label.VerticalAlignment = VerticalAlignment.Top;

            this.TextBox.TextWrapping = TextWrapping.Wrap;
            this.TextBox.AcceptsReturn = true;
            this.TextBox.VerticalContentAlignment = VerticalAlignment.Top;
            this.TextBox.VerticalAlignment = VerticalAlignment.Top;
            this.TextBox.Height = 65;
            this.mTextBoxLogic.HandlesEnter = false;
        }

        private void TextBox_LostFocus_1(object sender, RoutedEventArgs e)
        {

            lastApplyValueResult = mTextBoxLogic.TryApplyToInstance();

            RefreshIsEnabled();
        }

        private void RefreshIsEnabled()
        {
            if (lastApplyValueResult == ApplyValueResult.NotSupported)
            {
                this.IsEnabled = false;
            }
            else if(mTextBoxLogic.InstanceMember?.IsReadOnly == true)
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
    }
}
