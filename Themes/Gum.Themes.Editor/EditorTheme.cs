using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using KernSmith.Gum;
using GumRuntime;

namespace Gum.Themes.Editor;

public static class EditorTheme
{
    public static void Apply(GraphicsDevice graphicsDevice)
    {
        CustomSetPropertyOnRenderable.InMemoryFontCreator =
            new KernSmithFontCreator(graphicsDevice);

        var styling = Styling.ActiveStyle;

        styling.Text.Normal.Clear();
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "Font", Type = "string", Value = "Arial" });
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "FontSize", Type = "int", Value = 15 });
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "IsBold", Type = "bool", Value = false });
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "IsItalic", Type = "bool", Value = false });

        styling.Text.Strong.Clear();
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "Font", Type = "string", Value = "Arial" });
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "FontSize", Type = "int", Value = 15 });
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "IsBold", Type = "bool", Value = true });
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "IsItalic", Type = "bool", Value = false });

        styling.Colors.TextPrimary = new Color(180, 180, 180);
        styling.Colors.TextMuted = new Color(88, 88, 88);

        styling.Colors.Primary = new Color(60, 60, 60);

        RegisterVisuals();
    }

    private static void RegisterVisuals()
    {
        void TryAdd(System.Type formsType, System.Func<object, bool, GraphicalUiElement> factory)
        {
            FrameworkElement.DefaultFormsTemplates[formsType] = new VisualTemplate(factory);
        }

        TryAdd(typeof(TextBox), (_, c) => new TextBoxVisual(tryCreateFormsObject: c));
        TryAdd(typeof(Button), (_, c) => new ButtonVisual(tryCreateFormsObject: c));
        TryAdd(typeof(CheckBox), (_, c) => new CheckBoxVisual(tryCreateFormsObject: c));
        TryAdd(typeof(ComboBox), (_, c) => new ComboBoxVisual(tryCreateFormsObject: c));
        TryAdd(typeof(ListBoxItem), (_, c) => new ListBoxItemVisual(tryCreateFormsObject: c));
        TryAdd(typeof(ListBox), (_, c) => new ListBoxVisual(tryCreateFormsObject: c));
        TryAdd(typeof(ScrollBar), (_, c) => new ScrollBarVisual(tryCreateFormsObject: c));
        TryAdd(typeof(Slider), (_, c) => new SliderVisual(tryCreateFormsObject: c));
        TryAdd(typeof(Expander), (_, c) => new ExpanderVisual(tryCreateFormsObject: c));
    }
}
