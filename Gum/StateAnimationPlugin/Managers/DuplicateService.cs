using Gum;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Services;
using Gum.Services.Dialogs;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers
{
    public class DuplicateService
    {
        private readonly IDialogService _dialogService;
        public DuplicateService()
        {
            _dialogService = Locator.GetRequiredService<IDialogService>();
        }
        
        public void HandleDuplicate(ElementSave oldElement, ElementSave newElement)
        {
            var project = ProjectManager.Self.GumProjectSave;
            //////////////////////Early Out////////////////////
            if(project == null)
            {
                return;
            }
            //////////////////////End Early Out////////////////////
            ///
            var projectDirectory = FileManager.GetDirectory(project.FullFileName);

            var oldFile = new FilePath(projectDirectory + oldElement.Subfolder + "/" + oldElement.Name + "Animations.ganx");
            var newFile = new FilePath(projectDirectory + newElement.Subfolder + "/" + newElement.Name + "Animations.ganx");

            if (oldFile.Exists())
            {
                bool shouldCopy = true;
                if(newFile.Exists())
                {
                    shouldCopy = _dialogService.ShowYesNoMessage($"The animation file already exists:\n{newFile}\n\nDo you want to copy the animations from {oldElement.Name} over the existing file?");
                }

                if(shouldCopy)
                {
                    var newDirectory = newFile.GetDirectoryContainingThis();

                    if (System.IO.Directory.Exists(newDirectory.FullPath) == false)
                    {
                        System.IO.Directory.CreateDirectory(newDirectory.FullPath);
                    }

                    System.IO.File.Copy(oldFile.FullPath, newFile.FullPath, overwrite:true);
                }
            }
        }
    }
}
