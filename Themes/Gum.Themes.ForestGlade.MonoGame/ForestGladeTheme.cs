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

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Entry point for the Forest Glade theme. Call <see cref="Apply"/> once
/// after initializing Gum to register the bundled Nunito / Fraunces /
/// DejaVu Sans Mono fonts, configure the shared
/// <see cref="Styling.ActiveStyle"/> tokens, and install the Forest Glade
/// visuals as the default templates for Forms controls.
/// </summary>
public static class ForestGladeTheme
{
    /// <summary>
    /// Family name the bundled Nunito TTFs are registered under. Body
    /// typeface from the CSS spec (<c>--ff: 'Nunito'</c>). Regular and Bold
    /// faces are both registered so Gum's IsBold slot resolves correctly.
    /// </summary>
    public const string FontFamily = "Nunito";

    /// <summary>
    /// Family name the bundled Fraunces TTF is registered under. CSS uses
    /// Fraunces italic on the Window title bar only
    /// (<c>--ff-display: 'Fraunces'</c>, <c>.fg-win-title</c>). The single
    /// embedded face is Fraunces 144pt Bold Italic.
    /// </summary>
    public const string TitleFontFamily = "Fraunces";

    /// <summary>
    /// Family name the bundled icon font (DejaVu Sans Mono) is registered
    /// under. Used for glyphs Nunito doesn't cover — check marks, dropdown
    /// chevrons, scroll-bar arrows, decorative flower glyph on the Window
    /// title (Dingbats and Geometric Shapes blocks).
    /// </summary>
    public const string IconFontFamily = "Forest Glade Icons";

    /// <summary>
    /// Default text size used by the theme. Matches the source mockup's
    /// <c>--fs</c> token (14px).
    /// </summary>
    public const int FontSize = 14;

    /// <summary>
    /// Applies the Forest Glade theme: wires KernSmith as the in-memory
    /// font creator, registers the bundled TTFs, populates
    /// <see cref="Styling.ActiveStyle"/> with the Forest Glade color and
    /// text tokens, and registers the theme's visuals as the default
    /// templates for Forms controls.
    /// </summary>
    /// <param name="graphicsDevice">The active <see cref="GraphicsDevice"/>;
    /// required by KernSmith to rasterize fonts into textures.</param>
    public static void Apply(GraphicsDevice graphicsDevice)
    {
        CustomSetPropertyOnRenderable.InMemoryFontCreator =
            new KernSmithFontCreator(graphicsDevice);

        // Pre-register the icon glyphs the theme renders as Text rather
        // than as sprite-sheet icons. KernSmith bakes only the characters
        // it has been told about, so anything outside ASCII has to be
        // declared before the first font generation. Forest Glade uses:
        //   ✓ ✕      — CheckBox tick / Window close glyph (Dingbats)
        //   ▼ ▲ ◀ ▶  — ComboBox arrow + ScrollBar buttons (Geometric Shapes)
        //   ▴        — ComboBox open-state up-pointing arrow
        //   ✿        — Window title decorative flower (Dingbats)
        //   •        — PasswordBox bullet character
        BmfcSave.AddCharacters("✓✕▼▴▲◀▶✿•");

        // Forest Glade's visuals build their bodies out of Apos.Shapes-
        // backed RoundedRectangleRuntime instances, which require
        // ShapeRenderer to be initialized. Guard in case the host app
        // already initialized it.
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
        // Nunito body typeface — Regular and Bold faces.
        RegisterEmbeddedFont(FontFamily, "Nunito-Regular.ttf", style: null);
        RegisterEmbeddedFont(FontFamily, "Nunito-Bold.ttf", style: "Bold");

        // Fraunces 144pt Bold Italic — the display face used on the
        // Window title bar. Registered as the family's italic slot since
        // CSS applies font-style: italic alongside font-weight: 700.
        RegisterEmbeddedFont(TitleFontFamily, "Fraunces-BoldItalic.ttf", style: "Italic");

        // Icon font registered under a distinct family name so visual code
        // addresses it explicitly via ForestGladeTheme.IconFontFamily.
        RegisterEmbeddedFont(IconFontFamily, "DejaVuSansMono.ttf", style: null);
    }

    private static void RegisterEmbeddedFont(string family, string fileName, string? style)
    {
        // Resource prefix is derived from the assembly name so this code
        // works in both the MonoGame and KNI variants without forking. The
        // KNI csproj re-embeds the TTFs via <Link> so the resource path
        // inside that assembly is "<assembly-name>.Content.Fonts.<file>".
        Assembly assembly = typeof(ForestGladeTheme).Assembly;
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

        styling.Colors.TextPrimary = ForestGladeColors.Text;
        styling.Colors.TextMuted = ForestGladeColors.Muted;
        styling.Colors.Primary = ForestGladeColors.CanopyDeep;
        styling.Colors.Accent = ForestGladeColors.LeafBright;
    }

    private static void RegisterVisuals()
    {
        FrameworkElement.DefaultFormsTemplates[typeof(Button)] =
            new VisualTemplate((_, c) => new ButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(CheckBox)] =
            new VisualTemplate((_, c) => new CheckBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(RadioButton)] =
            new VisualTemplate((_, c) => new RadioButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ToggleButton)] =
            new VisualTemplate((_, c) => new ToggleButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(TextBox)] =
            new VisualTemplate((_, c) => new TextBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(PasswordBox)] =
            new VisualTemplate((_, c) => new PasswordBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Slider)] =
            new VisualTemplate((_, c) => new SliderVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ScrollBar)] =
            new VisualTemplate((_, c) => new ScrollBarVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ListBox)] =
            new VisualTemplate((_, c) => new ListBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ListBoxItem)] =
            new VisualTemplate((_, c) => new ListBoxItemVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ScrollViewer)] =
            new VisualTemplate((_, c) => new ScrollViewerVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ComboBox)] =
            new VisualTemplate((_, c) => new ComboBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Menu)] =
            new VisualTemplate((_, c) => new MenuVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(MenuItem)] =
            new VisualTemplate((_, c) => new MenuItemVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Window)] =
            new VisualTemplate((_, c) => new WindowVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Splitter)] =
            new VisualTemplate((_, c) => new SplitterVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Tooltip)] =
            new VisualTemplate((_, c) => new TooltipVisual(tryCreateFormsObject: c));

        // Label gets its color from Styling.ActiveStyle.Colors.TextPrimary
        // (set in ConfigureStyling), so the V3 LabelVisual already renders
        // Forest Glade text color without a subclass.
        FrameworkElement.DefaultFormsTemplates[typeof(Label)] =
            new VisualTemplate((_, c) => new Gum.Forms.DefaultVisuals.V3.LabelVisual(tryCreateFormsObject: c));

    }
}
