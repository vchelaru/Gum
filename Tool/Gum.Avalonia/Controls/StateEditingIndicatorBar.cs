using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Gum.Avalonia.Converters;
using Gum.Avalonia.Themes;

namespace Gum.Avalonia.Controls;

/// <summary>
/// The "Editing state X" banner shown above the Alignment and Variables tabs when the selected state
/// is not the default. Its bindings are relative, so the host's DataContext must expose
/// <c>HasStateInformation</c>, <c>StateInformation</c>, <c>StateBackground</c> and
/// <c>StateCategoryColorName</c> (as <c>AlignmentViewModel</c> and the Variables tab's view model do).
/// When the edited state's category has a palette color the banner uses that color's theme variant in
/// place of <c>StateBackground</c>. Twin of the WPF <c>StateEditingIndicatorBar</c>.
/// </summary>
public sealed class StateEditingIndicatorBar : Border
{
    /// <summary>The palette name of the edited state's category color, bound from the host's <c>StateCategoryColorName</c>.</summary>
    public static readonly StyledProperty<string?> CategoryColorNameProperty =
        AvaloniaProperty.Register<StateEditingIndicatorBar, string?>(nameof(CategoryColorName));

    /// <summary>Builds the banner.</summary>
    public StateEditingIndicatorBar()
    {
        Padding = new Thickness(6, 3);
        BindPlainBackground();
        this.Bind(IsVisibleProperty, new Binding("HasStateInformation"));
        this.Bind(CategoryColorNameProperty, new Binding("StateCategoryColorName"));

        // Black text reads on every palette color in both themes, as it does on the yellow.
        TextBlock text = new TextBlock { Foreground = Brushes.Black, TextWrapping = TextWrapping.Wrap };
        text.Bind(TextBlock.TextProperty, new Binding("StateInformation"));
        Child = text;
    }

    /// <summary>The palette name of the category color to draw, or null for the plain <c>StateBackground</c>.</summary>
    public string? CategoryColorName
    {
        get => GetValue(CategoryColorNameProperty);
        set => SetValue(CategoryColorNameProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != CategoryColorNameProperty)
        {
            return;
        }
        if (Gum.Plugins.InternalPlugins.StatePlugin.CategoryColors.IsKnown(CategoryColorName))
        {
            CategoryColorBrushes.Apply(this, BackgroundProperty, CategoryColorName);
        }
        else
        {
            BindPlainBackground();
        }
    }

    private void BindPlainBackground() =>
        this.Bind(BackgroundProperty, new Binding("StateBackground") { Converter = ViewConverters.DrawingColorBrush });
}
