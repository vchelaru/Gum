using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace KniSkiaGumImmediateMode.Screens;

/// <summary>
/// Demonstrates that <see cref="GumBatch.Draw(RenderingLibrary.Graphics.IRenderableIpso)"/> recursively
/// draws an object's children, respecting the parent/child layout rules. Only the parent needs to be
/// passed to <c>Draw</c>.
/// </summary>
public class ParentChildScreen : IImmediateModeScreen
{
    private RectangleRuntime _buttonRectangle = null!;

    public void Initialize()
    {
        _buttonRectangle = new RectangleRuntime();
        _buttonRectangle.Width = 200;
        _buttonRectangle.Height = 48;
        _buttonRectangle.IsFilled = true;
        _buttonRectangle.FillColor = SKColors.DarkBlue;
        _buttonRectangle.X = 60;
        _buttonRectangle.Y = 120;

        TextRuntime buttonText = new TextRuntime();
        buttonText.Font = "Arial";
        buttonText.FontSize = 16;
        buttonText.Text = "Button text";
        buttonText.X = 0;
        buttonText.Y = 0;
        buttonText.Width = 0;
        buttonText.Height = 0;
        buttonText.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        buttonText.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        buttonText.XOrigin = HorizontalAlignment.Center;
        buttonText.YOrigin = VerticalAlignment.Center;
        buttonText.HorizontalAlignment = HorizontalAlignment.Center;
        buttonText.VerticalAlignment = VerticalAlignment.Center;
        buttonText.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        buttonText.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        _buttonRectangle.Children.Add(buttonText);
    }

    public void Draw(GumBatch gumBatch, SKCanvas canvas)
    {
        gumBatch.Begin();
        // Only the parent is drawn - the child TextRuntime is rendered automatically as part of the
        // hierarchy.
        gumBatch.Draw(_buttonRectangle);
        gumBatch.End();
    }

    public void Dispose()
    {
    }
}
