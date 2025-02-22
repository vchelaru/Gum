using SkiaGum.GueDeriving;
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


            var rectangle = new RoundedRectangleRuntime();

            rectangle.Red = 50;
            rectangle.Green = 100;
            rectangle.Blue = 0;
            container.Children.Add(rectangle);

            var rectangle2 = new RoundedRectangleRuntime();
            rectangle2.Red = 200;
            rectangle2.Green = 0;
            rectangle2.Blue = 0;

            container.Children.Add(rectangle2);


        }
    }
}
