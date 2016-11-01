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
    public enum AngleType
    {
        Degrees,
        Radians
    }


    /// <summary>
    /// Interaction logic for AngleSelectorDisplay.xaml
    /// </summary>
    public partial class AngleSelectorDisplay : UserControl, INotifyPropertyChanged, IDataUi
    {
        #region Fields
        InstanceMember mInstanceMember;
        float mAngle;
        #endregion


        #region Properties

        public float Angle
        {
            get
            {
                return mAngle;
            }
            set
            {
                
                
                mAngle = value;
                NotifyPropertyChange("Angle");
                NotifyPropertyChange("NegativeAngle");
                
                UpdateUiToAngle();

                this.TrySetValueOnInstance();

            }
        }

        public float NegativeAngle
        {
            get
            {
                return mAngle * -1;
            }
            set
            {
                mAngle = value * -1;
                NotifyPropertyChange("Angle");
                NotifyPropertyChange("NegativeAngle");
                
                UpdateUiToAngle();
                this.TrySetValueOnInstance();
            }
        }


        public DataTypes.InstanceMember InstanceMember
        {
            get
            {
                return mInstanceMember;
            }
            set
            {
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

            Line.DataContext = this;
        }


        #endregion


        #region Methods

        private void UpdateUiToAngle()
        {
            TextBox.Text = mAngle.ToString();


        }



        private void TextBox_PreviewKeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                ApplyTextBoxText();
                //mTextAtStartOfEditing = TextBox.Text;

            }
        }

        private void ApplyTextBoxText()
        {
            float value;
            if (float.TryParse(this.TextBox.Text, out value))
            {
                Angle = value;
            }
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
                result = (float)(System.Math.PI * mAngle / 180.0f);

            }
            else
            {
                result = mAngle;

            }
            return ApplyValueResult.Success;
        }

        public ApplyValueResult TrySetValueOnUi(object value)
        {
            ApplyValueResult toReturn = ApplyValueResult.NotSupported;
            if (value is float)
            {
                if (TypeToPushToInstance == AngleType.Radians)
                {
                    this.Angle = 180 * (float)((float)value / Math.PI);

                }
                else
                {
                    this.Angle = (float)value;
                }
                toReturn = ApplyValueResult.Success;
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
                    int angleAsInt = (int)(angleToSet + .5f);

                    // We need snapping

                    Angle = angleAsInt;
                }
            }
        }

        private void Ellipse_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            Ellipse_MouseMove_1(null, null);
        }
    }
}
