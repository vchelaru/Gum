using Gum.Wireframe;
using RenderingLibrary.Graphics;


#if XNALIKE
using MonoGameGum.GueDeriving;
using Microsoft.Xna.Framework;
#else
using Gum.GueDeriving;
#endif
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for a Label control. Extends TextRuntime directly, sized to its text content.
/// </summary>
public class LabelVisual : TextRuntime
{
    public LabelVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(fullInstantiation)
    {
        if (fullInstantiation)
        {
            this.SuspendLayout();
            Name = "TextInstance";
            Text = "Label";
            X = 0;
            Y = 0;
            Width = 0;
            WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            Height = 0;
            HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.Color = Styling.ActiveStyle.Colors.TextPrimary;

            this.ApplyState(Styling.ActiveStyle.Text.Normal);
            this.ResumeLayout();
        }
        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Label(this);
        }
    }

    /// <summary>
    /// Returns the strongly-typed Label Forms control backing this visual.
    /// </summary>
    public Label FormsControl => (Label)FormsControlAsObject;
}
