using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Data;
using Avalonia.Media;

namespace Gum.Avalonia.Plugins.VariableGrid;

/// <summary>
/// The tool's <c>GumIcon</c> geometries (generated into <c>Gum/Themes/GumIcons.xaml</c>, embedded
/// here) as Avalonia icons, so the Variables tab's toggles show the same glyphs as the WPF head.
/// Each icon is a 32x32 path, with an optional ".Secondary" path drawn faintly.
/// </summary>
public class GumIconRegistry
{
    private const string ResourceName = "Gum.Avalonia.GumIcons.xaml";
    private readonly Dictionary<string, string> _figuresByKey;

    /// <summary>Loads the embedded geometries.</summary>
    public GumIconRegistry()
    {
        _figuresByKey = new Dictionary<string, string>();

        using Stream? stream = typeof(GumIconRegistry).Assembly.GetManifestResourceStream(ResourceName);
        if (stream == null)
        {
            return;
        }

        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        foreach (XElement element in XDocument.Load(stream).Root!.Elements())
        {
            string? key = (string?)element.Attribute(xaml + "Key");
            string? figures = (string?)element.Attribute("Figures");
            if (element.Name.LocalName == "PathGeometry" && key != null && figures != null)
            {
                _figuresByKey[key] = figures;
            }
        }
    }

    /// <summary>Whether an icon named <paramref name="key"/> exists.</summary>
    public bool Contains(string key) => _figuresByKey.ContainsKey(key);

    /// <summary>
    /// The icon named <paramref name="key"/>, <paramref name="size"/> units square, filled with the
    /// foreground of the control it sits in; null when there is no such icon.
    /// </summary>
    public Control? CreateIcon(string key, double size = 20)
    {
        if (!_figuresByKey.TryGetValue(key, out string? figures))
        {
            return null;
        }

        global::Avalonia.Controls.Canvas canvas = new global::Avalonia.Controls.Canvas { Width = 32, Height = 32 };
        canvas.Children.Add(CreatePath(figures, opacity: 1));
        if (_figuresByKey.TryGetValue(key + ".Secondary", out string? secondary))
        {
            canvas.Children.Add(CreatePath(secondary, opacity: 0.32));
        }

        return new Viewbox { Width = size, Height = size, Child = canvas, IsHitTestVisible = false };
    }

    private static Path CreatePath(string figures, double opacity)
    {
        Path path = new Path { Data = Geometry.Parse(figures), Opacity = opacity };
        path.Bind(Shape.FillProperty, new Binding(nameof(TemplatedControl.Foreground))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(TemplatedControl) },
        });
        return path;
    }
}
