using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Gum.Controls
{
    public partial class ColorPickerSwatch : UserControl, INotifyPropertyChanged
    {
        public ColorPickerSwatch()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(
                nameof(SelectedColor),
                typeof(Color?),
                typeof(ColorPickerSwatch),
                new FrameworkPropertyMetadata(Colors.Transparent, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorPickerSwatch swatch)
            {
                swatch.OnPropertyChanged(nameof(DisplayColor));
                swatch.OnPropertyChanged(nameof(PickerColor));
            }
        }

        public Color? SelectedColor
        {
            get => (Color?)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        // Helper property for the binding in XAML that needs a non-nullable Color for display
        public Color DisplayColor => SelectedColor ?? Colors.Transparent;

        // Property for the color picker that handles two-way binding
        public Color PickerColor
        {
            get => SelectedColor ?? Colors.Transparent;
            set
            {
                SelectedColor = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}