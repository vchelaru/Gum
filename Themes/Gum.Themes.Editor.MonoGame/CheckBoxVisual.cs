using BaseCheckBoxVisual = Gum.Forms.DefaultVisuals.V3.CheckBoxVisual;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Editor;

public class CheckBoxVisual : BaseCheckBoxVisual
{
    public CheckBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        var rectangle = new RectangleRuntime();
        CheckBoxBackground.Children.Add(rectangle);
        rectangle.Dock(Gum.Wireframe.Dock.Fill);

        this.States.EnabledOn.Apply += () =>
        {
            rectangle.Visible = false;
        };

        this.States.EnabledOff.Apply += () =>
        {
            rectangle.Visible = false;
        };

        this.States.HighlightedOn.Apply += () =>
        {
            rectangle.Visible = true;
            rectangle.StrokeColor = EditorStyling.ActiveStyle.Colors.BorderHover;
        };
        this.States.HighlightedOff.Apply += () =>
        {
            rectangle.Visible = true;
            rectangle.StrokeColor = EditorStyling.ActiveStyle.Colors.BorderHover;
        };

        this.States.PushedOn.Apply += () =>
        {
            rectangle.Visible = true;
            rectangle.StrokeColor = EditorStyling.ActiveStyle.Colors.BorderPushed;
        };
        this.States.PushedOff.Apply += () =>
        {
            rectangle.Visible = true;
            rectangle.StrokeColor = EditorStyling.ActiveStyle.Colors.BorderPushed;
        };

        this.States.DisabledOn.Apply += () =>
        {
            rectangle.Visible = false;
        };

        this.States.DisabledOff.Apply += () =>
        {
            rectangle.Visible = false;
        };
    }
}
