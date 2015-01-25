using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateAnimationPlugin.Validation
{
    public static class NameValidator
    {
        public static bool IsAnimationNameValid(string animationName, out string whyNotValid)
        {
            whyNotValid = null;

            if(string.IsNullOrEmpty(animationName))
            {
                whyNotValid = "Animation names cannot be empty";
            }

            else if(animationName.Contains(" "))
            {
                whyNotValid = "Animation names cannot have spaces";   
            }
            else if (animationName.IndexOfAny(Gum.Managers.NameVerifier.InvalidCharacters) != -1)
            {
                whyNotValid = "The name can't contain invalid character " + animationName[animationName.IndexOfAny(Gum.Managers.NameVerifier.InvalidCharacters)];
            }


            return string.IsNullOrEmpty(whyNotValid);
        }
    }
}
