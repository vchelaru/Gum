using BaseTextBoxVisual = Gum.Forms.DefaultVisuals.V3.TextBoxVisual;
using Gum.Forms.DefaultVisuals.V3;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using Gum.GueDeriving;

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

        PlaceholderColor = EditorStyling.ActiveStyle.Colors.TextMuted;
        ForegroundColor = EditorStyling.ActiveStyle.Colors.TextPrimary;
        BackgroundColor = EditorStyling.ActiveStyle.Colors.RecessedBackground;
        SelectionBackgroundColor = EditorStyling.ActiveStyle.Colors.Selection;
        SelectionInstance.Blend = Gum.RenderingLibrary.Blend.Additive;
        CaretColor = EditorStyling.ActiveStyle.Colors.Accent;

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
            outline.StrokeColor = EditorStyling.ActiveStyle.Colors.BorderHover;
            outline.Visible = true;
        };

        States.Focused.Apply += () =>
        {
            outline.StrokeColor = EditorStyling.ActiveStyle.Colors.Accent;
            outline.Visible = true;
        };
    }
}
