namespace StateAnimationPlugin.Managers;

/// <summary>
/// Framework-neutral request to show a checkbox option in the DeleteOptionsWindow's
/// plugin-extension area (see <c>DeleteDialogService</c>/<c>DeleteOptionsWindow.MainStackPanel</c>).
/// The WPF host materializes this into a real checkbox and, once the user confirms delete, reads
/// its final <see cref="IsChecked"/> state back into <see cref="ElementDeleteService"/> (ADR-0005).
/// </summary>
public class DeleteOptionCheckboxViewModel
{
    /// <summary>
    /// The checkbox's display text.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The checkbox's initial checked state when shown.
    /// </summary>
    public bool IsChecked { get; set; }
}
