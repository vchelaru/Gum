using Gum.GueDeriving;
using SkiaGum;
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

            // Issue #3708 manual check: two columns of small-font list items, each row nudged to a
            // different fractional X so it lands at a different sub-pixel offset -- like real list
            // items whose X varies with scroll/animation. Left column uses the SnapToPixel default
            // (rounds the draw origin to a whole pixel); right column forces FreeFloating (paints at
            // the exact fractional position). At a small font size the left column's glyph edges
            // should look uniformly crisp row-to-row; the right column's edges should look slightly
            // softer/inconsistent since each row's anti-aliasing lands on a different sub-pixel offset.
            string[] labels = { "Item One", "Item Two", "Item Three", "Item Four", "Item Five" };
            float[] fractionalOffsets = { 0.1f, 0.3f, 0.5f, 0.7f, 0.9f };

            var textBackground = new RectangleRuntime();
            textBackground.FillColor = new SkiaSharp.SKColor(230, 230, 230);
            textBackground.IsFilled = true;
            textBackground.X = 10;
            textBackground.Y = 140;
            textBackground.Width = 400;
            textBackground.Height = 120;
            SkiaElement.Children.Add(textBackground);

            for (int i = 0; i < labels.Length; i++)
            {
                var snapped = new TextRuntime();
                snapped.Text = labels[i];
                snapped.FontSize = 11;
                snapped.Color = new SkiaSharp.SKColor(0, 0, 0);
                snapped.X = 20 + fractionalOffsets[i];
                snapped.Y = 150 + i * 20;
                // TextRenderingPositionMode left at its default (SnapToPixel).
                SkiaElement.Children.Add(snapped);

                var freeFloating = new TextRuntime();
                freeFloating.Text = labels[i];
                freeFloating.FontSize = 11;
                freeFloating.Color = new SkiaSharp.SKColor(0, 0, 0);
                freeFloating.X = 220 + fractionalOffsets[i];
                freeFloating.Y = 150 + i * 20;
                freeFloating.TextRenderingPositionMode = TextRenderingPositionMode.FreeFloating;
                SkiaElement.Children.Add(freeFloating);
            }
        }
    }
}
