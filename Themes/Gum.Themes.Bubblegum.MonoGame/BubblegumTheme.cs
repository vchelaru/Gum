using System.IO;
using System.Reflection;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using GumRuntime;
using KernSmith.Gum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Graphics.Fonts;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Entry point for the Bubblegum theme. Call <see cref="Apply"/> once after
/// initializing Gum to register the bundled Nunito fonts, configure the
/// shared <see cref="Styling.ActiveStyle"/> tokens, and install the Bubblegum
/// visuals as the default templates for Forms controls.
/// </summary>
public static class BubblegumTheme
{
    /// <summary>
    /// Family name the bundled Nunito TTFs are registered under. Use this as the
    /// <c>Font</c> property value on any TextRuntime to get the theme font.
    /// </summary>
    public const string FontFamily = "Nunito";

    /// <summary>
    /// Default text size used by the theme. Matches the source mockup's
    /// <c>--fs</c> token (14px).
    /// </summary>
    public const int FontSize = 14;

    /// <summary>
    /// Applies the Bubblegum theme: wires KernSmith as the in-memory font
    /// creator, registers the bundled Nunito TTFs (Regular / Bold / Italic /
    /// BoldItalic), populates <see cref="Styling.ActiveStyle"/> with the
    /// Bubblegum color and text tokens, and registers the theme's visuals as
    /// the default templates for Forms controls.
    /// </summary>
    /// <param name="graphicsDevice">The active <see cref="GraphicsDevice"/>;
    /// required by KernSmith to rasterize fonts into textures.</param>
    public static void Apply(GraphicsDevice graphicsDevice)
    {
        CustomSetPropertyOnRenderable.InMemoryFontCreator =
            new KernSmithFontCreator(graphicsDevice);

        // Bubblegum's CSS doesn't use Dingbats / Geometric Shapes glyphs (no
        // ✓ / ▼ etc. in the source mockup — check marks are SVG paths there).
        // Visuals will use Apos.Shapes line strokes for the check / dash /
        // arrow chrome instead of Text glyphs, so no separate icon font is
        // needed and BmfcSave.AddCharacters is unnecessary.

        // Bubblegum's visuals build their bodies out of Apos.Shapes-backed
        // RoundedRectangleRuntime instances, which require ShapeRenderer to
        // be initialized. Guard for the case where the host app already
        // initialized it (mixing themes / using shape runtimes elsewhere).
        if (!ShapeRenderer.Self.IsInitialized)
        {
            ShapeRenderer.Self.Initialize();
        }

        RegisterBundledFonts();

        ConfigureStyling();

        RegisterVisuals();
    }

    private static void RegisterBundledFonts()
    {
        // Nunito ships a true 700-weight Bold, so Gum's IsBold slot maps
        // straight to Nunito-Bold (unlike Dark Pro's Medium-as-Bold mapping
        // for DM Mono). Bubblegum's CSS uses weights 700-900; all collapse to
        // the Bold slot in Gum's four-style font model.
        RegisterEmbeddedFont(FontFamily, "Nunito-Regular.ttf", style: null);
        RegisterEmbeddedFont(FontFamily, "Nunito-Bold.ttf", style: "Bold");
        RegisterEmbeddedFont(FontFamily, "Nunito-Italic.ttf", style: "Italic");
        RegisterEmbeddedFont(FontFamily, "Nunito-BoldItalic.ttf", style: "BoldItalic");
    }

    private static void RegisterEmbeddedFont(string family, string fileName, string? style)
    {
        // Resource prefix is derived from the assembly name so this code works
        // in both the MonoGame and KNI variants without forking. The KNI csproj
        // re-embeds the TTFs via <Link> so the resource path inside that
        // assembly is "<assembly-name>.Content.Fonts.<file>".
        Assembly assembly = typeof(BubblegumTheme).Assembly;
        string resourceName = $"{assembly.GetName().Name}.Content.Fonts.{fileName}";

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException(
                $"Embedded font resource '{resourceName}' not found. " +
                $"Verify the .ttf is included as <EmbeddedResource> in the theme csproj.");
        }

        using MemoryStream buffer = new MemoryStream();
        stream.CopyTo(buffer);
        byte[] fontBytes = buffer.ToArray();

        if (style == null)
        {
            KernSmithFontCreator.RegisterFont(family, fontBytes);
        }
        else
        {
            KernSmithFontCreator.RegisterFont(family, fontBytes, style: style);
        }
    }

    private static void ConfigureStyling()
    {
        Styling styling = Styling.ActiveStyle;

        styling.Text.Normal.Clear();
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "Font", Type = "string", Value = FontFamily });
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "FontSize", Type = "int", Value = FontSize });
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "IsBold", Type = "bool", Value = false });
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "IsItalic", Type = "bool", Value = false });

        styling.Text.Strong.Clear();
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "Font", Type = "string", Value = FontFamily });
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "FontSize", Type = "int", Value = FontSize });
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "IsBold", Type = "bool", Value = true });
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "IsItalic", Type = "bool", Value = false });

        styling.Colors.TextPrimary = BubblegumColors.Text;
        styling.Colors.TextMuted = BubblegumColors.Muted;
        styling.Colors.Primary = BubblegumColors.Surface1;
        styling.Colors.Accent = BubblegumColors.Accent;
    }

    private static void RegisterVisuals()
    {
        FrameworkElement.DefaultFormsTemplates[typeof(Button)] =
            new VisualTemplate((_, c) => new ButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(CheckBox)] =
            new VisualTemplate((_, c) => new CheckBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(RadioButton)] =
            new VisualTemplate((_, c) => new RadioButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(TextBox)] =
            new VisualTemplate((_, c) => new TextBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(PasswordBox)] =
            new VisualTemplate((_, c) => new PasswordBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Slider)] =
            new VisualTemplate((_, c) => new SliderVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ListBox)] =
            new VisualTemplate((_, c) => new ListBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ListBoxItem)] =
            new VisualTemplate((_, c) => new ListBoxItemVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ScrollBar)] =
            new VisualTemplate((_, c) => new ScrollBarVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ScrollViewer)] =
            new VisualTemplate((_, c) => new ScrollViewerVisual(tryCreateFormsObject: c));

        // Label gets its color from Styling.ActiveStyle.Colors.TextPrimary (set
        // in ConfigureStyling), so the V3 LabelVisual already renders Bubblegum
        // text color without a subclass.
        FrameworkElement.DefaultFormsTemplates[typeof(Label)] =
            new VisualTemplate((_, c) => new Gum.Forms.DefaultVisuals.V3.LabelVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ComboBox)] =
            new VisualTemplate((_, c) => new ComboBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Window)] =
            new VisualTemplate((_, c) => new WindowVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Splitter)] =
            new VisualTemplate((_, c) => new SplitterVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Menu)] =
            new VisualTemplate((_, c) => new MenuVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(MenuItem)] =
            new VisualTemplate((_, c) => new MenuItemVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ToggleButton)] =
            new VisualTemplate((_, c) => new ToggleButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Tooltip)] =
            new VisualTemplate((_, c) => new TooltipVisual(tryCreateFormsObject: c));
    }
}
