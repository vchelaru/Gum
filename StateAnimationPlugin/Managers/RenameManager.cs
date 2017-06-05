using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers
{
    class RenameManager : Singleton<RenameManager>
    {
        public void HandleRename(ElementSave elementSave, string oldName, ElementAnimationsViewModel viewModel)
        {
            // save the new:
            bool succeeded = false;
            try
            {
                AnimationCollectionViewModelManager.Self.Save(viewModel);
                succeeded = true;
            }
            catch
            {
                succeeded = false;
            }

            if(succeeded)
            {
                var oldFileName = AnimationCollectionViewModelManager.Self.GetAbsoluteAnimationFileNameFor(oldName);

                if(System.IO.File.Exists(oldFileName))
                {
                    System.IO.File.Delete(oldFileName);
                }
            }
        }


        public void HandleRename(InstanceSave instanceSave, string oldName, ElementAnimationsViewModel viewModel)
        {
            var whatToLookFor = $"{oldName}.";

            foreach (var animation in viewModel.Animations)
            {
                foreach (var keyframe in animation.Keyframes)
                {
                    if(keyframe.AnimationName != null && keyframe.AnimationName.StartsWith(whatToLookFor))
                    {

                        var variable =
                            keyframe.AnimationName.Substring(whatToLookFor.Length);

                        keyframe.AnimationName =
                            instanceSave.Name + "." + variable; 
                    }
                }
            }

        }

        public void HandleRename(StateSave stateSave, string oldName, ElementAnimationsViewModel viewModel)
        {
            foreach(var animation in viewModel.Animations)
            {
                foreach(var keyframe in animation.Keyframes)
                {
                    if(keyframe.StateName == oldName)
                    {
                        keyframe.StateName = stateSave.Name;
                    }
                }
            }
        }

        public void HandleRename(StateSaveCategory category, string oldName, ElementAnimationsViewModel viewModel)
        {
            foreach (var animation in viewModel.Animations)
            {
                foreach (var keyframe in animation.Keyframes)
                {
                    if(keyframe.StateName != null && keyframe.StateName.StartsWith(oldName + "/"))
                    {
                        keyframe.StateName = category.Name + "/" + FileManager.RemovePath(keyframe.StateName);
                    }
                }
            }
        }
    }
}
