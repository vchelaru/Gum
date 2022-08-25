using System;
using System.Collections;
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
    /// Interaction logic for ListBoxDisplay.xaml
    /// </summary>
    public partial class ListBoxDisplay : UserControl, IDataUi
    {
        InstanceMember mInstanceMember;
        public InstanceMember InstanceMember
        { 
            get => mInstanceMember; 
            set
            {
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
                Refresh();

            }
        }

        public ListBoxDisplay()
        {
            InitializeComponent();
        }

        public bool SuppressSettingProperty { get; set; }


        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            bool canRefresh = true;

            if(canRefresh)
            {
                SuppressSettingProperty = true;

                //mTextBoxLogic.RefreshDisplay();

                this.Label.Text = InstanceMember.DisplayName;
                this.RefreshContextMenu(ListBox.ContextMenu);
                //this.RefreshContextMenu(StackPanel.ContextMenu);

                //HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
                //HintTextBlock.Text = InstanceMember?.DetailText;
                TrySetValueOnUi(InstanceMember?.Value);
                //RefreshIsEnabled();

                SuppressSettingProperty = false;
            }
        }

        public ApplyValueResult TryGetValueOnUi(out object result)
        {
            // todo - need to make this more flexible, but for now let's just support strings:
            if(InstanceMember?.PropertyType == typeof(List<string>))
            {
                var value = new List<string>();

                foreach(var item in ListBox.Items)
                {
                    value.Add(item?.ToString());
                }

                result = value;

                return ApplyValueResult.Success;

            }
            else
            {
                result = null;
                return ApplyValueResult.NotSupported;
            }
        }

        public ApplyValueResult TrySetValueOnUi(object value)
        {
            ListBox.ItemsSource = value as IEnumerable;
            return ApplyValueResult.Success;
        }

        private void AddButtonClicked(object sender, RoutedEventArgs e)
        {
            NewEntryListBox.Visibility = Visibility.Visible;
            NewTextBox.Focus();
        }

        private void ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Delete)
            {
                var selectedItem = ListBox.SelectedIndex;

                if(selectedItem > -1)
                {
                    var listToRemoveFrom = ListBox.ItemsSource as IList;

                    listToRemoveFrom.RemoveAt(ListBox.SelectedIndex);
                }
                this.TrySetValueOnInstance();
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            HandleAddTextItem();
        }

        private void HandleAddTextItem()
        {
            var listToAddTo = ListBox.ItemsSource as IList;
            if(listToAddTo != null)
            {
                listToAddTo.Add(NewTextBox.Text);
            }
            NewTextBox.Text = null;
            NewEntryListBox.Visibility = Visibility.Collapsed;
            this.TrySetValueOnInstance();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            HandleCancelItem();
        }

        private void HandleCancelItem()
        {
            NewTextBox.Text = null;
            NewEntryListBox.Visibility = Visibility.Collapsed;
        }

        private void NewTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                e.Handled = true;
                HandleAddTextItem();

            }
            else if(e.Key == Key.Escape)
            {
                e.Handled = true;
                HandleCancelItem();
            }
        }

        private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value))
            {
                this.Refresh();

            }
        }
    }
}
