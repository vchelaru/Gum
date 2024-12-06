using Gum;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers
{
    class DuplicateService
    {
        public void HandleDuplicate(ElementSave oldElement, ElementSave newElement)
        {
            var projectDirectory = FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName);

            var oldFile = new FilePath(projectDirectory + oldElement.Subfolder + "/" + oldElement.Name + "Animations.ganx");
            var newFile = new FilePath(projectDirectory + newElement.Subfolder + "/" + newElement.Name + "Animations.ganx");

            if (oldFile.Exists())
            {
                bool shouldCopy = true;
                if(newFile.Exists())
                {
                    var result =
                        GumCommands.Self.GuiCommands.ShowYesNoMessageBox($"The animation file already exists:\n{newFile}\n\nDo you want to copy the animations from {oldElement.Name} over the existing file?");
                    shouldCopy = result == System.Windows.MessageBoxResult.Yes;
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
