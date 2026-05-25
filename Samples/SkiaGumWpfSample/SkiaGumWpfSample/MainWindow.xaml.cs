using Gum.GueDeriving;
using Gum.RenderingLibrary;
using SkiaGum.Renderables;
using System.Windows;

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

            // Issue #2922 smoke test: two overlapping rectangle pairs.
            // Top row uses default (Normal/SrcOver) blending — the second rectangle covers the
            // first. Bottom row sets Blend = Additive on the second rectangle — the overlapped
            // area shows the channel-summed color (red+green = yellow) instead of just red.
            //
            // RectangleRuntime doesn't expose Blend on its cross-platform runtime API today
            // (only SpriteRuntime / NineSliceRuntime do — PR #2920). Reach the Skia-side
            // RoundedRectangle renderable directly to set Blend; this is sample-internal code,
            // not the intended consumer pattern.

            AddOverlappedPair(yOffset: 20, additive: false);
            AddOverlappedPair(yOffset: 180, additive: true);
        }

        private void AddOverlappedPair(float yOffset, bool additive)
        {
            var greenRect = new RectangleRuntime();
            greenRect.X = 20;
            greenRect.Y = yOffset;
            greenRect.Width = 200;
            greenRect.Height = 120;
            greenRect.FillColor = new SkiaSharp.SKColor(0, 220, 0);
            SkiaElement.Children.Add(greenRect);

            var redRect = new RectangleRuntime();
            redRect.X = 120;
            redRect.Y = yOffset + 30;
            redRect.Width = 200;
            redRect.Height = 120;
            redRect.FillColor = new SkiaSharp.SKColor(220, 0, 0);
            SkiaElement.Children.Add(redRect);

            if (additive)
            {
                // Additive on Skia (#2922) maps to SKBlendMode.Plus.
                ((RoundedRectangle)redRect.RenderableComponent!).Blend = Blend.Additive;
            }
        }
    }
}
