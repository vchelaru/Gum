using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Gum.Avalonia.Converters;

namespace Gum.Avalonia.Controls;

/// <summary>
/// The "Editing state X" banner shown above the Alignment and Variables tabs when the selected state
/// is not the default. Its bindings are relative, so the host's DataContext must expose
/// <c>HasStateInformation</c>, <c>StateInformation</c> and <c>StateBackground</c> (as
/// <c>AlignmentViewModel</c> and the Variables tab's view model do). Twin of the WPF
/// <c>StateEditingIndicatorBar</c>.
/// </summary>
public sealed class StateEditingIndicatorBar : Border
{
    /// <summary>Builds the banner.</summary>
    public StateEditingIndicatorBar()
    {
        Padding = new Thickness(6, 3);
        this.Bind(BackgroundProperty, new Binding("StateBackground") { Converter = ViewConverters.DrawingColorBrush });
        this.Bind(IsVisibleProperty, new Binding("HasStateInformation"));

        TextBlock text = new TextBlock { Foreground = Brushes.Black, TextWrapping = TextWrapping.Wrap };
        text.Bind(TextBlock.TextProperty, new Binding("StateInformation"));
        Child = text;
    }
}
