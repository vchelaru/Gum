using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
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
