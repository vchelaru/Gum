using BaseTextBoxVisual = Gum.Forms.DefaultVisuals.V3.TextBoxVisual;
using Gum.Forms.DefaultVisuals.V3;
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;

namespace Gum.Themes.Editor;

public class TextBoxVisual : BaseTextBoxVisual
{
    public TextBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        var outline = new RectangleRuntime();
        this.AddChild(outline);
        outline.Dock(Gum.Wireframe.Dock.Fill);

        this.FocusedIndicator.Parent = null;

        PlaceholderColor = Styling.ActiveStyle.Colors.TextMuted;
        ForegroundColor = Styling.ActiveStyle.Colors.TextPrimary;
        BackgroundColor = new Color(10, 10, 10);
        SelectionBackgroundColor = new Color(0, 92, 128);
        SelectionInstance.Blend = Gum.RenderingLibrary.Blend.Additive;
        CaretColor = new Color(192, 222, 255);

        var selectionParent = SelectionInstance.Parent;
        selectionParent.Children.Move(selectionParent.Children.IndexOf(SelectionInstance), selectionParent.Children.Count - 1);

        States.Enabled.Apply += () =>
        {
            outline.Visible = false;
        };

        States.Disabled.Apply += () =>
        {
            outline.Visible = false;
        };

        States.Highlighted.Apply += () =>
        {
            outline.Color = new Color(150, 150, 150);
            outline.Visible = true;
        };

        States.Focused.Apply += () =>
        {
            outline.Color = new Color(192, 222, 255);
            outline.Visible = true;
        };
    }
}
