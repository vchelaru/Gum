using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{
    /// <summary>
    /// Interaction logic for CheckBoxDisplay.xaml
    /// </summary>
    public partial class CheckBoxDisplay : UserControl, IDataUi
    {
        #region Fields

        InstanceMember? _instanceMember;

        #endregion

        #region Properties

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

                if (instanceMemberChanged)
                {
                    this.RefreshAllContextMenus(force: true);
                    // Clear stale green foreground from a previous pooled use.
                    CheckBox.ClearValue(Control.ForegroundProperty);
                }

                Refresh();
            }
        }

        private void RefreshAllContextMenus(bool force = false)
        {
            if (force)
            {
                this.ForceRefreshContextMenu(CheckBox.ContextMenu);
            }
            else
            {
                this.RefreshContextMenu(CheckBox.ContextMenu);
            }
        }

        public Brush DesiredForegroundBrush
        {
            get
            {
                if(InstanceMember == null)
                {
                    return Brushes.Black;
                }
                else if (InstanceMember.IsDefault)
                {
                    return Brushes.Green;
                }
                else if(InstanceMember.IsIndeterminate)
                {
                    return Brushes.LightGray;
                }
                else
                {
                    return Brushes.Black;

                }
            }
        }

        public bool SuppressSettingProperty { get; set; }
        

        #endregion

        public CheckBoxDisplay()
        {
            InitializeComponent();

            CheckBox.DataContext = this;

            this.RefreshContextMenu(CheckBox.ContextMenu);
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            SuppressSettingProperty = true;

            object valueOnInstance;
            bool successfulGet = this.TryGetValueOnInstance(out valueOnInstance);
            if (successfulGet)
            {
                bool wasSet = false;
                if (valueOnInstance != null)
                {
                    wasSet = TrySetValueOnUi(valueOnInstance) == ApplyValueResult.Success;
                }
                if (!wasSet)
                {
                    this.CheckBox.IsChecked = false;
                }
            }
            this.CheckBox.Content = InstanceMember?.DisplayName ?? string.Empty;
            this.RefreshContextMenu(CheckBox.ContextMenu);


            HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
            HintTextBlock.Text = InstanceMember?.DetailText;

            Dispatcher.BeginInvoke(() =>
            {
                if (!DataUiGrid.GetOverridesIsDefaultStyling(this))
                {

                    SetForeground(DesiredForegroundBrush);
                }
            });

            RefreshIsEnabled();


            SuppressSettingProperty = false;
        }

        private void RefreshIsEnabled()
        {
            //if (lastApplyValueResult == ApplyValueResult.NotSupported)
            //{
            //    this.IsEnabled = false;
            //}
            //else 
            if (InstanceMember?.IsReadOnly == true)
            {
                this.IsEnabled = false;
            }
            else
            {
                this.IsEnabled = true;
            }
        }

        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            if (valueOnInstance is bool)
            {
                this.CheckBox.IsChecked = (bool)valueOnInstance;
                return ApplyValueResult.Success;
            }
            return ApplyValueResult.NotSupported;
        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            value = CheckBox.IsChecked;

            return ApplyValueResult.Success;
        }

        private void HandlePropertyChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value))
            {
                this.Refresh();
            }

            else if (e.PropertyName == nameof(InstanceMember.SupportsMakeDefault))
            {
                this.RefreshContextMenu(CheckBox.ContextMenu);
            }
        }

        private void CheckBoxChanged(object? sender, RoutedEventArgs e)
        {
            if (!SuppressSettingProperty)
            {
                this.TrySetValueOnInstance();

                Dispatcher.BeginInvoke(() =>
                {
                    if (!DataUiGrid.GetOverridesIsDefaultStyling(this))
                    {
                        SetForeground(DesiredForegroundBrush);
                    }
                });
            }
        }

        private void SetForeground(Brush brush)
        {
            if (brush == Brushes.Black)
            {
                CheckBox.ClearValue(Control.ForegroundProperty);
            }
            else
            {
                CheckBox.Foreground = brush;
            }
        }

    }
}
