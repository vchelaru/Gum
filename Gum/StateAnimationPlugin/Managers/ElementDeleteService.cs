using Gum.DataTypes;
using Gum.Gui.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Gum.Services;
using Gum.Services.Dialogs;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers;

internal class ElementDeleteService
{
    private readonly AnimationFilePathService _animationFilePathService;
    private readonly IDialogService _dialogService;
    private CheckBox deleteAnimationFileCheckbox;

    public ElementDeleteService(AnimationFilePathService animationFilePathService)
    {
        _animationFilePathService = animationFilePathService;
        deleteAnimationFileCheckbox = new CheckBox();
        deleteAnimationFileCheckbox.IsChecked = true;
        deleteAnimationFileCheckbox.Width = 220;
        _dialogService = Locator.GetRequiredService<IDialogService>();
    }

    internal void HandleDeleteOptionsWindowShow(DeleteOptionsWindow deleteWindow, Array objectsToDelete)
    {
        bool hasAlreadyAddedDeleteAnimationFileCheckBox = false;

        foreach(var objectToDelete in objectsToDelete)
        {
            if (objectToDelete is ComponentSave or ScreenSave)
            {
                var animationFile = _animationFilePathService.GetAbsoluteAnimationFileNameFor((ElementSave)objectToDelete);
                deleteAnimationFileCheckbox.IsChecked = false;
                if (hasAlreadyAddedDeleteAnimationFileCheckBox == false && (animationFile?.Exists() == true ))
                {
                    deleteAnimationFileCheckbox.IsChecked = true;
                    deleteWindow.MainStackPanel.Children.Add(deleteAnimationFileCheckbox);
                    deleteAnimationFileCheckbox.Content = "Delete Animation file (.ganx)";
                    hasAlreadyAddedDeleteAnimationFileCheckBox = true;
                }
            }
        }
    }

    internal void HandleConfirmDelete(DeleteOptionsWindow deleteOptionsWindow, Array deletedObjects)
    {
        foreach(var deletedObject in deletedObjects)
        {
            if (deleteAnimationFileCheckbox.IsChecked == true && deletedObject is ElementSave deletedElement)
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

            if(deleteOptionsWindow.MainStackPanel.Children.Contains(deleteAnimationFileCheckbox))
            {
                deleteOptionsWindow.MainStackPanel.Children.Remove(deleteAnimationFileCheckbox);
            }
        }
    }

}
