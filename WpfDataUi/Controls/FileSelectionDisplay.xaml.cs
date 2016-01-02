using Microsoft.Win32;
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
    /// Interaction logic for FileSelectionDisplay.xaml
    /// </summary>
    public partial class FileSelectionDisplay : UserControl, IDataUi
    {
        #region Fields

        TextBoxDisplayLogic mTextBoxLogic;

        InstanceMember mInstanceMember;

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


                //if (mInstanceMember != null)
                //{
                //    mInstanceMember.DebugInformation = "TextBoxDisplay " + mInstanceMember.Name;
                //}


                Refresh();
            }
        }


        public bool SuppressSettingProperty { get; set; }

        #endregion


        public FileSelectionDisplay()
        {
            InitializeComponent();


            mTextBoxLogic = new TextBoxDisplayLogic(this, TextBox);

            this.RefreshContextMenu(TextBox.ContextMenu);
        }


        public void Refresh()
        {

            SuppressSettingProperty = true;

            mTextBoxLogic.RefreshDisplay();


            this.Label.Content = InstanceMember.DisplayName;
            this.RefreshContextMenu(TextBox.ContextMenu);

            SuppressSettingProperty = false;
        }
        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            this.TextBox.Text = valueOnInstance.ToString();
            return ApplyValueResult.Success;
        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            return mTextBoxLogic.TryGetValueOnUi(out value);
        }

        private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
            {
                this.Refresh();

            }
        }


        private void TextBox_LostFocus_1(object sender, RoutedEventArgs e)
        {

            var result = mTextBoxLogic.TryApplyToInstance();

            if (result == ApplyValueResult.NotSupported)
            {
                this.IsEnabled = false;
            }
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            var shouldOpen = fileDialog.ShowDialog();

            if (shouldOpen.HasValue && shouldOpen.Value)
            {
                string file = fileDialog.FileName;
                this.TextBox.Text = file;
                mTextBoxLogic.TryApplyToInstance();
            }
        }

    }
}
