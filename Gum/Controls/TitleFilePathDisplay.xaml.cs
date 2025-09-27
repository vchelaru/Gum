using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Gum.Controls
{
    public partial class TitleFilePathDisplay : UserControl
    {
        public TitleFilePathDisplay()
        {
            InitializeComponent();
        }

        public string FullPath
        {
            get => (string)GetValue(FullPathProperty);
            set => SetValue(FullPathProperty, value);
        }

        public static readonly DependencyProperty FullPathProperty =
            DependencyProperty.Register(nameof(FullPath), typeof(string), typeof(TitleFilePathDisplay),
                new PropertyMetadata(string.Empty, OnFullPathChanged));

        public string FileName
        {
            get => (string)GetValue(FileNameProperty);
            private set => SetValue(FileNamePropertyKey, value);
        }

        private static readonly DependencyPropertyKey FileNamePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(FileName), typeof(string), typeof(TitleFilePathDisplay),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty FileNameProperty = FileNamePropertyKey.DependencyProperty;

        private static void OnFullPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TitleFilePathDisplay control && e.NewValue is string path)
            {
                try
                {
                    control.FileName = Path.GetFileNameWithoutExtension(path);
                }
                catch
                {
                    control.FileName = string.Empty;
                }
            }
        }
    }
}