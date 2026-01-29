using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{
    /// <summary>
    /// Interaction logic for NullableBoolDisplay.xaml
    /// </summary>
    public partial class NullableBoolDisplay : UserControl, IDataUi, INotifyPropertyChanged
    {
        public string TrueText
        {
            get => TrueRadioButton.Content?.ToString() ?? string.Empty;
            set => TrueRadioButton.Content = value;
        }

        public string FalseText
        {
            get => FalseRadioButton.Content?.ToString() ?? string.Empty;
            set => FalseRadioButton.Content = value;
        }

        public string NullText
        {
            get => NullRadioButton.Content?.ToString() ?? string.Empty;
            set => NullRadioButton.Content = value;
        }

        InstanceMember? _instanceMember;
        public InstanceMember? InstanceMember
        {
            get
            {
                return _instanceMember;
            }
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
                Refresh();
            }
        }
        public bool SuppressSettingProperty { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void HandlePropertyChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value))
            {
                this.Refresh();

            }
        }

        public NullableBoolDisplay()
        {
            InitializeComponent();

            this.RefreshContextMenu(GroupBox.ContextMenu);
        }



        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            SuppressSettingProperty = true;

            bool successfulGet = this.TryGetValueOnInstance(out object valueOnInstance);

            if (successfulGet)
            {
                bool wasSet = false;
                wasSet = TrySetValueOnUi(valueOnInstance) == ApplyValueResult.Success;
            }

            HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
            HintTextBlock.Text = InstanceMember?.DetailText;

            GroupBox.Header = InstanceMember?.DisplayName ?? InstanceMember?.Name;

            SuppressSettingProperty = false;

        }

        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            var value = valueOnInstance as bool?;

            switch(value)
            {
                case true:
                    TrueRadioButton.IsChecked = true;
                    return ApplyValueResult.Success;
                case false:
                    FalseRadioButton.IsChecked = true;
                    return ApplyValueResult.Success;
                case null:
                    NullRadioButton.IsChecked = true;
                    return ApplyValueResult.Success;
                default:
                    return ApplyValueResult.NotSupported;
            }
        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            if(TrueRadioButton.IsChecked == true)
            {
                value = true;
            }
            else if(FalseRadioButton.IsChecked == true)
            {
                value = false;
            }
            else // if(NullRadioButton.IsChecked == true)
            {
                value = null;
            }
            return ApplyValueResult.Success;
        }

        private void TrueRadioButton_Checked(object? sender, RoutedEventArgs e)
        {
            if (!SuppressSettingProperty)
            {
                this.TrySetValueOnInstance();
            }
        }

        private void FalseRadioButton_Checked(object? sender, RoutedEventArgs e)
        {
            if (!SuppressSettingProperty)
            {
                this.TrySetValueOnInstance();
            }
        }

        private void NullRadioButton_Checked(object? sender, RoutedEventArgs e)
        {
            if (!SuppressSettingProperty)
            {
                this.TrySetValueOnInstance();
            }
        }

        public new bool Equals(object? obj)
        {
            return obj is NullableBoolDisplay display &&
                   base.Equals(obj) &&
                   EqualityComparer<InstanceMember?>.Default.Equals(_instanceMember, display._instanceMember);
        }
    }
}
