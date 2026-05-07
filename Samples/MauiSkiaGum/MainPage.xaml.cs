using MauiSkiaGum.Components;
using SkiaGum.Content;
using Gum.GueDeriving;
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
            MainStack.WrapsChildren = true;
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

            var component = new TestComponentRuntime();
            MainStack.AddChild(component);

            // Arc examples - three different APIs for setting stroke width, all of which
            // should produce identical results.
            AddArc("Thickness =",   ArcStrokeApi.ThicknessProperty);
            AddArc("StrokeWidth =", ArcStrokeApi.StrokeWidthProperty);
            AddArc("SetProperty",   ArcStrokeApi.SetPropertyOfStrokeWidth);

            SkiaGumCanvasView.InvalidateSurface();
        }

        private enum ArcStrokeApi
        {
            ThicknessProperty,
            StrokeWidthProperty,
            SetPropertyOfStrokeWidth,
        }

        private void AddArc(string label, ArcStrokeApi api)
        {
            const float diameter = 120;
            const float stroke = diameter * 0.4f;

            // Container so the disc + arc overlap at the same position while the caption sits
            // below, and the whole probe takes one slot in the LeftToRightStack.
            var container = new ContainerRuntime();
            MainStack.AddChild(container);
            container.Width = diameter;
            container.Height = diameter + 24;

            // Solid red disc behind a thick black almost-full-sweep arc filling the same
            // bounding box. The black ring should obscure most of the red disc; if the stroke
            // value didn't make it through, the red disc shows through where the ring should be.
            var disc = new ColoredCircleRuntime();
            container.AddChild(disc);
            disc.Width = diameter;
            disc.Height = diameter;
            disc.Color = SKColors.Red;
            disc.IsFilled = true;

            var arc = new ArcRuntime();
            container.AddChild(arc);
            arc.Width = diameter;
            arc.Height = diameter;
            arc.Color = SKColors.Black;
            arc.StartAngle = 0;
            arc.SweepAngle = 359;
            arc.IsEndRounded = false;

            switch (api)
            {
                case ArcStrokeApi.ThicknessProperty:
                    arc.Thickness = stroke;
                    break;
                case ArcStrokeApi.StrokeWidthProperty:
                    arc.StrokeWidth = stroke;
                    break;
                case ArcStrokeApi.SetPropertyOfStrokeWidth:
                    arc.SetProperty("StrokeWidth", stroke);
                    break;
            }

            var caption = new TextRuntime();
            container.AddChild(caption);
            caption.Y = diameter + 4;
            caption.Width = diameter;
            caption.Text = label;
            caption.Color = SKColors.Black;
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
