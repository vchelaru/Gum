using Gum.DataTypes;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.StateAnimation.SaveClasses;
using StateAnimationPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace StateAnimationPlugin.ViewModels;

public class SubAnimationSelectionDialogViewModel : DialogViewModel
{
    private readonly AnimationFilePathService _animationFilePathService = new();

    public List<AnimationContainerViewModel>? AnimationContainers { get; set; }
    public AnimationContainerViewModel? SelectedContainer
    {
        get => Get <AnimationContainerViewModel?> ();
        set
        {
            if (Set(value))
            {
                Animations.Clear();
                if (value is { } selected)
                {
                    Animations.AddRange(GetAnimationsForContainer(selected));
                }
            }
        }
    }

    public ObservableCollection<AnimationViewModel> Animations { get; } = new();
    public AnimationViewModel? SelectedAnimation
    {
        get => Get<AnimationViewModel?>();
        set
        {
            if (Set(value))
            {
                AffirmativeCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public AnimationViewModel AnimationToExclude { get; internal set; }

    public override bool CanExecuteAffirmative() => SelectedAnimation is not null;


    private IEnumerable<AnimationViewModel> GetAnimationsForContainer(AnimationContainerViewModel container)
    {
        FilePath? fileName = GetFileNameForSelectedContainerAnimations(out ElementSave? elementSave);


        if (fileName?.Exists() == true)
        {
            ElementAnimationsSave? save = null;

            try
            {
                save = FileManager.XmlDeserialize<ElementAnimationsSave>(fileName.FullPath);
            }
            catch (Exception exception)
            {
                OutputManager.Self.AddError(exception.ToString());

            }

            if (save != null)
            {
                foreach (var item in save.Animations)
                {
                    AnimationViewModel toReturn = AnimationViewModel.FromSave(
                        item, elementSave);

                    toReturn.Name = item.Name;
                    toReturn.ContainingInstance = container.InstanceSave;

                    bool shouldSkip = false;

                    // Right now we're just checking to make sure an animation doesn't
                    // reference itself, but that doesn't prevent A referending B referencing A
                    // Eventually we need a deeper reursive check.

                    // skip if...
                    shouldSkip =
                        // we selected an animation that isn't on an instance (if it is, then
                        // there is no chance of it being recursive)...
                        container.InstanceSave == null &&
                        // And there is something to exclude...
                        AnimationToExclude != null &&
                        // and the names match
                        toReturn.Name == AnimationToExclude.Name;

                    if (!shouldSkip)
                    {
                        yield return toReturn;
                    }
                }
            }

        }

        FilePath? GetFileNameForSelectedContainerAnimations(out ElementSave? element)
        {
            FilePath? fileName = null;
            if (container.InstanceSave == null)
            {
                element = container.ElementSave;

                // Get all animations on "this" container
                fileName =
                    _animationFilePathService.GetAbsoluteAnimationFileNameFor(container.ElementSave);
            }
            else
            {
                var instance = container.InstanceSave;

                var instanceElement = ObjectFinder.Self.GetElementSave(instance);
                element = instanceElement;

                if (instanceElement != null)
                {
                    fileName = _animationFilePathService.GetAbsoluteAnimationFileNameFor(
                        instanceElement);

                }
            }
            return fileName;
        }
    }
}