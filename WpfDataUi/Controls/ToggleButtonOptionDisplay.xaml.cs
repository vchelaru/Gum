using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// Interaction logic for ToggleButtonOptionDisplay.xaml
    /// </summary>
    public partial class ToggleButtonOptionDisplay : UserControl, IDataUi
    {
        #region Internal Classes

        public class Option
        {
            public string Name;
            public Object Value;

            public BitmapImage Image { get; set; }
            // todo: image
        }

        #endregion

        #region Fields/Properties

        List<ToggleButton> toggleButtons = new List<ToggleButton>();

        InstanceMember mInstanceMember;

        public InstanceMember InstanceMember
        {
            get
            {
                return mInstanceMember;
            }
            set
            {
                mInstanceMember = value;
                Refresh();
            }
        }

        public bool SuppressSettingProperty { get; set; }

        #endregion

        public ToggleButtonOptionDisplay(Option[] options)
        {
            InitializeComponent();
            this.Height = 60;
            foreach(var option in options)
            {
                var toggleButton = new ToggleButton();

                if(option.Image != null)
                {
                    //var stackPanel = new StackPanel();


                    //stackPanel.Children.Add(image);

                    //var label = new TextBlock();
                    //label.Text = "hi";
                    //stackPanel.Children.Add(label);
                    
                    //toggleButton.Content = image;
                    var image = new Image();

                    image.Source = option.Image;
                    toggleButton.Content = image;

                    toggleButton.Width = 35;
                    toggleButton.Height = 35;
                }
                else
                {
                    toggleButton.Content = option.Name;
                }
                toggleButton.Click += HandleToggleClick;
                toggleButton.Tag = option;
                toggleButtons.Add(toggleButton);
                ButtonStackPanel.Children.Add(toggleButton);

            }
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            SuppressSettingProperty = true;

            string propertyName = InstanceMember?.DisplayName;

            // todo: camel case it
            Label.Text =  propertyName;
            TrySetValueOnUi(InstanceMember.Value);
            //if (this.HasEnoughInformationToWork())
            //{
            //    Type type = this.GetPropertyType();

            //    mInstancePropertyType = type;
            //}

            //object valueOnInstance;
            //bool successfulGet = this.TryGetValueOnInstance(out valueOnInstance);
            //if (successfulGet)
            //{
            //    bool wasSet = false;
            //    if (valueOnInstance != null)
            //    {
            //        wasSet = TrySetValueOnUi(valueOnInstance) == ApplyValueResult.Success;
            //    }
            //    if (!wasSet)
            //    {
            //        this.CheckBox.IsChecked = false;
            //    }
            //}
            //this.CheckBox.Content = InstanceMember.DisplayName;
            //this.RefreshContextMenu(CheckBox.ContextMenu);


            //CheckBox.Foreground = DesiredForegroundBrush;

            SuppressSettingProperty = false;
        }

        public ApplyValueResult TryGetValueOnUi(out object result)
        {
            var checkedButton = toggleButtons.FirstOrDefault(item => item.IsChecked == true);

            if(checkedButton != null)
            {
                result = ((Option)checkedButton.Tag).Value;
                return ApplyValueResult.Success;
            }
            else
            {
                result = null;
                return ApplyValueResult.UnknownError;
            }
        }

        public ApplyValueResult TrySetValueOnUi(object value)
        {
            foreach(var button in toggleButtons)
            {
                var valueOnButton = ((Option)button.Tag).Value;
                // These are boxed:
                //button.IsChecked = valueOnButton == value;
                button.IsChecked = valueOnButton.Equals(value);
            }

            return ApplyValueResult.Success;
        }

        private void HandleToggleClick(object sender, RoutedEventArgs e)
        {
            var toggleButtonClicked = sender as ToggleButton;

            if(toggleButtonClicked.IsChecked == true)
            {
                foreach(var otherButton in toggleButtons.Where(item =>item != toggleButtonClicked))
                {
                    otherButton.IsChecked = false;
                }
            }
            else
            {
                // shut off, which we don't allow, so re-enable it
                toggleButtonClicked.IsChecked = true;
            }
            this.TrySetValueOnInstance();

        }
    }
}
