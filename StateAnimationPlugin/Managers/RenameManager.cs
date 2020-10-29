using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using StateAnimationPlugin.SaveClasses;
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
            var parentCategory = SelectedState.Self.SelectedElement?.Categories.First(item => item.States.Contains(stateSave));

            string prefix = "";
            if(parentCategory != null)
            {
                prefix = parentCategory + "/";
            }

            oldName = prefix + oldName;

            foreach(var animation in viewModel.Animations)
            {
                foreach(var keyframe in animation.Keyframes)
                {
                    if(keyframe.StateName == oldName)
                    {
                        keyframe.StateName = prefix + stateSave.Name;
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

        public void HandleRename(AnimationViewModel animationViewModel, string oldAnimationName,
            IEnumerable<AnimationViewModel> animations, ElementSave element)
        {
            foreach (var keyframe in animations.SelectMany(item => item.Keyframes))
            {
                if (keyframe.AnimationName == oldAnimationName)
                {
                    keyframe.AnimationName = animationViewModel.Name;
                }
            }

            // Unfortunately we have to jump out of the view model and
            // look at any object where this is an instance, and see if 
            // its animation is referenced.
            var elementsReferencingThis = ObjectFinder.Self.GetElementsReferencing(element);


            foreach (var elementReferencing in elementsReferencingThis)
            {
                var fileName = AnimationCollectionViewModelManager.Self.GetAbsoluteAnimationFileNameFor(elementReferencing);

                bool didChange = false;

                if (FileManager.FileExists(fileName))
                {
                    try
                    {
                        var animationSave = FileManager.XmlDeserialize<ElementAnimationsSave>(fileName);

                        var potentialAnimations = animationSave.Animations
                            .SelectMany(item => item.Animations)
                            .Where(item =>!string.IsNullOrEmpty(item.SourceObject)
                                        && item.RootName == oldAnimationName);

                        foreach (var animationReference in potentialAnimations)
                        {
                            var instance = elementReferencing.GetInstance(animationReference.SourceObject);
                            if(instance != null)
                            {
                                // Is the instance this?
                                var instanceElement = ObjectFinder.Self.GetElementSave(instance);
                                if(instanceElement == element)
                                {
                                    didChange = true;
                                    animationReference.Name = animationReference.SourceObject + "." + animationViewModel.Name;
                                }
                            }
                        }

                        if(didChange)
                        {
                            FileManager.XmlSerialize(animationSave, fileName);
                        }
                    }
                    catch (Exception e)
                    {
                        OutputManager.Self.AddError(e.ToString());
                    }
                }
            }
        }
    }
}
