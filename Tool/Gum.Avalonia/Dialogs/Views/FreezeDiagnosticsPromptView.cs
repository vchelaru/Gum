using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Gum.Diagnostics;

namespace Gum.Avalonia.Dialogs.Views;

/// <summary>Shows <see cref="FreezeDiagnosticsPromptViewModel"/>: the message and a "don't ask again" check box.</summary>
public sealed class FreezeDiagnosticsPromptView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public FreezeDiagnosticsPromptView()
    {
        Spacing = 8;
        MaxWidth = 560;
        TextBlock message = new TextBlock { TextWrapping = TextWrapping.Wrap };
        message.Bind(TextBlock.TextProperty, new Binding(nameof(FreezeDiagnosticsPromptViewModel.Message)));
        Children.Add(message);

        CheckBox doNotAskAgain = new CheckBox { Content = "Don't ask again", Margin = new Thickness(0, 8, 0, 0) };
        doNotAskAgain.Bind(CheckBox.IsCheckedProperty, new Binding(nameof(FreezeDiagnosticsPromptViewModel.IsDoNotAskAgainChecked)) { Mode = BindingMode.TwoWay });
        Children.Add(doNotAskAgain);
    }
}
