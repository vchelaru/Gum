using Gum.StateAnimation.SaveClasses;
﻿using Gum;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Services.Dialogs;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers
{
    public class DuplicateService : IDuplicateService
    {
        private readonly IDialogService _dialogService;
        private readonly IProjectManager _projectManager;

        public DuplicateService(IDialogService dialogService, IProjectManager projectManager)
        {
            _dialogService = dialogService;
            _projectManager = projectManager;
        }

        public void HandleDuplicate(ElementSave oldElement, ElementSave newElement)
        {
            var project = _projectManager.GumProjectSave;
            //////////////////////Early Out////////////////////
            if(project == null)
            {
                return;
            }
            //////////////////////End Early Out////////////////////
            ///
            var projectDirectory = FileManager.GetDirectory(project.FullFileName);

            // Suffix follows the open project's own format so a .gumj project duplicates .ganj
            // rather than looking for a .ganx that doesn't exist (issue #4595).
            var suffix = ElementAnimationsSave.GetFileNameSuffix(GumProjectSave.IsJsonFormat(project.FullFileName));

            var oldFile = new FilePath(projectDirectory + oldElement.Subfolder + "/" + oldElement.Name + suffix);
            var newFile = new FilePath(projectDirectory + newElement.Subfolder + "/" + newElement.Name + suffix);

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

                    if (newDirectory != null && System.IO.Directory.Exists(newDirectory.FullPath) == false)
                    {
                        System.IO.Directory.CreateDirectory(newDirectory.FullPath);
                    }

                    System.IO.File.Copy(oldFile.FullPath, newFile.FullPath, overwrite:true);
                }
            }
        }
    }
}
