using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Styling;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace AvaloniaDataUi.Controls;

/// <summary>
/// A read-only, label-less box of code file paths, one per line, in a monospace font: a line ending in
/// <c>.Generated.cs</c> is drawn in the generated color and any other line in the custom color, so the
/// two halves of a partial class can be told apart at a glance. Shown for <see cref="StandardDisplayers.CodeFilePaths"/>.
/// A line with no path (a placeholder) is drawn muted.
/// </summary>
public class CodeFilePathsDisplay : DataUiDisplayBase
{
    private readonly Border _box;
    private readonly TextBlock _lines;
    private readonly TextBlock _hint;
    private string _text = string.Empty;

    /// <summary>Builds the displayer.</summary>
    public CodeFilePathsDisplay()
    {
        _lines = new TextBlock
        {
            FontFamily = new FontFamily("Cascadia Mono,Consolas,Menlo,DejaVu Sans Mono,monospace"),
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        _lines.Bind(TextBlock.FontSizeProperty, _lines.GetResourceObservable("Frb.FontSize.Caption"));
        _box = new Border
        {
            Child = _lines,
            Padding = new Thickness(10, 7),
            CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.Parse("#40808080")),
            Background = new SolidColorBrush(Color.Parse("#20000000")),
            Margin = new Thickness(0, 0, 4, 0),
        };
        _hint = CreateHintTextBlock();

        StackPanel stack = new StackPanel();
        stack.Children.Add(_box);
        stack.Children.Add(_hint);
        Content = stack;

        ActualThemeVariantChanged += (_, _) => RenderLines();
    }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        _text = InstanceMember.Value as string ?? string.Empty;
        _box.Margin = new Thickness(InstanceMember.FirstGridLength, 0, 4, 0);
        RenderLines();
        RefreshHint(_hint);
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? result)
    {
        result = _text;
        return ApplyValueResult.Success;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object? value)
    {
        _text = value as string ?? string.Empty;
        RenderLines();
        return ApplyValueResult.Success;
    }

    private void RenderLines()
    {
        bool isLight = ActualThemeVariant == ThemeVariant.Light;
        IBrush generated = new SolidColorBrush(Color.Parse(isLight ? "#1A5FA8" : "#7DB7F0"));
        IBrush custom = new SolidColorBrush(Color.Parse(isLight ? "#1E7A34" : "#7FD28A"));

        _lines.Inlines ??= new InlineCollection();
        _lines.Inlines.Clear();
        string[] lines = _text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            bool isPath = line.Contains('/');
            Run run = new Run(i == lines.Length - 1 ? line : line + "\n");
            if (!isPath)
            {
                run.Foreground = new SolidColorBrush(Color.Parse("#99808080"));
            }
            else
            {
                run.Foreground = line.TrimEnd().EndsWith(".Generated.cs") ? generated : custom;
            }
            _lines.Inlines.Add(run);
        }
    }
}
