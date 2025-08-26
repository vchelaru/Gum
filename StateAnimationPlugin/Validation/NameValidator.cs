using Gum.Managers;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateAnimationPlugin.Validation;

public class NameValidator
{
    private readonly INameVerifier _nameVerifier;

    public NameValidator(INameVerifier nameVerifier)
    {
        _nameVerifier = nameVerifier;
    }
    public bool IsAnimationNameValid(string animationName,
        IEnumerable<AnimationViewModel> existingAnimations, out string whyNotValid)
    {
        whyNotValid = null;

        if(string.IsNullOrEmpty(animationName))
        {
            whyNotValid = "Animation names cannot be empty";
            return false;
        }
        if(animationName.Contains(" "))
        {
            whyNotValid = "Animation names cannot have spaces";
            return false;
        }
        if (!_nameVerifier.IsNameValidCommon(animationName, out whyNotValid, out _))
        {
            return false;
        }
        if(existingAnimations.Any(item=>item.Name.Equals(animationName, StringComparison.InvariantCultureIgnoreCase)))
        {
            whyNotValid = $"The name \"{animationName}\" is already being used.";
            return false;
        }
        
        return string.IsNullOrEmpty(whyNotValid);
    }
}
