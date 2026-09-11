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
        InstanceMember? _instanceMember;
        public InstanceMember? InstanceMember
        {
            get => _instanceMember;
            set
            {
                bool instanceMemberChanged = _instanceMember != value;
                if (_instanceMember != null && instanceMemberChanged)
                {
                    _instanceMember.PropertyChanged -= HandlePropertyChange;
                }
                _instanceMember = value;
                if (_instanceMember != null && instanceMemberChanged)
                {
                    _instanceMember.PropertyChanged += HandlePropertyChange;
                }

                if (instanceMemberChanged)
                {
                    // Clear stale green background from a previous pooled use.
                    this.TextBox.ClearValue(TextBox.BackgroundProperty);
                }

                Refresh();

            }
        }


        readonly StringListLogic _listLogic = new StringListLogic();

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

                HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
                HintTextBlock.Text = InstanceMember?.DetailText;
                TrySetValueOnUi(InstanceMember?.Value);
                RefreshIsEnabled();

                SuppressSettingProperty = false;
            }
        }

        public string GetCurrentLineText() => _listLogic.GetLineAt(TextBox.Text, TextBox.SelectionStart);


        public ApplyValueResult TryGetValueOnUi(out object? result)
        {
            // Only string lists for now.
            if (InstanceMember?.PropertyType == typeof(List<string>))
            {
                result = _listLogic.ParseLines(TextBox.Text);
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
                TextBox.Text = _listLogic.JoinLines(valueAsList);
            }
            return ApplyValueResult.Success;
        }

        private void RefreshIsEnabled()
        {
            if (InstanceMember?.IsReadOnly == true)
            {
                this.IsEnabled = false;
            }
            else
            {
                this.IsEnabled = true;
            }
        }

        private void HandlePropertyChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value) ||
                e.PropertyName == nameof(InstanceMember.DetailText))
            {
                this.Refresh();
            }
        }


        public bool HasUserChangedAnything { get; set; }

        private void TextBox_LostFocus(object? sender, RoutedEventArgs e)
        {
            if (HasUserChangedAnything)
            {
                this.TrySetValueOnInstance();
            }
        }

        private void TextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            // Refresh() sets TextBox.Text programmatically inside a
            // SuppressSettingProperty window. WPF fires TextChanged synchronously
            // during that assignment, so without this guard a programmatic refresh
            // would flag HasUserChangedAnything = true and then on the next
            // LostFocus the inherited value would be written back as a local
            // override — observed when right-clicking "Make Default" on
            // VariableReferences.
            if (SuppressSettingProperty)
            {
                return;
            }
            HasUserChangedAnything = true;
        }

        private void TextBox_GotFocus(object? sender, RoutedEventArgs e)
        {
            
            HasUserChangedAnything = false;
        }
    }
}
