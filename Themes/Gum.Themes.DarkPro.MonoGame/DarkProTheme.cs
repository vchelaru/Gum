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

namespace Gum.Themes.DarkPro;

/// <summary>
/// Entry point for the Dark Pro theme. Call <see cref="Apply"/> once after
/// initializing Gum to register the bundled DM Mono fonts, configure the
/// shared <see cref="Styling.ActiveStyle"/> tokens, and install the Dark Pro
/// visuals as the default templates for Forms controls.
/// </summary>
public static class DarkProTheme
{
    /// <summary>
    /// Family name the bundled DM Mono TTFs are registered under. Use this as
    /// the <c>Font</c> property value on any TextRuntime to get the theme font.
    /// </summary>
    public const string FontFamily = "DM Mono";

    /// <summary>
    /// Family name the bundled icon font (DejaVu Sans Mono) is registered under.
    /// Use this for glyphs DM Mono doesn't cover - check marks, close buttons,
    /// combo/scrollbar arrows, etc. (Dingbats and Geometric Shapes blocks).
    /// </summary>
    public const string IconFontFamily = "DM Mono Icons";

    /// <summary>
    /// Default text size used by the theme. Matches the Dark Pro mockup's
    /// <c>--fs</c> token (14px).
    /// </summary>
    public const int FontSize = 14;

    /// <summary>
    /// Applies the Dark Pro theme: wires KernSmith as the in-memory font
    /// creator, registers the bundled DM Mono TTFs (Regular / Medium /
    /// Italic / MediumItalic), populates <see cref="Styling.ActiveStyle"/>
    /// with the Dark Pro color and text tokens, and registers the theme's
    /// visuals as the default templates for Forms controls.
    /// </summary>
    /// <param name="graphicsDevice">The active <see cref="GraphicsDevice"/>;
    /// required by KernSmith to rasterize fonts into textures.</param>
    public static void Apply(GraphicsDevice graphicsDevice)
    {
        CustomSetPropertyOnRenderable.InMemoryFontCreator =
            new KernSmithFontCreator(graphicsDevice);

        // Pre-register glyphs that visuals render as Text rather than as
        // sprite-sheet icons. KernSmith bakes only the characters it has
        // been told about, so anything outside ASCII has to be declared
        // before the first font generation. These all live in the bundled
        // icon font (DM Mono Icons / DejaVu Sans Mono) since DM Mono itself
        // doesn't cover Dingbats or Geometric Shapes.
        BmfcSave.AddCharacters("✓✕▾▴▲▼◀▶");

        // Dark Pro's visuals build their bodies out of Apos.Shapes-backed
        // RoundedRectangleRuntime instances, which require ShapeRenderer to
        // be initialized. Initialize here so consumers don't need to know
        // the theme uses shapes internally. Guard for the case where the
        // host app already initialized it (e.g. mixing themes or using
        // shape runtimes elsewhere).
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
        // DM Mono's true 700-weight Bold variant is not shipped; Medium (500)
        // maps to Gum's IsBold slot because the Dark Pro design uses 500 as
        // the emphasis weight. Consumers can override by re-registering.
        RegisterEmbeddedFont(FontFamily, "DMMono-Regular.ttf", style: null);
        RegisterEmbeddedFont(FontFamily, "DMMono-Medium.ttf", style: "Bold");
        RegisterEmbeddedFont(FontFamily, "DMMono-Italic.ttf", style: "Italic");
        RegisterEmbeddedFont(FontFamily, "DMMono-MediumItalic.ttf", style: "BoldItalic");

        // Icon font (DejaVu Sans Mono) registered under a distinct family name
        // so the visual code addresses it explicitly via DarkProTheme.IconFontFamily.
        RegisterEmbeddedFont(IconFontFamily, "DejaVuSansMono.ttf", style: null);
    }

    private static void RegisterEmbeddedFont(string family, string fileName, string? style)
    {
        Assembly assembly = typeof(DarkProTheme).Assembly;
        string resourceName = $"Gum.Themes.DarkPro.MonoGame.Content.Fonts.{fileName}";

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException(
                $"Embedded font resource '{resourceName}' not found. " +
                $"Verify the .ttf is included as <EmbeddedResource> in Gum.Themes.DarkPro.MonoGame.csproj.");
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

        // Dark Pro tokens (see gum-styles.css :root for the source palette).
        // The Styling.Colors slots don't map 1:1 with the Dark Pro tokens, so
        // we set what overlaps (TextPrimary, TextMuted, Primary, Accent) and
        // the visuals carry the rest of the palette inline. Promoting the
        // full Dark Pro palette into a typed Styling extension is a planned
        // follow-up once more visuals land.
        styling.Colors.TextPrimary = DarkProColors.Text;
        styling.Colors.TextMuted = DarkProColors.Muted;
        styling.Colors.Primary = DarkProColors.Surface1;
        styling.Colors.Accent = DarkProColors.Accent;
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
    }
}
