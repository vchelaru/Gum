using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Gum.Avalonia.Themes;

/// <summary>
/// Loads the FRB palette into the Avalonia head from the WPF tool's own resource dictionaries
/// (<c>Gum/Themes/Frb.Brushes.{Light,Dark}.xaml</c>, <c>Frb.Accents.xaml</c>, <c>Frb.Brushes.xaml</c>,
/// <c>Palette.xaml</c>), embedded unchanged, so there is one palette for both heads. The light and
/// dark dictionaries become <see cref="ThemeVariant.Light"/> / <see cref="ThemeVariant.Dark"/> theme
/// dictionaries, so every <c>Frb.*</c> key follows the requested theme variant.
/// </summary>
/// <remarks>
/// Only what the palette files contain is understood: <c>Color</c> elements and
/// <c>SolidColorBrush</c> elements whose color is a literal or a <c>StaticResource</c> /
/// <c>DynamicResource</c> reference to a color, with an optional <c>Opacity</c>. Anything else with
/// an <c>x:Key</c> is skipped.
/// </remarks>
public static class FrbThemeResources
{
    private const string ResourcePrefix = "Gum.Avalonia.Themes.";
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>The embedded dictionary files, by their WPF file name.</summary>
    public static IReadOnlyList<string> FileNames { get; } = new[]
    {
        "Palette.xaml", "Frb.Accents.xaml", "Frb.Brushes.xaml", "Frb.Brushes.Light.xaml", "Frb.Brushes.Dark.xaml",
    };

    /// <summary>
    /// Adds the palette to <paramref name="resources"/>: the grays, accent colors and accent brushes
    /// at the top level, and the per-variant colors and brushes as theme dictionaries.
    /// </summary>
    public static void Install(IResourceDictionary resources)
    {
        Dictionary<string, Color> palette = new Dictionary<string, Color>();
        AddTo(resources, Read("Palette.xaml"), palette);
        AddTo(resources, Read("Frb.Accents.xaml"), palette);
        AddTo(resources, Read("Frb.Brushes.xaml"), palette);

        resources.ThemeDictionaries[ThemeVariant.Light] = LoadVariant("Frb.Brushes.Light.xaml", palette);
        resources.ThemeDictionaries[ThemeVariant.Dark] = LoadVariant("Frb.Brushes.Dark.xaml", palette);

        AddControlMetrics(resources);
        ApplyControlAliases(resources);
    }

    /// <summary>
    /// Points the Fluent theme's control resources at the palette brushes, so text boxes, check
    /// boxes, combo boxes and tree items take the WPF head's colors (<c>Frb.Styles.Defaults.xaml</c>).
    /// Each alias shares the palette's brush: root-level brushes (the Primary ones, which the accent
    /// replaces) go in the root, and per-theme brushes into each theme dictionary. Call again after
    /// replacing a root brush.
    /// </summary>
    public static void ApplyControlAliases(IResourceDictionary resources)
    {
        foreach ((string alias, string source) in ControlBrushAliases())
        {
            if (source == TransparentSource)
            {
                resources[alias] = Brushes.Transparent;
            }
            else if (resources.TryGetValue(source, out object? rootBrush))
            {
                resources[alias] = rootBrush;
            }
            else
            {
                foreach (IThemeVariantProvider variant in resources.ThemeDictionaries.Values)
                {
                    if (variant is IResourceDictionary dictionary && dictionary.TryGetValue(source, out object? brush))
                    {
                        dictionary[alias] = brush;
                    }
                }
            }
        }
    }

    // Sizes from the WPF control styles: compact fields and tree rows, 2px corners, 1px borders.
    private static void AddControlMetrics(IResourceDictionary resources)
    {
        resources["ControlCornerRadius"] = new CornerRadius(2);
        resources["TextControlThemeMinHeight"] = 22d;
        resources["TextControlThemePadding"] = new Thickness(4, 1, 4, 1);
        resources["TextControlBorderThemeThickness"] = new Thickness(1);
        resources["TextControlBorderThemeThicknessFocused"] = new Thickness(1);
        resources["ComboBoxMinHeight"] = 22d;
        resources["ComboBoxPadding"] = new Thickness(4, 1, 0, 1);
        resources["TreeViewItemMinHeight"] = 20d;
        resources["TreeViewItemBorderThemeThickness"] = new Thickness(1);
    }

    private const string TransparentSource = "#Transparent";

    private static IEnumerable<(string Alias, string Source)> ControlBrushAliases()
    {
        // TextBox (SimpleTextBox): the field background with a secondary border that turns Primary on
        // hover and focus; the background darkens while focused.
        foreach (string state in new[] { "", "PointerOver", "Focused" })
        {
            yield return ("TextControlForeground" + state, "Frb.Brushes.Foreground");
        }
        yield return ("TextControlForegroundDisabled", "Frb.Brushes.Foreground.Disabled");
        foreach (string state in new[] { "", "PointerOver", "Disabled" })
        {
            yield return ("TextControlBackground" + state, "Frb.Brushes.Field.Background");
        }
        yield return ("TextControlBackgroundFocused", "Frb.Brushes.Background");
        yield return ("TextControlBorderBrush", "Frb.Brushes.Border.Secondary");
        yield return ("TextControlBorderBrushDisabled", "Frb.Brushes.Border.Secondary");
        yield return ("TextControlBorderBrushPointerOver", "Frb.Brushes.Primary");
        yield return ("TextControlBorderBrushFocused", "Frb.Brushes.Primary");
        foreach (string state in new[] { "", "PointerOver", "Focused", "Disabled" })
        {
            yield return ("TextControlPlaceholderForeground" + state, "Frb.Brushes.Foreground.Subtle");
        }

        // CheckBox: the box and check mark drawn in the text color on no fill; Primary on hover.
        foreach (string value in new[] { "Unchecked", "Checked", "Indeterminate" })
        {
            foreach (string state in new[] { "", "PointerOver", "Pressed", "Disabled" })
            {
                bool hot = state is "PointerOver" or "Pressed";
                string text = state == "Disabled" ? "Frb.Brushes.Foreground.Disabled" : "Frb.Brushes.Foreground";
                yield return ($"CheckBoxForeground{value}{state}", text);
                yield return ($"CheckBoxBackground{value}{state}", TransparentSource);
                yield return ($"CheckBoxBorderBrush{value}{state}", TransparentSource);
                yield return ($"CheckBoxCheckBackgroundFill{value}{state}", hot ? "Frb.Brushes.Primary.Transparent" : TransparentSource);
                yield return ($"CheckBoxCheckBackgroundStroke{value}{state}", hot ? "Frb.Brushes.Primary" : text);
                yield return ($"CheckBoxCheckGlyphForeground{value}{state}", text);
            }
        }

        // ComboBox: like the text box, with the drop-down on Surface01.
        foreach (string key in new[] { "ComboBoxBackground", "ComboBoxBackgroundPointerOver", "ComboBoxBackgroundPressed", "ComboBoxBackgroundDisabled", "ComboBoxBackgroundUnfocused" })
        {
            yield return (key, "Frb.Brushes.Field.Background");
        }
        foreach (string key in new[] { "ComboBoxBorderBrush", "ComboBoxBorderBrushDisabled", "ComboBoxBackgroundBorderBrushUnfocused" })
        {
            yield return (key, "Frb.Brushes.Border.Secondary");
        }
        foreach (string key in new[] { "ComboBoxBorderBrushPointerOver", "ComboBoxBorderBrushPressed", "ComboBoxBackgroundBorderBrushFocused" })
        {
            yield return (key, "Frb.Brushes.Primary");
        }
        foreach (string key in new[] { "ComboBoxForeground", "ComboBoxForegroundFocused", "ComboBoxForegroundFocusedPressed", "ComboBoxDropDownGlyphForeground", "ComboBoxDropDownGlyphForegroundFocused", "ComboBoxDropDownGlyphForegroundFocusedPressed" })
        {
            yield return (key, "Frb.Brushes.Foreground");
        }
        yield return ("ComboBoxForegroundDisabled", "Frb.Brushes.Foreground.Disabled");
        yield return ("ComboBoxDropDownGlyphForegroundDisabled", "Frb.Brushes.Foreground.Disabled");
        yield return ("ComboBoxPlaceHolderForeground", "Frb.Brushes.Foreground.Subtle");
        yield return ("ComboBoxPlaceHolderForegroundFocusedPressed", "Frb.Brushes.Foreground.Subtle");
        yield return ("ComboBoxDropDownBackground", "Frb.Surface01");
        yield return ("ComboBoxDropDownBorderBrush", "Frb.Brushes.Border");

        // TreeViewItem: Surface.Fill on hover, and the selection as a Primary outline, not a fill.
        foreach (string state in new[] { "", "PointerOver", "Pressed", "Selected", "SelectedPointerOver", "SelectedPressed" })
        {
            yield return ("TreeViewItemForeground" + state, "Frb.Brushes.Foreground");
        }
        yield return ("TreeViewItemForegroundDisabled", "Frb.Brushes.Foreground.Disabled");
        yield return ("TreeViewItemForegroundSelectedDisabled", "Frb.Brushes.Foreground.Disabled");
        foreach (string state in new[] { "", "Disabled", "Selected", "SelectedDisabled" })
        {
            yield return ("TreeViewItemBackground" + state, TransparentSource);
        }
        foreach (string state in new[] { "PointerOver", "Pressed", "SelectedPointerOver", "SelectedPressed" })
        {
            yield return ("TreeViewItemBackground" + state, "Frb.Brushes.Surface.Fill");
        }
        foreach (string state in new[] { "", "PointerOver", "Pressed", "Disabled" })
        {
            yield return ("TreeViewItemBorderBrush" + state, TransparentSource);
        }
        foreach (string state in new[] { "Selected", "SelectedPointerOver", "SelectedPressed", "SelectedDisabled" })
        {
            yield return ("TreeViewItemBorderBrush" + state, "Frb.Brushes.Primary");
        }
    }

    /// <summary>Builds the resource dictionary for one variant file, resolving colors against it first, then <paramref name="shared"/>.</summary>
    public static ResourceDictionary LoadVariant(string fileName, IReadOnlyDictionary<string, Color> shared)
    {
        ResourceDictionary dictionary = new ResourceDictionary();
        Dictionary<string, Color> colors = new Dictionary<string, Color>(shared);
        AddTo(dictionary, Read(fileName), colors);
        return dictionary;
    }

    private static XDocument Read(string fileName)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException($"The theme dictionary {fileName} is not embedded in the head.");
        return XDocument.Load(stream);
    }

    // Colors are added in document order, which is the order the files define them in (a brush
    // always comes after the color it references).
    private static void AddTo(IResourceDictionary target, XDocument document, Dictionary<string, Color> colors)
    {
        foreach (XElement element in document.Descendants())
        {
            if (element.Attribute(XamlNamespace + "Key")?.Value is not { } key)
            {
                continue;
            }

            switch (element.Name.LocalName)
            {
                case "Color":
                    Color color = Color.Parse(element.Value.Trim());
                    colors[key] = color;
                    target[key] = color;
                    break;
                case "SolidColorBrush":
                    if (ResolveColor(element.Attribute("Color")?.Value, colors) is { } brushColor)
                    {
                        double opacity = element.Attribute("Opacity")?.Value is { } opacityText
                            ? double.Parse(opacityText, CultureInfo.InvariantCulture)
                            : 1;
                        target[key] = new SolidColorBrush(brushColor, opacity);
                    }
                    break;
            }
        }
    }

    private static Color? ResolveColor(string? value, IReadOnlyDictionary<string, Color> colors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        if (value.StartsWith('{'))
        {
            // {StaticResource Key} or {DynamicResource Key}
            string referencedKey = value.Trim('{', '}').Split(' ', StringSplitOptions.RemoveEmptyEntries).Last();
            return colors.TryGetValue(referencedKey, out Color referenced) ? referenced : null;
        }

        return Color.Parse(value);
    }
}
