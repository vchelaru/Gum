using SkiaGum.GueDeriving;
using SkiaGum.Maui;
using SkiaSharp;

namespace MauiSkiaGum
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();


            SkiaGumCanvasView.InvalidateSurface();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            var circle = new ColoredCircleRuntime();
            circle.Color = SKColors.Red;
            circle.Width = 30;
            circle.Height = 30;
            circle.X = 36*count;
            circle.Y = 50;

            SkiaGumCanvasView.AddChild(circle);
            SkiaGumCanvasView.InvalidateSurface();

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }

}
