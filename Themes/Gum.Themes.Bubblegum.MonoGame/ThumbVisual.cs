using Gum.DataTypes;
using Gum.Forms.Controls;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// The slider and scroll bar thumb. It is a copy of this theme's <see cref="ButtonVisual"/>, so it
/// looks the same until it is restyled; restyle it here without touching buttons.
/// </summary>
public class ThumbVisual : ButtonVisual
{
    public ThumbVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject: false)
    {
        // A thumb has no label to size it, so it keeps a fixed height rather than the button's label-sized one.
        Height = 36;
        HeightUnits = DimensionUnitType.Absolute;

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Thumb(this);
        }
    }
}