using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;

namespace MonoGameGumImmediateMode.Screens
{
    /// <summary>
    /// Demonstrates that <see cref="GumBatch.Draw(IRenderableIpso)"/> recursively
    /// draws an object's children, respecting the parent/child layout rules. Only
    /// the parent needs to be passed to <c>Draw</c>.
    /// </summary>
    public class ParentChildScreen : IImmediateModeScreen
    {
        private ColoredRectangleRuntime _buttonRectangle;

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            _buttonRectangle = new ColoredRectangleRuntime();
            _buttonRectangle.Width = 200;
            _buttonRectangle.Height = 48;
            _buttonRectangle.Color = Color.DarkBlue;
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

        public void Draw(GumBatch gumBatch, SpriteBatch spriteBatch)
        {
            gumBatch.Begin();
            // Only the parent is drawn — the child TextRuntime is rendered
            // automatically as part of the hierarchy.
            gumBatch.Draw(_buttonRectangle);
            gumBatch.End();
        }

        public void Dispose()
        {
        }
    }
}
