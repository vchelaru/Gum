using Gum.Services.Dialogs;
using GumFormsPlugin.ViewModels;

namespace Gum.Dialogs;

/// <summary>
/// Options shown when creating a new project. Forms controls are included by default because
/// almost every project wants them, and finding them otherwise requires knowing the Add Forms
/// menu item exists.
/// </summary>
public class NewProjectDialogViewModel : DialogViewModel
{
    /// <summary>
    /// Theme picker, shared with the Add Forms dialog. Only relevant when
    /// <see cref="IsIncludeFormsControls"/> is checked.
    /// </summary>
    public ThemeSelectionViewModel ThemeSelection { get; }

    public NewProjectDialogViewModel(ThemeSelectionViewModel themeSelection)
    {
        ThemeSelection = themeSelection;
        IsIncludeFormsControls = true;
    }

    /// <summary>
    /// Whether the selected Forms theme (<see cref="ThemeSelection"/>) is imported into the new
    /// project.
    /// </summary>
    public bool IsIncludeFormsControls
    {
        get => Get<bool>();
        set => Set(value);
    }
}
