using Gum.DataTypes;
using Gum.Services.Dialogs;
using System;
using System.Linq;

namespace StateAnimationPlugin.Managers;

/// <summary>
/// Offers to delete a Component/Screen's animation (.ganx) sidecar file alongside the element
/// itself, via the DeleteOptionsWindow plugin-extension flow (see the gum-tool-delete-logic
/// skill). Framework-neutral: exposes <see cref="DeleteOptionCheckboxViewModel"/> data for the WPF
/// host (<c>MainStateAnimationPlugin</c>) to materialize into a real checkbox, rather than owning a
/// live WPF control itself (ADR-0005).
/// </summary>
public class ElementDeleteService
{
    private readonly IAnimationFilePathService _animationFilePathService;
    private readonly IDialogService _dialogService;

    public ElementDeleteService(IAnimationFilePathService animationFilePathService, IDialogService dialogService)
    {
        _animationFilePathService = animationFilePathService;
        _dialogService = dialogService;
    }

    /// <summary>
    /// Returns a checkbox view model to show in the DeleteOptionsWindow when any Component/Screen
    /// among <paramref name="objectsToDelete"/> has an existing animation file, or null when no
    /// checkbox should be shown.
    /// </summary>
    public DeleteOptionCheckboxViewModel? HandleDeleteOptionsWindowShow(Array objectsToDelete)
    {
        bool anyAnimationFileExists = objectsToDelete.OfType<ElementSave>()
            .Where(item => item is ComponentSave or ScreenSave)
            .Any(item => _animationFilePathService.GetAbsoluteAnimationFileNameFor(item)?.Exists() == true);

        if (!anyAnimationFileExists)
        {
            return null;
        }

        return new DeleteOptionCheckboxViewModel
        {
            Label = "Delete Animation file (.ganx)",
            IsChecked = true
        };
    }

    /// <summary>
    /// Deletes the animation (.ganx) file for each deleted element when <paramref name="isChecked"/>
    /// is true (the checkbox added by <see cref="HandleDeleteOptionsWindowShow"/> was left checked).
    /// </summary>
    public void HandleConfirmDelete(Array deletedObjects, bool isChecked)
    {
        if (!isChecked)
        {
            return;
        }

        foreach (var deletedObject in deletedObjects)
        {
            if (deletedObject is ElementSave deletedElement)
            {
                var fileName = _animationFilePathService.GetAbsoluteAnimationFileNameFor(deletedElement);

                if (fileName?.Exists() == true)
                {
                    try
                    {
                        System.IO.File.Delete(fileName.FullPath);
                    }
                    catch
                    {
                        _dialogService.ShowMessage("Could not delete the file\n" + fileName);
                    }
                }
            }
        }
    }
}
