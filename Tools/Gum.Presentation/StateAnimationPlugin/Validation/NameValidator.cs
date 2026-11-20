using Gum.Managers;
using Gum.ProjectServices.Editing;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StateAnimationPlugin.Validation;

public class NameValidator
{
    private readonly INameVerifier _nameVerifier;

    public NameValidator(INameVerifier nameVerifier)
    {
        _nameVerifier = nameVerifier;
    }
    public bool IsAnimationNameValid(string? animationName, Gum.DataTypes.ElementSave element,
        IEnumerable<AnimationViewModel> existingAnimations, out string? whyNotValid, object? objectToIgnore = null)
    {
        whyNotValid = null;

        whyNotValid = AnimationNameRules.GetWhyInvalid(animationName,
            existingAnimations.Where(item => item != objectToIgnore).Select(item => item.Name));
        if (whyNotValid != null)
        {
            return false;
        }
        if (!_nameVerifier.IsNameValidCommon(animationName, out whyNotValid, out _))
        {
            return false;
        }

        if(element != null)
        {
            _nameVerifier.IsNameValidTopLevel(animationName, element, objectToIgnore, out whyNotValid);
        }

        return string.IsNullOrEmpty(whyNotValid);
    }
}
