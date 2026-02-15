using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using Gum.StateAnimation.SaveClasses;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using Gum.Services;

namespace StateAnimationPlugin.Managers
{
    class RenameManager : Singleton<RenameManager>
    {
        private readonly AnimationFilePathService _animationFilePathService;
        private readonly ISelectedState _selectedState;
        private readonly IOutputManager _outputManager;

        public RenameManager()
        {
            _animationFilePathService = new AnimationFilePathService();
            _selectedState = Locator.GetRequiredService<ISelectedState>();
            _outputManager = Locator.GetRequiredService<IOutputManager>();
        }

        public void HandleRename(ElementSave elementSave, string oldName, ElementAnimationsViewModel viewModel)
        {
            if (elementSave == viewModel?.Element)
            {
                var oldFileName = _animationFilePathService.GetAbsoluteAnimationFileNameFor(oldName);

                // save if we had an old file, or if there are any animations.
                // We still want to save if there are no animations because the
                // user may explicitly remove animations and we want that to save
                // so it can overwrite old files that might have animations.
                var shouldSave = oldFileName?.Exists() == true || viewModel.Animations.Count > 0;

                // save the new:
                bool succeeded = false;
                try
                {
                    if(shouldSave)
                    {
                        AnimationCollectionViewModelManager.Self.Save(viewModel);
                    }
                    succeeded = true;
                }
                catch
                {
                    succeeded = false;
                }

                if (succeeded)
                {
                    if (oldFileName?.Exists() == true)
                    {
                        System.IO.File.Delete(oldFileName.FullPath);
                    }
                }
            }
            else // renaming an element that is not currently selected. See if it has an animation, and if so move it
            {
                var gumProject = ProjectManager.Self.GumProjectSave;
                if(gumProject == null)
                {
                    throw new InvalidOperationException("Renaming elements is not supported when a Gum project is null...how did this happen anyway?");
                }
                var projectDirectory = FileManager.GetDirectory(gumProject.FullFileName);

                var oldFile = new FilePath( projectDirectory + elementSave.Subfolder + "/" + oldName + "Animations.ganx");
                
                if(oldFile.Exists())
                {
                    var newFile = new FilePath(projectDirectory + elementSave.Subfolder + "/" + elementSave.Name + "Animations.ganx");

                    var newDirectory = newFile.GetDirectoryContainingThis();

                    if(newDirectory != null && System.IO.Directory.Exists(newDirectory.FullPath) == false)
                    {
                        System.IO.Directory.CreateDirectory(newDirectory.FullPath);
                    }

                    System.IO.File.Move(oldFile.FullPath, newFile.FullPath);
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
            var parentCategory = _selectedState.SelectedElement?.Categories.FirstOrDefault(item => item.States.Contains(stateSave));

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
                var fileName = _animationFilePathService.GetAbsoluteAnimationFileNameFor(elementReferencing);

                bool didChange = false;

                if (fileName?.Exists() == true)
                {
                    try
                    {
                        var animationSave = FileManager.XmlDeserialize<ElementAnimationsSave>(fileName.FullPath);

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
                            FileManager.XmlSerialize(animationSave, fileName.FullPath);
                        }
                    }
                    catch (Exception e)
                    {
                        _outputManager.AddError(e.ToString());
                    }
                }
            }
        }
    }
}
