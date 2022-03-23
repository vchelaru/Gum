using System;
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
    /// Interaction logic for ComboBoxDisplay.xaml
    /// </summary>
    public partial class ComboBoxDisplay : UserControl, IDataUi, INotifyPropertyChanged
    {
        #region Fields


        InstanceMember mInstanceMember;


        Type mInstancePropertyType;

        static Brush mUnmodifiedBrush = null;

        #endregion

        #region Properties

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

        public Brush DesiredForegroundBrush
        {
            get
            {
                if (InstanceMember.IsDefault)
                {
                    return Brushes.Green;
                }
                else
                {
                    return Brushes.Black;

                }
            }
        }

        protected Grid Grid
        {
            get;
            private set;
        }

        private ComboBox ComboBox
        {
            get;
            set;
        }

        private TextBlock TextBlock
        {
            get;
            set;
        }

        public bool IsEditable
        {
            get => ComboBox.IsEditable;
            set => ComboBox.IsEditable = value;
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        public ComboBoxDisplay()
        {

            // So this used to use WPF, but it turns out that inheriting from 
            // this class and instantiating the derived class causes a crash, as 
            // discussed here:
            // http://stackoverflow.com/questions/7646331/the-component-does-not-have-a-resource-identified-by-the-uri
            // I tried rebuilding, deleting folders, restarting Visual Studio, no go. So I guess I'm going to C# it
            //InitializeComponent();
            CreateLayout();
            


            if (mUnmodifiedBrush == null)
            {
                mUnmodifiedBrush = ComboBox.Background;
            }

            this.ComboBox.DataContext = this;

            //this.ComboBox.IsEditable = true;

            this.RefreshContextMenu(ComboBox.ContextMenu);

            this.ComboBox.IsKeyboardFocusWithinChanged += HandleIsKeyboardFocusChanged;

        }

        private void HandleIsKeyboardFocusChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Sept 15, 2021
            // This is an interesting bug...
            // If the user right-clicks on a combo 
            // box to set it to default, the combo box
            // changes the property on an object which may
            // result in out put being printed (such as about
            // code generation). When this happens, the output
            // window scrolls to the bottom, which takes keyboard
            // focus from whatever was focused before, which is this
            // combo box. The combo box probably got focus from the right-click.
            // For now the fix is easy - just make it only do so if it's editable,
            // but this may require more fixes to prevent this bug from happening on 
            // editable combo boxes.
            //if(ComboBox.IsKeyboardFocusWithin == false)
            if(ComboBox.IsKeyboardFocusWithin == false && IsEditable)
            {
                HandleChange();
            }
        }

        private void CreateLayout()
        {
            Grid = new Grid();
            var firstColumnDefinition = new ColumnDefinition();
            firstColumnDefinition.SetBinding(ColumnDefinition.WidthProperty, "FirstGridLength");
            Grid.ColumnDefinitions.Add(firstColumnDefinition);
            Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock = new TextBlock
            {
                Name="Label",
                VerticalAlignment = VerticalAlignment.Center,
                Text = "Property Label:"
            };
            TextBlock.TextWrapping = TextWrapping.Wrap;
            TextBlock.ContextMenu = new ContextMenu();

            Grid.Children.Add(TextBlock);

            ComboBox = new ComboBox
            {
                Name="ComboBox",
                VerticalAlignment = VerticalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Height=22,
                MinWidth = 60
            };
            ComboBox.ContextMenu = new ContextMenu();
            //ComboBox.SetBinding(TextBlock.ForegroundProperty, "DesiredForegroundBrush");

            //TextBlock.SetForeground(ComboBox, asdf);
            //var textBlock = FindVisualChildByName<TextBlock>(ComboBox, "TextBlock");
            //textBlock.SetBinding(TextBlock.ForegroundProperty, "DesiredForegroundBrush");
            ComboBox.SelectionChanged += ComboBox_SelectionChanged;


            Grid.SetColumn(ComboBox, 1);
            Grid.Children.Add(ComboBox);

            this.Content = Grid;
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            if (this.HasEnoughInformationToWork())
            {
                Type type = this.GetPropertyType();

                mInstancePropertyType = type;

                PopulateItems();
            }

            object valueOnInstance;
            bool successfulGet = this.TryGetValueOnInstance(out valueOnInstance);
            if (successfulGet)
            {
                if (valueOnInstance != null)
                {
                    TrySetValueOnUi(valueOnInstance);
                }
                else
                {
                    this.ComboBox.Text = null;
                }
            }
            else
            {

            }


            TextBlock.SetForeground(ComboBox, DesiredForegroundBrush);

            this.RefreshContextMenu(ComboBox.ContextMenu);
            this.RefreshContextMenu(TextBlock.ContextMenu);
            
            this.TextBlock.Text = InstanceMember.DisplayName;

            RefreshIsEnabled();

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

        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            this.SuppressSettingProperty = true;
            this.ComboBox.SelectedItem = valueOnInstance;
            this.ComboBox.Text = valueOnInstance?.ToString();
            this.SuppressSettingProperty = false;

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("DesiredForegroundBrush"));
            }

            return ApplyValueResult.Success;
        }

        private void PopulateItems()
        {
            this.SuppressSettingProperty = true;
            this.ComboBox.Items.Clear();
            
            // We want to check the CustomOptions first
            // because we may have an enum that has been
            // reduced by the converter.  In that case we 
            // want to show the reduced set instead of the
            // entire enum
            if (InstanceMember.CustomOptions != null)
            {
                // Used to check for this:
                //  && InstanceMember.CustomOptions.Count != 0
                // But I see no reason - if there's no option, we just don't show any in the combo box
                foreach (var item in InstanceMember.CustomOptions)
                {
                    this.ComboBox.Items.Add(item);
                }

            }
            else if (mInstancePropertyType.IsEnum)
            {
                foreach (var item in Enum.GetValues(mInstancePropertyType))
                {
                    this.ComboBox.Items.Add(item);
                }

            }
            //else
            //{
            //    throw new NotImplementedException();
            //}
            this.SuppressSettingProperty = false;
        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            if(ComboBox.IsEditable)
            {
                value = ComboBox.Text;
            }
            else
            {
                value = this.ComboBox.SelectedItem;
            }

            return ApplyValueResult.Success;
        }

        bool isInSelectionChanged = false;
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // August 17, 2021 - If we check ComboBox.IsKeyboardFocusWithin,
            // then selecting a new item in the drop-down won't update the UI
            // immediately because the text is focused. Why? This seems annoying
            // for the user to have to tab out of the textbox...
            //var canBroadcast = ComboBox.IsEditable == false ||
            //    ComboBox.IsKeyboardFocusWithin == false;
            // Update - this code was here to prevent recursive calls to this because of 
            // the text box being changed. Therefore, we should have a value here to prevent recurisve calls
            if(!isInSelectionChanged)
            {
                isInSelectionChanged = true;

                var selectedItemString = this.ComboBox.SelectedItem?.ToString();
                var selectedItem = this.ComboBox.SelectedItem;
                // The text hasn't yet been set by default, so we need to force the text value here:
                ComboBox.Text = selectedItemString;

                // March 21, 2022
                // The ComboBoxDisplay
                // has a very weird bug
                // as reported here: https://github.com/vchelaru/FlatRedBall/issues/503
                // This bug happens when
                // a value is changed on the
                // combo box. That change is assigned
                // on the ComboBox.Text, which then calls
                // ComboBox_SelectionChanged again. For some
                // reason this recurisve call nulls out the display.
                // Vic has no idea why, but re-setting the SelectedItem
                // seems to fix it. So...HACK ALERT:
                // Update March 23, 2022
                ComboBox.SelectedItem = selectedItem;

                HandleChange();

                isInSelectionChanged = false;
            }

        }

        private void HandleChange()
        {
            this.TrySetValueOnInstance();

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("DesiredForegroundBrush"));
            }
            TextBlock.SetForeground(ComboBox, DesiredForegroundBrush);
        }
    }
}
