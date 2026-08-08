namespace Gum.ViewModels;

/// <summary>
/// Well-known <see cref="ContextMenuItemViewModel.IconKey"/> values, shared between the headless
/// view models that set them and the WPF-side renderer that maps them to an actual icon.
/// </summary>
public static class ContextMenuIconKeys
{
    public const string State = nameof(State);
    public const string Category = nameof(Category);
}
