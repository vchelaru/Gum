using BaseScrollBarVisual = Gum.Forms.DefaultVisuals.V3.ScrollBarVisual;

namespace Gum.Themes.Editor;

public class ScrollBarVisual : BaseScrollBarVisual
{
    public ScrollBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        this.TrackBackgroundColor = new Microsoft.Xna.Framework.Color(10, 10, 10);
        this.ThumbInstance.BackgroundColor = new Microsoft.Xna.Framework.Color(60, 60, 60);
    }
}
