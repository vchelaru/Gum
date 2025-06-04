using SkiaGum.GueDeriving;
using SkiaSharp;

namespace MauiSkiaGum
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();


            for(int i = 0; i < 1; i++)
            {
                var circle = new ColoredCircleRuntime();
                circle.Color = SKColors.Red;
                circle.X = 100;
                circle.Y = 50;

                SkiaGumCanvasView.AddChild(circle);
            }

        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }

}
