using System;
using System.Collections.Generic;
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

namespace EditorTabPlugin_XNA.Views;
/// <summary>
/// Interaction logic for EditorControls.xaml
/// </summary>
public partial class EditorControls : UserControl
{
    private const double DefaultBaseFontSize = 12.0;
    private const double DefaultButtonWidth = 20.0;

    public EditorControls()
    {
        InitializeComponent();
    }

    internal void UpdateButtonSizes(double baseFontSize)
    {
        double scale = baseFontSize / DefaultBaseFontSize;
        double buttonWidth = DefaultButtonWidth * scale;

        MinusButton.Width = buttonWidth;
        MinusButton.FontSize = baseFontSize;

        PlusButton.Width = buttonWidth;
        PlusButton.FontSize = baseFontSize;
    }
}
