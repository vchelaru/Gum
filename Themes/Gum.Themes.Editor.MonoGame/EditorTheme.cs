using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endif
using GumRuntime;
using RenderingLibrary.Graphics.Fonts;

namespace Gum.Themes.Editor;

public static class EditorTheme
{
    /// <summary>
    /// Applies the Editor theme: wires runtime font generation, configures styling, and registers
    /// the themed Forms control visuals as the active default templates. Call once after Gum has
    /// been initialized (<c>GumService.Initialize</c>).
    /// </summary>
    public static void Apply()
    {
        ThemePlatform.WireInMemoryFontCreator();

        // Register special characters used by editor theme visuals (e.g., ExpanderVisual arrows)
        BmfcSave.AddCharacters("►▼");

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

#if !RAYLIB
    /// <summary>
    /// Backwards-compatible overload retained for existing MonoGame/KNI callers. The graphics
    /// device is now resolved internally from the active Gum renderer, so the argument is ignored;
    /// prefer the parameterless <see cref="Apply()"/>.
    /// </summary>
    public static void Apply(GraphicsDevice graphicsDevice) => Apply();
#endif

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
        TryAdd(typeof(Expander), (_, c) => new ExpanderVisual(tryCreateFormsObject: c));
        TryAdd(typeof(ListBoxItem), (_, c) => new ListBoxItemVisual(tryCreateFormsObject: c));
        TryAdd(typeof(ListBox), (_, c) => new ListBoxVisual(tryCreateFormsObject: c));
        TryAdd(typeof(ScrollBar), (_, c) => new ScrollBarVisual(tryCreateFormsObject: c));
        TryAdd(typeof(ScrollViewer), (_, c) => new ScrollViewerVisual(tryCreateFormsObject: c));
        TryAdd(typeof(Slider), (_, c) => new SliderVisual(tryCreateFormsObject: c));

        // Controls Editor does not restyle are pinned to their stock V3 visuals so that
        // EditorTheme.Apply fully specifies the template set. FrameworkElement.DefaultFormsTemplates
        // is global static state; without re-registering these, re-applying a theme at runtime
        // (the showcase's 1-6 swap, or a consumer switching skins) would leave a previously-applied
        // theme's visual in place for these controls, because Editor never overwrites their entries.
        // These are the same visuals a single-theme Editor run already falls back to, so this is a
        // no-op for consumers who apply Editor once.
        TryAdd(typeof(RadioButton), (_, c) => new Gum.Forms.DefaultVisuals.V3.RadioButtonVisual(tryCreateFormsObject: c));
        TryAdd(typeof(ToggleButton), (_, c) => new Gum.Forms.DefaultVisuals.V3.ToggleButtonVisual(tryCreateFormsObject: c));
        TryAdd(typeof(PasswordBox), (_, c) => new Gum.Forms.DefaultVisuals.V3.PasswordBoxVisual(tryCreateFormsObject: c));
        TryAdd(typeof(Menu), (_, c) => new Gum.Forms.DefaultVisuals.V3.MenuVisual(tryCreateFormsObject: c));
        TryAdd(typeof(MenuItem), (_, c) => new Gum.Forms.DefaultVisuals.V3.MenuItemVisual(tryCreateFormsObject: c));
        TryAdd(typeof(Window), (_, c) => new Gum.Forms.DefaultVisuals.V3.WindowVisual(tryCreateFormsObject: c));
        TryAdd(typeof(Splitter), (_, c) => new Gum.Forms.DefaultVisuals.V3.SplitterVisual(tryCreateFormsObject: c));
        TryAdd(typeof(Label), (_, c) => new Gum.Forms.DefaultVisuals.V3.LabelVisual(tryCreateFormsObject: c));
        TryAdd(typeof(ItemsControl), (_, c) => new Gum.Forms.DefaultVisuals.V3.ItemsControlVisual(tryCreateFormsObject: c));
        TryAdd(typeof(Tooltip), (_, c) => new Gum.Forms.DefaultVisuals.V3.TooltipVisual(tryCreateFormsObject: c));
        TryAdd(typeof(Gum.Forms.Controls.Games.DialogBox), (_, c) => new Gum.Forms.DefaultVisuals.V3.DialogBoxVisual(tryCreateFormsObject: c));
    }
}
