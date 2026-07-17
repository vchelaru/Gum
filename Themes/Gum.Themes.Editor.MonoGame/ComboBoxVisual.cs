using BaseComboBoxVisual = Gum.Forms.DefaultVisuals.V3.ComboBoxVisual;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Editor;

public class ComboBoxVisual : BaseComboBoxVisual
{
    public ComboBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        var rectangle = new RectangleRuntime();
        this.Children.Add(rectangle);
        rectangle.Dock(Gum.Wireframe.Dock.Fill);

        this.FocusedIndicator.Parent = null;

        DropdownIndicatorColor = EditorStyling.ActiveStyle.Colors.TextPrimary;

        this.States.Enabled.Apply += () =>
        {
            rectangle.Visible = false;
        };
        this.States.Highlighted.Apply += () =>
        {
            rectangle.Visible = true;
            rectangle.StrokeColor = EditorStyling.ActiveStyle.Colors.BorderHover;
        };
        this.States.Pushed.Apply += () =>
        {
            rectangle.Visible = true;
            rectangle.StrokeColor = EditorStyling.ActiveStyle.Colors.BorderPushed;
        };
        this.States.Disabled.Apply += () =>
        {
            rectangle.Visible = false;
        };

        this.ListBoxInstance.UseFixedStackChildrenSize = true;
        var listBoxVisual = this.ListBoxInstance as ListBoxVisual;
        listBoxVisual?.MakeHeightSizedToChildren();
        
    }
}
