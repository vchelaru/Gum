using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using Avalonia.Controls.Shapes;

namespace Gum.Avalonia.Themes;

/// <summary>
/// Styles that give the head's controls the WPF tool's look, drawn from the same palette brushes.
/// The control colors themselves come from <see cref="FrbThemeResources"/>, which points the Fluent
/// theme's resources at the palette; these styles cover what the WPF head restyles beyond color:
/// <list type="bullet">
/// <item>The main panel's tabs (<c>Gum/Themes/MainPanelControl.Styles.xaml</c>): headers at the body
/// font size, so they scale with the app zoom; subtle text; and the selected tab on Surface01 with
/// a 2px Primary rule along its top.</item>
/// <item>Property grid categories (<c>Frb.Styles.Defaults.xaml</c>): Contrast01 behind a category
/// with no header color, and the name in ThemeContrast.</item>
/// <item>The property grid's option buttons: a field-colored group whose chosen button has a
/// Primary outline rather than a fill.</item>
/// <item>Check boxes: the WPF template, a small outlined box instead of Fluent's 20px box on a 32px row.</item>
/// <item><see cref="FlatButtonClass"/>: the WPF head's borderless tool buttons.</item>
/// <item><see cref="IconButtonClass"/>: the WPF <c>IconButton</c>, a flat outlined button.</item>
/// </list>
/// </summary>
public static class GumChromeStyles
{
    /// <summary>The class the main panel puts on its tab regions.</summary>
    public const string MainTabsClass = "gumMainTabs";

    /// <summary>The class for a flat, outlined button, the WPF head's <c>IconButton</c> style.</summary>
    public const string IconButtonClass = "gumIconButton";

    /// <summary>The class for a borderless button that shows a fill only on hover, the WPF head's tool buttons.</summary>
    public const string FlatButtonClass = "gumFlatButton";

    /// <summary>A converter that multiplies a font size, for text and icons sized off the app's base size.</summary>
    public static IValueConverter ScaleFontSize(double factor) => new FuncValueConverter<double, double>(size => size * factor);

    /// <summary>Creates the styles. Add them after the Fluent theme so they take precedence.</summary>
    public static Styles Create() => new Styles
    {
        new Style(selector => selector.Is<DataUiGrid>())
        {
            Setters =
            {
                new Setter(DataUiGrid.CategoryHeaderBackgroundProperty, Resource("Frb.Brushes.Contrast01")),
                new Setter(DataUiGrid.CategoryHeaderForegroundProperty, Resource("Frb.Brushes.ThemeContrast")),
                // As the WPF head's grids: no green or gray tint on default and mixed values.
                new Setter(DataUiGrid.OverridesIsDefaultStylingProperty, true),
            },
        },

        new Style(MainTabItem)
        {
            Setters =
            {
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<TabItem>(BuildMainTabItem)),
                new Setter(TemplatedControl.FontSizeProperty, new Binding(nameof(TabControl.FontSize))
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(TabControl) },
                }),
                new Setter(TemplatedControl.FontWeightProperty, FontWeight.Normal),
                new Setter(TemplatedControl.ForegroundProperty, Resource("Frb.Brushes.Foreground.Subtle")),
                new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
                new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent),
                new Setter(TemplatedControl.PaddingProperty, new Thickness(6, 1, 6, 3)),
                new Setter(Layoutable.MinHeightProperty, 0d),
                new Setter(Layoutable.MarginProperty, new Thickness(0)),
            },
        },
        new Style(selector => MainTabItem(selector).Class(":pointerover"))
        {
            Setters =
            {
                new Setter(TemplatedControl.ForegroundProperty, Resource("Frb.Brushes.Foreground")),
                new Setter(TemplatedControl.BackgroundProperty, Resource("Frb.Surface01")),
            },
        },
        new Style(selector => MainTabItem(selector).Class(":selected"))
        {
            Setters =
            {
                new Setter(TemplatedControl.ForegroundProperty, Resource("Frb.Brushes.ThemeContrast")),
                new Setter(TemplatedControl.FontWeightProperty, FontWeight.Medium),
                new Setter(TemplatedControl.BackgroundProperty, Resource("Frb.Surface01")),
                new Setter(TemplatedControl.BorderBrushProperty, Resource("Frb.Brushes.Primary")),
            },
        },

        // Check boxes use the WPF head's own template (Frb.Styles.Defaults.xaml); Fluent's writes a 20px
        // box on a 32px row into the template itself, beyond the reach of a style.
        new Style(selector => selector.OfType<CheckBox>())
        {
            Setters =
            {
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<CheckBox>(BuildCheckBox)),
                new Setter(TemplatedControl.ForegroundProperty, Resource("Frb.Brushes.Foreground")),
                new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
                new Setter(TemplatedControl.PaddingProperty, new Thickness(0)),
                new Setter(Layoutable.MinHeightProperty, 0d),
            },
        },
        new Style(selector => selector.OfType<CheckBox>().Class(":disabled"))
        {
            Setters = { new Setter(TemplatedControl.ForegroundProperty, Resource("Frb.Brushes.Foreground.Disabled")) },
        },
        new Style(selector => selector.OfType<CheckBox>().Template().OfType<Border>().Name("PART_Box"))
        {
            Setters =
            {
                new Setter(Border.BackgroundProperty, Brushes.Transparent),
                new Setter(Border.BorderBrushProperty, new Binding(nameof(TemplatedControl.Foreground))
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                }),
            },
        },
        new Style(selector => selector.OfType<CheckBox>().Class(":pointerover").Template().OfType<Border>().Name("PART_Box"))
        {
            Setters =
            {
                new Setter(Border.BackgroundProperty, Resource("Frb.Brushes.Primary.Transparent")),
                new Setter(Border.BorderBrushProperty, Resource("Frb.Brushes.Primary")),
            },
        },

        new Style(selector => selector.OfType<Border>().Class(ToggleButtonOptionDisplay.OptionGroupClass))
        {
            Setters =
            {
                new Setter(Border.BackgroundProperty, Resource("Frb.Brushes.Field.Background")),
                new Setter(Border.BorderBrushProperty, Resource("Frb.Brushes.Border.Secondary")),
                new Setter(Border.BorderThicknessProperty, new Thickness(1)),
                new Setter(Border.CornerRadiusProperty, new CornerRadius(2)),
                new Setter(Decorator.PaddingProperty, new Thickness(1)),
            },
        },
        new Style(selector => selector.OfType<ToggleButton>().Class(ToggleButtonOptionDisplay.OptionClass))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
                new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)),
                new Setter(TemplatedControl.ForegroundProperty, Resource("Frb.Brushes.Foreground")),
            },
        },
        ButtonPart<ToggleButton>(ToggleButtonOptionDisplay.OptionClass, new[] { ":pointerover" }, Resource("Frb.Brushes.Surface.Fill"), Resource("Frb.Brushes.Border.Secondary")),
        ButtonPart<ToggleButton>(ToggleButtonOptionDisplay.OptionClass, new[] { ":pressed" }, Resource("Frb.Brushes.Surface.Fill"), Resource("Frb.Brushes.Border.Secondary")),
        ButtonPart<ToggleButton>(ToggleButtonOptionDisplay.OptionClass, new[] { ":checked" }, Brushes.Transparent, Resource("Frb.Brushes.Primary")),
        ButtonPart<ToggleButton>(ToggleButtonOptionDisplay.OptionClass, new[] { ":checked", ":pointerover" }, Resource("Frb.Brushes.Surface.Fill"), Resource("Frb.Brushes.Primary")),

        new Style(selector => selector.OfType<Button>().Class(IconButtonClass))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
                new Setter(TemplatedControl.BorderBrushProperty, Resource("Frb.Brushes.Border.Secondary")),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)),
                new Setter(TemplatedControl.ForegroundProperty, Resource("Frb.Brushes.Foreground")),
                new Setter(TemplatedControl.PaddingProperty, new Thickness(4, 2, 4, 3)),
            },
        },
        ButtonPart<Button>(IconButtonClass, new[] { ":pointerover" }, Resource("Frb.Brushes.Surface.Fill"), Resource("Frb.Brushes.Border.Secondary")),
        ButtonPart<Button>(IconButtonClass, new[] { ":pressed" }, Resource("Frb.Brushes.Surface.Fill"), Resource("Frb.Brushes.Primary")),

        new Style(selector => selector.OfType<Button>().Class(FlatButtonClass))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
                new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)),
                new Setter(TemplatedControl.ForegroundProperty, Resource("Frb.Brushes.Foreground")),
            },
        },
        ButtonPart<Button>(FlatButtonClass, new[] { ":pointerover" }, Resource("Frb.Brushes.Surface.Fill"), Brushes.Transparent),
        ButtonPart<Button>(FlatButtonClass, new[] { ":pressed" }, Resource("Frb.Brushes.Surface.Fill"), Brushes.Transparent),
    };

    // The tab control's own items only, so a plugin view's nested tab control keeps its look.
    private static Selector MainTabItem(Selector? selector) =>
        selector.OfType<TabControl>().Class(MainTabsClass).Child().OfType<TabItem>();

    // A button's presenter colors in the given states. The Fluent theme colors the same template
    // part in its own state styles, which these outrank.
    private static Style ButtonPart<TButton>(string buttonClass, string[] states, object background, object borderBrush)
        where TButton : Control =>
        new Style(selector =>
        {
            Selector button = selector.OfType<TButton>().Class(buttonClass);
            foreach (string state in states)
            {
                button = button.Class(state);
            }
            return button.Template().OfType<ContentPresenter>().Name("PART_ContentPresenter");
        })
        {
            Setters =
            {
                new Setter(ContentPresenter.BackgroundProperty, background),
                new Setter(ContentPresenter.BorderBrushProperty, borderBrush),
                new Setter(ContentPresenter.ForegroundProperty, Resource("Frb.Brushes.Foreground")),
            },
        };

    private static Control BuildMainTabItem(TabItem item, INameScope scope)
    {
        ContentPresenter header = new ContentPresenter
        {
            Name = "PART_ContentPresenter",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            [!ContentPresenter.ContentProperty] = item[!HeaderedContentControl.HeaderProperty],
            [!ContentPresenter.ContentTemplateProperty] = item[!HeaderedContentControl.HeaderTemplateProperty],
        };
        header.RegisterInNameScope(scope);

        return new Border
        {
            BorderThickness = new Thickness(0, 2, 0, 0),
            [!Border.BackgroundProperty] = item[!TemplatedControl.BackgroundProperty],
            [!Border.BorderBrushProperty] = item[!TemplatedControl.BorderBrushProperty],
            [!Decorator.PaddingProperty] = item[!TemplatedControl.PaddingProperty],
            Child = header,
        };
    }

    private static readonly Geometry CheckMark = Geometry.Parse("M 1 4 L 3 7 L 8 1");
    private static readonly IValueConverter IsTrue = new FuncValueConverter<bool?, bool>(value => value == true);
    private static readonly IValueConverter IsNull = new FuncValueConverter<bool?, bool>(value => value == null);

    // A box the size of the text, outlined in the text color, with a stroked check mark (or a small
    // square when mixed), and the content to its right.
    private static Control BuildCheckBox(CheckBox checkBox, INameScope scope)
    {
        Border box = new Border
        {
            Name = "PART_Box",
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(1),
            VerticalAlignment = VerticalAlignment.Center,
            [!Layoutable.WidthProperty] = checkBox[!TemplatedControl.FontSizeProperty],
            [!Layoutable.HeightProperty] = checkBox[!TemplatedControl.FontSizeProperty],
        };
        box.RegisterInNameScope(scope);

        Path check = new Path
        {
            Data = CheckMark,
            StrokeThickness = 2,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            [!Shape.StrokeProperty] = checkBox[!TemplatedControl.ForegroundProperty],
        };
        check.Bind(Visual.IsVisibleProperty, new Binding(nameof(CheckBox.IsChecked)) { Source = checkBox, Converter = IsTrue });

        Border mixed = new Border
        {
            Width = 6,
            Height = 6,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            [!Border.BackgroundProperty] = checkBox[!TemplatedControl.ForegroundProperty],
        };
        mixed.Bind(Visual.IsVisibleProperty, new Binding(nameof(CheckBox.IsChecked)) { Source = checkBox, Converter = IsNull });

        ContentPresenter content = new ContentPresenter
        {
            Name = "PART_ContentPresenter",
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            [!ContentPresenter.ContentProperty] = checkBox[!ContentControl.ContentProperty],
            [!ContentPresenter.ContentTemplateProperty] = checkBox[!ContentControl.ContentTemplateProperty],
        };
        content.RegisterInNameScope(scope);
        Grid.SetColumn(content, 1);

        Grid root = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            [!Panel.BackgroundProperty] = checkBox[!TemplatedControl.BackgroundProperty],
        };
        root.Children.Add(box);
        root.Children.Add(check);
        root.Children.Add(mixed);
        root.Children.Add(content);
        return root;
    }

    private static IBinding Resource(string key) => new DynamicResourceExtension(key);
}
