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
        InstanceMember mInstanceMember;

        TextBoxDisplayLogic mTextBoxLogic;

        decimal? mAngle;
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

                ReactToAngleSetThroughProperty();

            }
        }

        private void ReactToAngleSetThroughProperty()
        {
            NotifyPropertyChange(nameof(Angle));
            NotifyPropertyChange(nameof(NegativeAngle));

            UpdateUiToAngle();

            this.TrySetValueOnInstance();
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
                ReactToAngleSetThroughProperty();

            }
        }

        public decimal? SnappingInterval { get; set; } = 1;

        public DataTypes.InstanceMember InstanceMember
        {
            get
            {
                return mInstanceMember;
            }
            set
            {
                mTextBoxLogic.InstanceMember = value;

                bool valueChanged = mInstanceMember != value;

                mInstanceMember = value;

                if (mInstanceMember != null && valueChanged)
                {
                    mInstanceMember.PropertyChanged -= HandlePropertyChange;
                }
                mInstanceMember = value;

                if (mInstanceMember != null && valueChanged)
                {
                    mInstanceMember.PropertyChanged += HandlePropertyChange;
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

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Constructor

        public AngleSelectorDisplay()
        {
            TypeToPushToInstance = AngleType.Radians;

            InitializeComponent();
            
            PlaceholderText.Text = "<NULL>";

            Line.DataContext = this;

            mTextBoxLogic = new TextBoxDisplayLogic(this, this.TextBox);

            // do we have to refresh the context menu? We do in the TextBoxDisplay
        }

        #endregion


        #region Methods

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



        private void TextBox_PreviewKeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // don't handle it, let the text box display logic handle it too:
                //e.Handled = true;
                ApplyTextBoxText();
                //mTextAtStartOfEditing = TextBox.Text;

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

            // This also applies to instance, but it stores
            // the value in the text box logic so ESC works properly
            mTextBoxLogic.TryApplyToInstance();
        }

        private void TextBox_LostFocus_1(object sender, RoutedEventArgs e)
        {
            ApplyTextBoxText();
        }
        #endregion


        void NotifyPropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Grid_DragOver_1(object sender, DragEventArgs e)
        {
            Ellipse_MouseMove_1(null, null);
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
            SuppressSettingProperty = false;
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

        private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
            {
                this.Refresh();

            }
        }

        private void Ellipse_MouseMove_1(object sender, MouseEventArgs e)
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
                    if(SnappingInterval != null)
                    {
                        newAngle = RoundDecimal((decimal)angleToSet, SnappingInterval.Value);
                    }

                    // We need snapping
                    if(mAngle != newAngle)
                    {
                        // don't set the float property, this causes a cast to float and loses precision, resulting
                        // in the text box displaying things like 1.00001 instead of 1
                        //Angle = angleAsInt;
                        mAngle = newAngle;
                        ReactToAngleSetThroughProperty();

                    }
                }
            }
        }

        public static decimal RoundDecimal(decimal valueToRound, decimal multipleOf)
        {
            return ((int)(System.Math.Sign(valueToRound) * .5m + valueToRound / multipleOf)) * multipleOf;
        }
        private void Ellipse_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            Ellipse_MouseMove_1(null, null);
        }
    }
}
