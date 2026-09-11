using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaDataUi;

namespace Gum.Avalonia.Themes;

/// <summary>
/// Styles that give the head's chrome the WPF tool's look, drawn from the same palette brushes. The
/// main panel's tabs follow <c>Gum/Themes/MainPanelControl.Styles.xaml</c>: headers at the body font
/// size (so they scale with the app zoom), subtle text, and the selected tab on Surface01 with a
/// 2px Primary rule along its top. Property grid categories get the WPF header strip brushes
/// (<c>Frb.Styles.Defaults.xaml</c>): Contrast01 behind a category with no header color, and the
/// name in ThemeContrast.
/// </summary>
public static class GumChromeStyles
{
    /// <summary>The class the main panel puts on its tab regions.</summary>
    public const string MainTabsClass = "gumMainTabs";

    /// <summary>Creates the styles. Add them after the Fluent theme so they take precedence.</summary>
    public static Styles Create() => new Styles
    {
        new Style(selector => selector.Is<DataUiGrid>())
        {
            Setters =
            {
                new Setter(DataUiGrid.CategoryHeaderBackgroundProperty, Resource("Frb.Brushes.Contrast01")),
                new Setter(DataUiGrid.CategoryHeaderForegroundProperty, Resource("Frb.Brushes.ThemeContrast")),
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
    };

    // The tab control's own items only, so a plugin view's nested tab control keeps its look.
    private static Selector MainTabItem(Selector? selector) =>
        selector.OfType<TabControl>().Class(MainTabsClass).Child().OfType<TabItem>();

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

    private static IBinding Resource(string key) => new DynamicResourceExtension(key);
}
