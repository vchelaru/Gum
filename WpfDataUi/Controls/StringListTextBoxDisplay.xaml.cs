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
    /// Interaction logic for StringListTextBoxDisplay.xaml
    /// </summary>
    public partial class StringListTextBoxDisplay : UserControl, IDataUi
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


        public StringListTextBoxDisplay()
        {
            InitializeComponent();
        }

        public bool SuppressSettingProperty { get; set; }

        static SolidColorBrush DefaultValueBackground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 255, 180));
        static SolidColorBrush CustomValueBackground = System.Windows.Media.Brushes.White;

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            bool canRefresh = true;

            if (canRefresh)
            {
                SuppressSettingProperty = true;

                //mTextBoxLogic.RefreshDisplay();

                this.Label.Text = InstanceMember.DisplayName;
                this.RefreshContextMenu(TextBox.ContextMenu);
                //this.RefreshContextMenu(StackPanel.ContextMenu);

                this.TextBox.Background = InstanceMember.IsDefault ? DefaultValueBackground : CustomValueBackground;

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
            if (InstanceMember?.PropertyType == typeof(List<string>))
            {
                var value = new List<string>();

                value = TextBox.Text.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
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
            if (value is List<string> valueAsList)
            {
                var newList = new List<string>();
                newList.AddRange(valueAsList);
                TextBox.Text = String.Join(System.Environment.NewLine, valueAsList.ToArray());
            }
            else
            {
                // nothing?
                // todo - we may want to clone the list here too to prevent unintentional editing of the underlying list
                //ListBox.ItemsSource = value as IEnumerable;
            }
            return ApplyValueResult.Success;
        }

        private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value))
            {
                this.Refresh();

            }
        }


        public bool HasUserChangedAnything { get; set; }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (HasUserChangedAnything)
            {
                this.TrySetValueOnInstance();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            HasUserChangedAnything = true;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            
            HasUserChangedAnything = false;
        }
    }
}
