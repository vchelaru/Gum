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
            CreateInitialSkiaLayout();
        }

        private void CreateInitialSkiaLayout()
        {
            MainStack = new ContainerRuntime();
            SkiaGumCanvasView.AddChild(MainStack);
            MainStack.Dock(Gum.Wireframe.Dock.Fill);
            MainStack.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
            MainStack.StackSpacing = 4;

            var roundedRectangle = new RoundedRectangleRuntime();
            MainStack.Children.Add(roundedRectangle);
            roundedRectangle.Width = 100;
            roundedRectangle.Height = 100;
            // set a default corner radius:
            roundedRectangle.CornerRadius = 20;
            // overwrite some:
            roundedRectangle.CustomRadiusTopLeft = 0;
            roundedRectangle.CustomRadiusTopRight = 40;
            roundedRectangle.CustomRadiusBottomLeft = 10;
            roundedRectangle.Color = SKColors.Blue;

            var text = new TextRuntime();
            MainStack.AddChild(text);
            text.Text = "Click the button to add circles below:";
            text.Color = SKColors.Black;
            text.FontSize = 24;

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
            MainStack.AddChild(circle);
            circle.Color = SKColors.Red;
            circle.Width = 30;
            circle.Height = 30;

            SkiaGumCanvasView.InvalidateSurface();

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }

}
