using System;
using System.ComponentModel;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{
    /// <summary>
    /// Interaction logic for AngleSelectorDisplay.xaml
    /// </summary>
    public partial class AngleSelectorDisplay : UserControl, INotifyPropertyChanged, IDataUi
    {
        #region Fields
        InstanceMember? _instanceMember;

        TextBoxDisplayLogic mTextBoxLogic;

        readonly AngleSelectorLogic _angleLogic = new AngleSelectorLogic();

        decimal? mAngle;
        private bool needsToPushFullCommitOnMouseUp;
        #endregion

        #region Properties

        public float? Angle
        {
            get
            {
                if(mAngle == null)
                {
                    return null;
                }
                else
                {
                    return (float)mAngle.Value;
                }
            }
            set
            {
                if(value == null)
                {
                    mAngle = null;
                }
                else
                {
                    mAngle = (decimal)value;
                }

                ReactToAngleSetThroughProperty(SetPropertyCommitType.Full);

            }
        }

        private void ReactToAngleSetThroughProperty(SetPropertyCommitType commitType)
        {
            NotifyPropertyChange(nameof(Angle));
            NotifyPropertyChange(nameof(NegativeAngle));
            UpdateUiToAngle();

            var getValueResult = TryGetValueOnUi(out object? valueOnUi);

            if(getValueResult == ApplyValueResult.Success)
            {
                this.TrySetValueOnInstance(valueOnUi, commitType);
            }
        }

        public float? NegativeAngle
        {
            get
            {
                if(mAngle == null)
                {
                    return null;
                }
                else
                {
                    return (float)mAngle * -1;
                }
            }
            set
            {
                if(value == null)
                {
                    mAngle = null;
                }
                else
                {
                    mAngle = (decimal)value * -1;
                }
                ReactToAngleSetThroughProperty(SetPropertyCommitType.Full);

            }
        }

        public decimal? SnappingInterval { get; set; } = 1;

        public DataTypes.InstanceMember? InstanceMember
        {
            get
            {
                return _instanceMember;
            }
            set
            {
                mTextBoxLogic.InstanceMember = value;

                bool valueChanged = _instanceMember != value;
                var oldInstanceMember = _instanceMember;

                _instanceMember = value;

                if (oldInstanceMember != null && valueChanged)
                {
                    oldInstanceMember.PropertyChanged -= HandlePropertyChange;
                }

                if (_instanceMember != null && valueChanged)
                {
                    _instanceMember.PropertyChanged += HandlePropertyChange;
                }

                if (valueChanged)
                {
                    // Clear stale green background from a previous pooled use.
                    this.TextBox.ClearValue(TextBox.BackgroundProperty);
                }

                Refresh();
            }
        }

        public bool SuppressSettingProperty
        {
            get;
            set;
        }

        public AngleType TypeToPushToInstance
        {
            get;
            set;
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Constructor

        public AngleSelectorDisplay()
        {
            TypeToPushToInstance = AngleType.Radians;

            InitializeComponent();
            
            PlaceholderText.Text = "<NULL>";

            Line.DataContext = this;

            mTextBoxLogic = WpfDataUiTextBox.CreateLogic(this, this.TextBox);

            this.RefreshContextMenu(TopRowGrid.ContextMenu);
            this.RefreshContextMenu(TextBox.ContextMenu);

            // do we have to refresh the context menu? We do in the TextBoxDisplay
        }

        #endregion


        private void UpdateUiToAngle()
        {
            TextBox.Text = mAngle?.ToString();

            RefreshPlaceholderText();
        }

        private void RefreshPlaceholderText()
        {
            if (InstanceMember?.IsIndeterminate == true)
            {
                // Multiple selected instances disagree on this value. The text box is
                // intentionally blank, so showing "<NULL>" would misleadingly imply the
                // value is unset. The indeterminate background color is the visual cue.
                PlaceholderText.Visibility = Visibility.Collapsed;
            }
            else if (Angle == null)
            {
                PlaceholderText.Visibility = Visibility.Visible;
                PlaceholderText.Text = "<NULL>";
            }
            else
            {
                PlaceholderText.Visibility = Visibility.Collapsed;
            }
        }


        private void ApplyTextBoxText()
        {
            if (_angleLogic.TryParseAngleText(this.TextBox.Text, InstanceMember!.PropertyType, out float? parsed))
            {
                Angle = parsed;
            }
            // This also applies to instance, but it stores
            // the value in the text box logic so ESC works properly
            mTextBoxLogic.TryApplyToInstance();
        }


        void NotifyPropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            SuppressSettingProperty = true;

            //if (this.HasEnoughInformationToWork())
            //{
            //    Type type = this.GetPropertyType();

            //    mInstancePropertyType = type;
            //}

            object valueOnInstance;
            bool successfulGet = this.TryGetValueOnInstance(out valueOnInstance);
            if (successfulGet)
            {
                if (valueOnInstance != null)
                {
                    TrySetValueOnUi(valueOnInstance);
                }
            }

            this.Label.Content = InstanceMember?.DisplayName;

            this.RefreshContextMenu(TopRowGrid.ContextMenu);
            this.RefreshContextMenu(TextBox.ContextMenu);

            HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
            HintTextBlock.Text = InstanceMember?.DetailText;

            Dispatcher.BeginInvoke(() =>
            {
                if (!DataUiGrid.GetOverridesIsDefaultStyling(this))
                {
                    if (InstanceMember?.IsDefault == true)
                    {
                        TextBox.Background = DataUiBrushes.DefaultValueBackground;
                    }
                    else if (InstanceMember?.IsIndeterminate == true)
                    {
                        TextBox.Background = DataUiBrushes.IndeterminateValueBackground;
                    }
                    else
                    {
                        TextBox.Background = DataUiBrushes.CustomValueBackground;
                    }
                }
            });

            RefreshIsEnabled();

            mTextBoxLogic.RefreshBackgroundColor();


            SuppressSettingProperty = false;
        }

        private void RefreshIsEnabled()
        {
            if (InstanceMember?.IsReadOnly == true)
            {
                IsEnabled = false;
            }
            else
            {
                IsEnabled = true;
            }
        }

        public ApplyValueResult TryGetValueOnUi(out object? result)
        {
            result = _angleLogic.ToInstanceValue(mAngle, TypeToPushToInstance);
            return ApplyValueResult.Success;
        }

        public ApplyValueResult TrySetValueOnUi(object value)
        {
            ApplyValueResult toReturn = ApplyValueResult.NotSupported;
            if (value is float || value is int)
            {
                // Don't fight the user's drag with the value it is producing.
                bool isBeingDragged = value is float
                    ? Mouse.Captured == EllipseInstance
                    : this.IsMouseOver && Mouse.LeftButton == MouseButtonState.Pressed;
                if (!isBeingDragged && _angleLogic.TryGetDisplayedDegrees(value, TypeToPushToInstance, out float? degrees))
                {
                    this.Angle = degrees;
                }

                toReturn = ApplyValueResult.Success;
            }
            else if(value is null)
            {
                this.Angle = null;

                TextBox.Text = null;
                toReturn = ApplyValueResult.Success;
            }

            RefreshPlaceholderText();

            return toReturn;
        }

        private void HandlePropertyChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
            {
                this.Refresh();

            }
        }

        public static decimal RoundDecimal(decimal valueToRound, decimal multipleOf) =>
            AngleSelectorLogic.RoundDecimal(valueToRound, multipleOf);

        #region Event Handlers
        private void TextBox_PreviewKeyDown_1(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // don't handle it, let the text box display logic handle it too:
                //e.Handled = true;
                ApplyTextBoxText();
                //mTextAtStartOfEditing = TextBox.Text;

            }
        }

        private void TextBox_LostFocus_1(object? sender, RoutedEventArgs e)
        {
            if(mTextBoxLogic.HasUserChangedAnything)
            {
                ApplyTextBoxText();
            }
        }

        private void Grid_DragOver_1(object? sender, DragEventArgs e)
        {
            Ellipse_MouseMove_1(null, null);
        }

        private void Ellipse_MouseMove_1(object? sender, MouseEventArgs? e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                var point = Mouse.GetPosition(CenterPoint);
                bool isShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                if (_angleLogic.TryDragTo(point.X, point.Y, isShiftDown, SnappingInterval, out decimal newAngle) &&
                    mAngle != newAngle)
                {
                    // Set the decimal, not the float property, so the text box shows 1 rather than 1.00001.
                    mAngle = newAngle;
                    ReactToAngleSetThroughProperty(SetPropertyCommitType.Intermediate);
                    needsToPushFullCommitOnMouseUp = true;
                }
            }
        }

        private void Ellipse_MouseLeftButtonDown_1(object? sender, MouseButtonEventArgs e)
        {
            Ellipse_MouseMove_1(null, null);
        }

        private void TopRowGrid_PreviewMouseUp(object? sender, MouseButtonEventArgs e)
        {
            if(needsToPushFullCommitOnMouseUp)
            {
                needsToPushFullCommitOnMouseUp = false;
                ReactToAngleSetThroughProperty(SetPropertyCommitType.Full);
            }
        }

        private void Ellipse_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            System.Windows.Input.Mouse.Capture(EllipseInstance);

            _angleLogic.BeginDialDrag(mAngle);
        }

        private void Ellipse_MouseUp(object? sender, MouseButtonEventArgs e)
        {
            System.Windows.Input.Mouse.Capture(null);

            ReactToAngleSetThroughProperty(SetPropertyCommitType.Full);

        }
        #endregion

    }
}
