using SkiaGum.GueDeriving;
using SkiaGum.Maui;
using SkiaSharp;

namespace MauiSkiaGum
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        ContainerRuntime MainStack;

        public MainPage()
        {
            InitializeComponent();

            MainStack = new ContainerRuntime();
            MainStack.Dock(Gum.Wireframe.Dock.Fill);
            MainStack.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
            SkiaGumCanvasView.AddChild(MainStack);

            var text = new TextRuntime();
            text.Text = "Click the button to add circles below:";
            MainStack.AddChild(text);

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

            MainStack.AddChild(circle);
            SkiaGumCanvasView.InvalidateSurface();

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }

}
