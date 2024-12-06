using Gum.DataTypes;
using Gum.Gui.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers;

internal class ElementDeleteService
{
    private readonly AnimationFilePathService _animationFilePathService;
    private CheckBox deleteAnimationFileCheckbox;

    public ElementDeleteService(AnimationFilePathService animationFilePathService)
    {
        _animationFilePathService = animationFilePathService;
        deleteAnimationFileCheckbox = new CheckBox();
        deleteAnimationFileCheckbox.IsChecked = true;
        deleteAnimationFileCheckbox.Width = 220;
    }

    internal void HandleDeleteOptionsWindowShow(DeleteOptionsWindow deleteWindow, object objectToDelete)
    {
        if (objectToDelete is ComponentSave or ScreenSave)
        {
            FilePath? animationFile = _animationFilePathService.GetAbsoluteAnimationFileNameFor(objectToDelete as ElementSave);
            deleteAnimationFileCheckbox.IsChecked = false;
            if (animationFile?.Exists() == true)
            {
                deleteAnimationFileCheckbox.IsChecked = true;
                deleteWindow.MainStackPanel.Children.Add(deleteAnimationFileCheckbox);
                deleteAnimationFileCheckbox.Content = "Delete Animation file (.ganx)";
            }
        }
    }

    internal void HandleConfirmDelete(DeleteOptionsWindow deleteOptionsWindow, object deletedObject)
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
                    System.Windows.Forms.MessageBox.Show("Could not delete the file\n" + fileName);
                }
            }
        }

        if(deleteOptionsWindow.MainStackPanel.Children.Contains(deleteAnimationFileCheckbox))
        {
            deleteOptionsWindow.MainStackPanel.Children.Remove(deleteAnimationFileCheckbox);
        }
    }

}
