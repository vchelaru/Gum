using BaseComboBoxVisual = Gum.Forms.DefaultVisuals.V3.ComboBoxVisual;
using Gum.Forms.DefaultVisuals.V3;
using MonoGameGum.GueDeriving;

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

        DropdownIndicatorColor = Styling.ActiveStyle.Colors.TextPrimary;

        this.States.Enabled.Apply += () =>
        {
            rectangle.Visible = false;
        };
        this.States.Highlighted.Apply += () =>
        {
            rectangle.Visible = true;
            rectangle.Color = new Microsoft.Xna.Framework.Color(150, 150, 150);
        };
        this.States.Pushed.Apply += () =>
        {
            rectangle.Visible = true;
            rectangle.Color = new Microsoft.Xna.Framework.Color(255, 255, 255);
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
