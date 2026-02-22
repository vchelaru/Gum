using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{

    /// <summary>
    /// Interaction logic for ToggleButtonOptionDisplay.xaml
    /// </summary>
    public partial class ToggleButtonOptionDisplay : UserControl, IDataUi
    {
        public const string ContentTemplateKey = "ToggleButtonOptionDisplayOptionContentTemplate";
        public const string GumIconTemplateKey = "ToggleButtonOptionDisplayOptionContentTemplateGumIcon";

        #region Internal Classes

        public class Option
        {
            public string Name;
            public Object Value;

            public BitmapImage Image { get; set; }
            // todo: image
            public string? IconName { get; set; }
            public string? GumIconName { get; set; }
        }

        // DependencyProperty for WrapPanelRadius
        public static readonly DependencyProperty WrapPanelRadiusProperty =
            DependencyProperty.Register(
                nameof(WrapPanelRadius),
                typeof(CornerRadius),
                typeof(ToggleButtonOptionDisplay),
                new PropertyMetadata(new CornerRadius(0)));

        public CornerRadius WrapPanelRadius
        {
            get => (CornerRadius)GetValue(WrapPanelRadiusProperty);
            set => SetValue(WrapPanelRadiusProperty, value);
        }

        // DependencyProperty for WrapPanelBorderThickness
        public static readonly DependencyProperty WrapPanelBorderThicknessProperty =
            DependencyProperty.Register(
                nameof(WrapPanelBorderThickness),
                typeof(Thickness),
                typeof(ToggleButtonOptionDisplay),
                new PropertyMetadata(new Thickness(0)));

        public Thickness WrapPanelBorderThickness
        {
            get => (Thickness)GetValue(WrapPanelBorderThicknessProperty);
            set => SetValue(WrapPanelBorderThicknessProperty, value);
        }

        // DependencyProperty for WrapPanelBackground
        public static readonly DependencyProperty WrapPanelBackgroundProperty =
            DependencyProperty.Register(
                nameof(WrapPanelBackground),
                typeof(Brush),
                typeof(ToggleButtonOptionDisplay),
                new PropertyMetadata(null));

        public Brush WrapPanelBackground
        {
            get => (Brush)GetValue(WrapPanelBackgroundProperty);
            set => SetValue(WrapPanelBackgroundProperty, value);
        }

        // DependencyProperty for WrapPanelPadding
        public static readonly DependencyProperty WrapPanelPaddingProperty =
            DependencyProperty.Register(
                nameof(WrapPanelPadding),
                typeof(Thickness),
                typeof(ToggleButtonOptionDisplay),
                new PropertyMetadata(new Thickness(0)));

        public Thickness WrapPanelPadding
        {
            get => (Thickness)GetValue(WrapPanelPaddingProperty);
            set => SetValue(WrapPanelPaddingProperty, value);
        }

        // DependencyProperty for WrapPanelBorderBrush
        public static readonly DependencyProperty WrapPanelBorderBrushProperty =
            DependencyProperty.Register(
                nameof(WrapPanelBorderBrush),
                typeof(Brush),
                typeof(ToggleButtonOptionDisplay),
                new PropertyMetadata(null));

        public Brush WrapPanelBorderBrush
        {
            get => (Brush)GetValue(WrapPanelBorderBrushProperty);
            set => SetValue(WrapPanelBorderBrushProperty, value);
        }

        #endregion

        #region Fields/Properties

        static Brush mUnmodifiedBrush = null;


        List<ToggleButton> toggleButtons = new List<ToggleButton>();

        InstanceMember? _instanceMember;

        public InstanceMember? InstanceMember
        {
            get
            {
                return _instanceMember;
            }
            set
            {
                var didChange = _instanceMember != value;
                if (_instanceMember != null && didChange)
                {
                    _instanceMember.PropertyChanged -= HandlePropertyChange;
                }
                _instanceMember = value;
                if (_instanceMember != null && didChange)
                {
                    _instanceMember.PropertyChanged += HandlePropertyChange;
                }
                Refresh();
                if(didChange)
                {
                    RefreshAllContextMenus(true);
                }
            }
        }

        public bool SuppressSettingProperty { get; set; }

        public Brush DesiredBackgroundBrush
        {
            get
            {
                if (InstanceMember.IsDefault)
                {
                    return Brushes.LightGreen;
                }
                else if(InstanceMember.IsIndeterminate)
                {
                    return Brushes.LightGray;
                }
                else
                {
                    return mUnmodifiedBrush;
                }
            }
        }

        #endregion

        public ToggleButtonOptionDisplay(Option[] options)
        {
            InitializeComponent();
            // We prob want this auto-height
            //this.Height = 40;
            RefreshButtonFromOptions(options);

            ButtonWrapPanel.ContextMenu = new ContextMenu();
            this.RefreshContextMenu(ButtonWrapPanel.ContextMenu);
            this.RefreshContextMenu(Grid.ContextMenu);

        }

        public void RefreshButtonFromOptions(Option[] options)
        {
            var areSame = true;

            if(options.Length != toggleButtons.Count)
            {
                areSame = false;
            }


            if (areSame)
            {
                // image comparison would be expensive, so let's just do tag
                for (int i = 0; i < toggleButtons.Count; i++)
                {
                    var button = toggleButtons[i];
                    var option = options[i];
                    if (button.Tag != option)
                    {
                        areSame = false;
                        break;
                    }
                }
            }

            if (!areSame)
            {
                ForceRefreshButtons(options);
            }
        }

        private void ForceRefreshButtons(Option[] options)
        {
            ButtonWrapPanel.Children.Clear();
            toggleButtons.Clear();

            DataTemplate? dataTemplate =
                (TryFindResource(ContentTemplateKey) ??
                 Application.Current.TryFindResource(ContentTemplateKey)) as DataTemplate;

            DataTemplate? gumIconTemplate = (TryFindResource(GumIconTemplateKey) ??
                 Application.Current.TryFindResource(GumIconTemplateKey)) as DataTemplate;

            foreach (var option in options)
            {
                var toggleButton = new ToggleButton()
                {
                    DataContext = option
                };

                if (mUnmodifiedBrush == null)
                {
                    mUnmodifiedBrush = toggleButton.Background;
                }

                if (gumIconTemplate is not null && option.GumIconName is not null)
                {
                    toggleButton.ContentTemplate = gumIconTemplate;
                    toggleButton.Content = option;
                }
                else if (dataTemplate is not null && option.IconName is not null)
                {
                    toggleButton.ContentTemplate = dataTemplate;
                    toggleButton.Content = option;
                }
                else if (option.Image != null)
                {
                    //var stackPanel = new StackPanel();


                    //stackPanel.Children.Add(image);

                    //var label = new TextBlock();
                    //label.Text = "hi";
                    //stackPanel.Children.Add(label);

                    //toggleButton.Content = image;
                    var image = new Image()
                    {
                        
                    };

                    image.Source = option.Image;
                    toggleButton.Content = image;

                }
                else
                {
                    toggleButton.Content = option.Name;
                }


                var tooltip = new ToolTip();
                tooltip.Content = option.Name;
                toggleButton.ToolTip = tooltip;

                toggleButton.Click += HandleToggleClick;
                toggleButton.Tag = option;
                toggleButtons.Add(toggleButton);
                ButtonWrapPanel.Children.Add(toggleButton);

            }
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            SuppressSettingProperty = true;

            string propertyName = InstanceMember?.DisplayName;

            if (propertyName != null)
            {
                propertyName = InsertSpacesInCamelCaseString(propertyName);
            }
            Label.Text = propertyName;
            TrySetValueOnUi(InstanceMember.Value);

            RefreshAllContextMenus();

            HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
            HintTextBlock.Text = InstanceMember?.DetailText;

            RefreshButtonAppearance();

            RefreshIsEnabled();

            SuppressSettingProperty = false;
        }

        private void RefreshAllContextMenus(bool force=false)
        {
            if(force)
            {
                this.ForceRefreshContextMenu(ButtonWrapPanel.ContextMenu);
                this.ForceRefreshContextMenu(Grid.ContextMenu);

            }
            else
            {
                this.RefreshContextMenu(ButtonWrapPanel.ContextMenu);
                this.RefreshContextMenu(Grid.ContextMenu);
            }
        }

        private void HandlePropertyChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value))
            {
                this.Refresh();
            }
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
                ButtonWrapPanel.IsEnabled = false;
            }
            else
            {
                ButtonWrapPanel.IsEnabled = true;
            }
        }

        private void RefreshButtonAppearance()
        {
            if (DataUiGrid.GetOverridesIsDefaultStyling(this))
            {
                return;
            }

            foreach (var button in toggleButtons)
            {
                if (button.Template is not null)
                {
                    break;
                }
                button.Background = DesiredBackgroundBrush;
                const double smallSize = 30;
                // 35 vs 30 is hard to tell when default, so let's
                // increase it slightly...
                //const double largeSize = 35;
                const double largeSize = 38;

                if (button.IsChecked == true)
                {
                    button.Width = largeSize;
                    button.Height = largeSize;
                }
                else
                {
                    button.Width = smallSize;
                    button.Height = smallSize;
                }
            }
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

        private void HandleToggleClick(object? sender, RoutedEventArgs e)
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
            RefreshButtonAppearance();

        }

        static string InsertSpacesInCamelCaseString(string originalString)
        {
            // Normally in reverse loops you go til i > -1, but 
            // we don't want the character at index 0 to be tested.
            for (int i = originalString.Length - 1; i > 0; i--)
            {
                if (char.IsUpper(originalString[i]) && i != 0
                    // make sure there's not already a space there
                    && originalString[i - 1] != ' '
                    )
                {
                    originalString = originalString.Insert(i, " ");
                }
            }

            return originalString;
        }
    }
}
