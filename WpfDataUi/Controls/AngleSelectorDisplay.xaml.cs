using System;
using System.ComponentModel;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{
    #region enums

    public enum AngleType
    {
        Degrees,
        Radians
    }

    #endregion

    /// <summary>
    /// Interaction logic for AngleSelectorDisplay.xaml
    /// </summary>
    public partial class AngleSelectorDisplay : UserControl, INotifyPropertyChanged, IDataUi
    {
        #region Fields
        InstanceMember? _instanceMember;

        TextBoxDisplayLogic mTextBoxLogic;

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

            var getValueResult = TryGetValueOnUi(out object valueOnUi);

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

                _instanceMember = value;

                if (_instanceMember != null && valueChanged)
                {
                    _instanceMember.PropertyChanged -= HandlePropertyChange;
                }
                _instanceMember = value;

                if (_instanceMember != null && valueChanged)
                {
                    _instanceMember.PropertyChanged += HandlePropertyChange;
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

            mTextBoxLogic = new TextBoxDisplayLogic(this, this.TextBox);

            this.RefreshContextMenu(TopRowGrid.ContextMenu);
            this.RefreshContextMenu(TextBox.ContextMenu);

            // do we have to refresh the context menu? We do in the TextBoxDisplay
        }

        #endregion


        private void UpdateUiToAngle()
        {
            TextBox.Text = mAngle?.ToString();

            if (Angle == null)
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
            float value;
            var text = this.TextBox.Text;
            if(string.IsNullOrEmpty(text))
            {
                Angle = null;
            }
            else if (float.TryParse(this.TextBox.Text, out value))
            {
                Angle = value;
            }
            else
            {
                // couldn't parse it, so let's try to math operation it?
                try
                {
                    Angle = TextBoxDisplayLogic.TryHandleMathOperation(text, InstanceMember.PropertyType) as float?;
                }
                catch
                {
                    // do nothing...
                }
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

            this.Label.Content = InstanceMember.DisplayName;

            this.RefreshContextMenu(TopRowGrid.ContextMenu);
            this.RefreshContextMenu(TextBox.ContextMenu);

            HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
            HintTextBlock.Text = InstanceMember?.DetailText;

            Dispatcher.BeginInvoke(() =>
            {
                if (!DataUiGrid.GetOverridesIsDefaultStyling(this))
                {
                    if (InstanceMember.IsDefault)
                    {
                        TextBox.Background = TextBoxDisplayLogic.DefaultValueBackground;
                    }
                    else if (InstanceMember.IsIndeterminate)
                    {
                        TextBox.Background = TextBoxDisplayLogic.IndeterminateValueBackground;
                    }
                    else
                    {
                        TextBox.Background = TextBoxDisplayLogic.CustomValueBackground;
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

        public ApplyValueResult TryGetValueOnUi(out object result)
        {
            if (TypeToPushToInstance == AngleType.Radians)
            {
                if(mAngle != null)
                {
                    result = (float)(System.Math.PI * (double)mAngle / 180.0f);
                }
                else
                {
                    result = null;
                }

            }
            else
            {
                if(mAngle != null)
                {
                    result = (float)mAngle;
                }
                else
                {
                    result = null;
                }

            }
            return ApplyValueResult.Success;
        }

        public ApplyValueResult TrySetValueOnUi(object value)
        {
            ApplyValueResult toReturn = ApplyValueResult.NotSupported;
            if (value is float asFloat)
            {
                var isOver = this.IsMouseOver && Mouse.LeftButton == MouseButtonState.Pressed;
                if(!isOver)
                {
                    if (TypeToPushToInstance == AngleType.Radians)
                    {
                        this.Angle = 180 * (float)(asFloat / Math.PI);

                    }
                    else
                    {
                        this.Angle = asFloat;
                    }


                }

                toReturn = ApplyValueResult.Success;
            }
            else if(value is int asInt)
            {
                var isOver = this.IsMouseOver && Mouse.LeftButton == MouseButtonState.Pressed;
                if (!isOver)
                {
                    if (TypeToPushToInstance == AngleType.Radians)
                    {
                        this.Angle = 180 * (float)(asInt / Math.PI);

                    }
                    else
                    {
                        this.Angle = asInt;
                    }


                }

                toReturn = ApplyValueResult.Success;
            }
            else if(value is null)
            {
                this.Angle = null;

                TextBox.Text = null;
                toReturn = ApplyValueResult.Success;
            }

            if(Angle == null)
            {
                PlaceholderText.Visibility = Visibility.Visible;
                PlaceholderText.Text = "<NULL>";
            }
            else
            {
                PlaceholderText.Visibility = Visibility.Collapsed;
            }

            return toReturn;
        }

        private void HandlePropertyChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
            {
                this.Refresh();

            }
        }

        public static decimal RoundDecimal(decimal valueToRound, decimal multipleOf)
        {
            return ((int)(System.Math.Sign(valueToRound) * .5m + valueToRound / multipleOf)) * multipleOf;
        }

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

        private void Ellipse_MouseMove_1(object? sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                var point = Mouse.GetPosition(CenterPoint);

                if (point.X != 0 || point.Y != 0)
                {
                    point.Y *= -1;

                    var angleToSet = Math.Atan2(point.Y, point.X);
                    angleToSet = 180 * (float)(angleToSet / Math.PI);
                    //int angleAsInt = (int)(angleToSet + .5f);

                    decimal newAngle = (decimal)angleToSet;

                    var effectiveSnappingInterval = SnappingInterval;

                    if(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        // this will snap to 15 pixels:
                        if(effectiveSnappingInterval == null || effectiveSnappingInterval < 15)
                        {
                            effectiveSnappingInterval = 15;
                        }
                    }

                    if(effectiveSnappingInterval != null)
                    {
                        newAngle = RoundDecimal((decimal)angleToSet, effectiveSnappingInterval.Value);
                    }

                    // We need snapping
                    if(mAngle != newAngle)
                    {
                        // don't set the float property, this causes a cast to float and loses precision, resulting
                        // in the text box displaying things like 1.00001 instead of 1
                        //Angle = angleAsInt;
                        mAngle = newAngle;
                        ReactToAngleSetThroughProperty(SetPropertyCommitType.Intermediate);
                        needsToPushFullCommitOnMouseUp = true;
                    }
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

        }

        private void Ellipse_MouseUp(object? sender, MouseButtonEventArgs e)
        {
            System.Windows.Input.Mouse.Capture(null);

            ReactToAngleSetThroughProperty(SetPropertyCommitType.Full);

        }
        #endregion

    }
}
