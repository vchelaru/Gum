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

            // Issue #4037: per-run [OutlineThickness=N] BBCode tag, plus the whole-string
            // OutlineThickness property -- both now render through RichTextKit's halo with the
            // vendored round-join patch, so acute corners (W, V, A) shouldn't spike.
            var text = new TextRuntime();
            text.Width = 400;
            text.FontSize = 40;
            text.Color = SkiaSharp.SKColors.White;
            text.OutlineColor = SkiaSharp.SKColors.Red;
            text.OutlineThickness = 4;
            text.Text = "WAVY whole-string outline, [OutlineThickness=10]WAVY thick run[/OutlineThickness], [OutlineThickness=0]WAVY no outline[/OutlineThickness]";
            container.Children.Add(text);
        }
    }
}
