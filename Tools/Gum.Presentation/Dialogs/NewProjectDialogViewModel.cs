using Gum.Services.Dialogs;

namespace Gum.Dialogs;

/// <summary>
/// Options shown when creating a new project. Forms controls are included by default because
/// almost every project wants them, and finding them otherwise requires knowing the Add Forms
/// menu item exists.
/// </summary>
public class NewProjectDialogViewModel : DialogViewModel
{
    public NewProjectDialogViewModel()
    {
        IsIncludeFormsControls = true;
    }

    /// <summary>
    /// Whether the default Forms theme is imported into the new project.
    /// </summary>
    public bool IsIncludeFormsControls
    {
        get => Get<bool>();
        set => Set(value);
    }
}
