using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.ToolStates;
using Gum.StateAnimation.SaveClasses;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using Gum.Services;

namespace StateAnimationPlugin.Managers;

public class AnimationCollectionViewModelManager : Singleton<AnimationCollectionViewModelManager>
{
    AnimationFilePathService _animationFilePathService;
    private readonly ISelectedState _selectedState;
    private readonly INameVerifier _nameVerifier;

    public AnimationCollectionViewModelManager()
    {
        _animationFilePathService = new AnimationFilePathService();
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _nameVerifier = Locator.GetRequiredService<INameVerifier>();
    }

    public ElementAnimationsViewModel GetAnimationCollectionViewModel(ElementSave element)
    {
        if(element == null)
        {
            return null;
        }
        else
        {
            var model = GetElementAnimationsSave(element);

            ElementAnimationsViewModel toReturn;
            if (model != null)
            {
                toReturn = ElementAnimationsViewModel.FromSave(model, element, _nameVerifier);
            }
            else
            {
                toReturn = new ElementAnimationsViewModel(_nameVerifier);

            }

            toReturn.Element = element;

            return toReturn;
        }

    }

    public ElementAnimationsSave GetElementAnimationsSave(ElementSave element)
    {
        ElementAnimationsSave model = null;
        var fileName = _animationFilePathService.GetAbsoluteAnimationFileNameFor(element);


        if (fileName.Exists())
        {
            try
            {
                model = FileManager.XmlDeserialize<ElementAnimationsSave>(fileName.FullPath);

            }
            catch (Exception exception)
            {
                OutputManager.Self.AddError(exception.ToString());
            }
        }

        return model;
    }

    public void Save(ElementAnimationsViewModel viewModel)
    {
        var currentElement = _selectedState.SelectedElement;

        var fileName = _animationFilePathService.GetAbsoluteAnimationFileNameFor(currentElement);

        if(fileName != null)
        {
            var save = viewModel.ToSave();

            FileWatchManager.Self.IgnoreNextChangeUntil(fileName.FullPath);
            FileManager.XmlSerialize(save, fileName.FullPath);
        }
    }


}
