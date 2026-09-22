using Gum.DataTypes;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Gum.Wireframe;
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
    private readonly IAnimationFilePathService _animationFilePathService;
    private readonly IOutputManager _outputManager;
    private readonly ISelectedState _selectedState;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IAnimationSaveRepository _animationCollectionViewModelManager;

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

    public AnimationViewModel? AnimationToExclude { get; internal set; }

    public override bool CanExecuteAffirmative() => SelectedAnimation is not null;

    public SubAnimationSelectionDialogViewModel(IAnimationSaveRepository animationCollectionViewModelManager,
        IOutputManager outputManager, ISelectedState selectedState,
        IWireframeObjectManager wireframeObjectManager, IAnimationFilePathService animationFilePathService)
    {
        _outputManager = outputManager;
        _selectedState = selectedState;
        _wireframeObjectManager = wireframeObjectManager;
        _animationFilePathService = animationFilePathService;
        _animationCollectionViewModelManager = animationCollectionViewModelManager;
    }


    private IEnumerable<AnimationViewModel> GetAnimationsForContainer(AnimationContainerViewModel container)
    {
        FilePath? fileName = GetFileNameForSelectedContainerAnimations(out ElementSave? elementSave);


        if (fileName?.Exists() == true)
        {
            ElementAnimationsSave? save = null;

            try
            {
                save = ElementAnimationsSave.Load(fileName.FullPath);
            }
            catch (Exception exception)
            {
                _outputManager.AddError(exception.ToString());

            }

            if (save != null)
            {
                foreach (var item in save.Animations)
                {
                    AnimationViewModel toReturn = AnimationViewModel.FromSave(
                        item, elementSave!, _animationCollectionViewModelManager, _selectedState, _wireframeObjectManager);

                    toReturn.Name = item.Name;
                    toReturn.ContainingInstance = container.InstanceSave;

                    // An animation on this element (an instance's animations cannot reach back
                    // here) is skipped when it is the one being edited, or already plays it,
                    // directly or through another animation: either would loop when played.
                    bool shouldSkip =
                        container.InstanceSave == null &&
                        AnimationToExclude != null &&
                        (toReturn.Name == AnimationToExclude.Name || toReturn.PlaysOwnAnimation(AnimationToExclude.Name));

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
