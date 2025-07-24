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
            MainStack.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
            MainStack.StackSpacing = 16;

            var roundedRectangle = new RoundedRectangleRuntime();
            MainStack.AddChild(roundedRectangle);
            roundedRectangle.Width = 100;
            roundedRectangle.Height = 100;
            roundedRectangle.Color = SKColors.Blue;

            // This is the default radius:
            roundedRectangle.CornerRadius = 20;

            // But we can overwite each one by setting a value:
            roundedRectangle.CustomRadiusTopRight = 40;
            roundedRectangle.CustomRadiusBottomLeft = 0;
            roundedRectangle.CustomRadiusBottomRight = 0;

            // undo assignments by setting the value back to null:
            roundedRectangle.CustomRadiusTopLeft = 50;
            roundedRectangle.CustomRadiusTopLeft = null;


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
