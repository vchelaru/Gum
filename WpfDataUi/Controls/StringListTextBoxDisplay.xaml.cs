using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        static SolidColorBrush IndeterminateBackground = new SolidColorBrush(System.Windows.Media.Colors.LightGray);
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

                Dispatcher.BeginInvoke(() =>
                {
                    if (!DataUiGrid.GetOverridesIsDefaultStyling(this))
                    {
                        this.TextBox.Background = InstanceMember.IsDefault ? DefaultValueBackground
                            : InstanceMember.IsIndeterminate ? IndeterminateBackground
                            : CustomValueBackground;
                    }
                });

                //HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
                //HintTextBlock.Text = InstanceMember?.DetailText;
                TrySetValueOnUi(InstanceMember?.Value);
                //RefreshIsEnabled();

                SuppressSettingProperty = false;
            }
        }

        public string GetCurrentLineText()
        {
            // Get the current cursor position
            int currentPosition = TextBox.SelectionStart;

            // Get the line index of the current position
            int currentLineIndex = TextBox.GetLineIndexFromCharacterIndex(currentPosition);

            // Get the text of the current line
            string currentLineText = TextBox.GetLineText(currentLineIndex);

            return currentLineText;
        }


        public ApplyValueResult TryGetValueOnUi(out object result)
        {
            // todo - need to make this more flexible, but for now let's just support strings:
            if (InstanceMember?.PropertyType == typeof(List<string>))
            {
                var value = new List<string>();
                
                // newlines could be \r\n or just \n, so we need to split on both
                if (TextBox.Text.Contains("\r\n"))
                {
                    value = TextBox.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();
                }
                else // if(TextBox.Text.Contains("\n")) this also captures there being no newlines
                {
                    value = TextBox.Text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();
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
