using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateAnimationPlugin.Validation
{
    public static class NameValidator
    {
        public static bool IsAnimationNameValid(string animationName,
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
            if (Gum.Managers.NameVerifier.IsNameValidCommon(animationName, out whyNotValid))
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
}
