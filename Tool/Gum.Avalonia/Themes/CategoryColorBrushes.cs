using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Gum.Plugins.InternalPlugins.StatePlugin;

namespace Gum.Avalonia.Themes;

/// <summary>
/// Paints controls with a category palette color. A category stores only the color's name; the brush
/// for that name comes from the theme's <see cref="CategoryColors.ResourceKey"/> resource, so it
/// follows a light/dark switch. The name is never hard-coded to a hex value in a view.
/// </summary>
public static class CategoryColorBrushes
{
    /// <summary>
    /// Binds <paramref name="property"/> of <paramref name="target"/> to the theme brush of palette color
    /// <paramref name="colorName"/>, or to transparent when it is null or not in the palette.
    /// </summary>
    public static void Apply(Control target, AvaloniaProperty<IBrush?> property, string? colorName)
    {
        if (CategoryColors.IsKnown(colorName))
        {
            target.Bind(property, target.GetResourceObservable(CategoryColors.ResourceKey(colorName!)));
        }
        else
        {
            target.SetValue(property, Brushes.Transparent);
        }
    }

    /// <summary>
    /// Keeps <paramref name="property"/> of <paramref name="target"/> painted with the palette color that
    /// <paramref name="read"/> returns for <paramref name="host"/>'s DataContext, refreshing when the
    /// DataContext changes or raises <see cref="INotifyPropertyChanged.PropertyChanged"/> for
    /// <paramref name="notifyPropertyName"/>.
    /// </summary>
    public static void Follow(Control host, Control target, AvaloniaProperty<IBrush?> property, string notifyPropertyName, Func<object?, string?> read) =>
        Follow(host, notifyPropertyName, read, colorName => Apply(target, property, colorName));

    /// <summary>
    /// Calls <paramref name="apply"/> with the palette color name that <paramref name="read"/> returns for
    /// <paramref name="host"/>'s DataContext, now and whenever the DataContext changes or raises
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/> for <paramref name="notifyPropertyName"/>.
    /// </summary>
    public static void Follow(Control host, string notifyPropertyName, Func<object?, string?> read, Action<string?> apply)
    {
        INotifyPropertyChanged? observed = null;

        void Refresh() => apply(read(host.DataContext));

        void HandleChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == notifyPropertyName)
            {
                Refresh();
            }
        }

        void Observe()
        {
            if (observed != null)
            {
                observed.PropertyChanged -= HandleChanged;
            }
            observed = host.DataContext as INotifyPropertyChanged;
            if (observed != null)
            {
                observed.PropertyChanged += HandleChanged;
            }
            Refresh();
        }

        host.DataContextChanged += (_, _) => Observe();
        host.DetachedFromVisualTree += (_, _) =>
        {
            if (observed != null)
            {
                observed.PropertyChanged -= HandleChanged;
                observed = null;
            }
        };
        host.AttachedToVisualTree += (_, _) =>
        {
            if (observed == null)
            {
                Observe();
            }
        };
        Observe();
    }
}
