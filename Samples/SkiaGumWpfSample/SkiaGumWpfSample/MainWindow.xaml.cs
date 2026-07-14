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

            // Demonstrates Skia OutlineThickness (issue #3675): yellow text with a black outline
            // rendered via RichTextKit's halo. Compare against the no-outline text below it.
            var outlinedText = new TextRuntime();
            outlinedText.Text = "Outlined";
            outlinedText.Font = "Arial";
            outlinedText.FontSize = 48;
            outlinedText.Red = 255;
            outlinedText.Green = 255;
            outlinedText.Blue = 0;
            outlinedText.OutlineThickness = 3;
            container.Children.Add(outlinedText);

            var plainText = new TextRuntime();
            plainText.Text = "No outline";
            plainText.Font = "Arial";
            plainText.FontSize = 48;
            plainText.Red = 255;
            plainText.Green = 255;
            plainText.Blue = 0;
            container.Children.Add(plainText);
        }
    }
}
