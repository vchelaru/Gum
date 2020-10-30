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
    class DuplicateManager: Singleton<DuplicateManager>
    {
        public void HandleDuplicate(ElementSave oldElement, ElementSave newElement)
        {
            var projectDirectory = FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName);

            var oldFile = new FilePath(projectDirectory + oldElement.Subfolder + "/" + oldElement.Name + "Animations.ganx");

            if (oldFile.Exists())
            {
                var newFile = new FilePath(projectDirectory + newElement.Subfolder + "/" + newElement.Name + "Animations.ganx");

                var newDirectory = newFile.GetDirectoryContainingThis();

                if (System.IO.Directory.Exists(newDirectory.FullPath) == false)
                {
                    System.IO.Directory.CreateDirectory(newDirectory.FullPath);
                }

                System.IO.File.Copy(oldFile.FullPath, newFile.FullPath);
            }
        }
    }
}
