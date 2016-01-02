using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateAnimationPlugin.Managers
{
    class RenameManager : Singleton<RenameManager>
    {

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
    }
}
