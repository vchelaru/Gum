using Gum.GueDeriving;
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

namespace SkiaGumWpfSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var container = new ContainerRuntime();
            container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
            container.StackSpacing = 3;
            SkiaElement.Children.Add(container);


            var rectangle = new RectangleRuntime();

            rectangle.FillColor = new SkiaSharp.SKColor(50, 100, 0);
            rectangle.IsFilled = true;
            container.Children.Add(rectangle);

            var rectangle2 = new RectangleRuntime();
            rectangle2.FillColor = new SkiaSharp.SKColor(200, 0, 0);
            rectangle2.IsFilled = true;

            container.Children.Add(rectangle2);
        }
    }
}
