using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.ToolStates;
using Gum.StateAnimation.SaveClasses;
using StateAnimationPlugin.ViewModels;
using System;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers;

/// <summary>
/// Loads and saves an element's animations, bridging the on-disk <see cref="ElementAnimationsSave"/>
/// and the editable <see cref="ElementAnimationsViewModel"/>. Instantiated by
/// <see cref="MainStateAnimationPlugin"/>; not an app-wide service.
/// </summary>
public class AnimationCollectionViewModelManager : IAnimationCollectionViewModelManager
{
    private readonly IAnimationFilePathService _animationFilePathService;
    private readonly ISelectedState _selectedState;
    private readonly IFileWatchManager _fileWatchManager;
    private readonly Func<ElementAnimationsViewModel> _animationVmFactory;
    private readonly IOutputManager _outputManager;

    public AnimationCollectionViewModelManager(
        ISelectedState selectedState,
        IOutputManager outputManager,
        IFileWatchManager fileWatchManager,
        IAnimationFilePathService animationFilePathService,
        Func<ElementAnimationsViewModel> animationVmFactory)
    {
        _selectedState = selectedState;
        _outputManager = outputManager;
        _fileWatchManager = fileWatchManager;
        _animationFilePathService = animationFilePathService;
        _animationVmFactory = animationVmFactory;
    }

    /// <inheritdoc/>
    public ElementAnimationsViewModel? GetAnimationCollectionViewModel(ElementSave? element)
    {
        if (element == null)
        {
            return null;
        }

        ElementAnimationsViewModel toReturn = _animationVmFactory();

        if (GetElementAnimationsSave(element) is { } model)
        {
            toReturn.LoadFromSave(model, element);
        }

        toReturn.Element = element;

        return toReturn;
    }

    /// <inheritdoc/>
    public ElementAnimationsSave? GetElementAnimationsSave(ElementSave element)
    {
        ElementAnimationsSave? model = null;
        var fileName = _animationFilePathService.GetAbsoluteAnimationFileNameFor(element);

        if (fileName?.Exists() == true)
        {
            try
            {
                model = FileManager.XmlDeserialize<ElementAnimationsSave>(fileName.FullPath);
            }
            catch (Exception exception)
            {
                _outputManager.AddError(exception.ToString());
            }
        }

        return model;
    }

    /// <inheritdoc/>
    public void Save(ElementAnimationsViewModel viewModel)
    {
        var currentElement = _selectedState.SelectedElement;

        if (currentElement == null)
        {
            return;
        }

        var fileName = _animationFilePathService.GetAbsoluteAnimationFileNameFor(currentElement);

        if (fileName != null)
        {
            var save = viewModel.ToSave();

            _fileWatchManager.IgnoreNextChangeUntil(fileName.FullPath);
            FileManager.XmlSerialize(save, fileName.FullPath);
        }
    }

    /// <inheritdoc/>
    public void SaveElementAnimations(ElementSave element, ElementAnimationsSave save)
    {
        var fileName = _animationFilePathService.GetAbsoluteAnimationFileNameFor(element);

        if (fileName != null)
        {
            _fileWatchManager.IgnoreNextChangeUntil(fileName.FullPath);
            FileManager.XmlSerialize(save, fileName.FullPath);
        }
    }
}
