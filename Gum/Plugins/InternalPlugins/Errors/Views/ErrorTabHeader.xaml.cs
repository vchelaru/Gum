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

namespace Gum.Plugins.InternalPlugins.Errors.Views;
/// <summary>
/// Interaction logic for ErrorTabHeader.xaml
/// </summary>
public partial class ErrorTabHeader : UserControl
{
    public void SetErrorCount(int count)
    {
        if (count == 0)
        {
            CircleInstance.Fill = Brushes.Green;
            LabelInstance.Content = "0 Errors";
        }
        else
        {
            CircleInstance.Fill = Brushes.Red;

            if (count == 1)
            {
                LabelInstance.Content = "1 Error";
            }
            else
            {
                LabelInstance.Content = $"{count} Errors";
            }
        }
    }

    public ErrorTabHeader()
    {
        InitializeComponent();

        SetErrorCount(0);
    }
}
