using Gum.Wireframe;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace MonoGameGum.Forms.DefaultVisuals;

public class LabelVisual : InteractiveGue
{
    public TextRuntime TextInstance { get; private set; }
    public LabelVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            this.Width = 128;
            this.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            this.Height = 0;
            this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.X = 0;
            this.Y = 0;

            TextInstance = new TextRuntime();
            TextInstance.X = 0;
            TextInstance.Y = 0;
            TextInstance.Width = 0;
            TextInstance.Height = 0;
            TextInstance.Name = "TextInstance";
            TextInstance.Text = "label";
            TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            TextInstance.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Left;
            TextInstance.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Top;
            TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            TextInstance.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Left;
            TextInstance.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.Children.Add(TextInstance);
        }
        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Label(this);
        }

    }

    public Label FormsControl => FormsControlAsObject as Label;
}
