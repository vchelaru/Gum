using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.Managers
{
    public class NameVerifier
    {
        static NameVerifier mSelf;

        public static NameVerifier Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new NameVerifier();
                }
                return mSelf;
            }
        }


        public bool IsFolderNameValid(string folderName, out string whyNotValid)
        {

            IsNameValidCommon(folderName, out whyNotValid);

            return string.IsNullOrEmpty(whyNotValid);

        }

        public bool IsScreenNameValid(string screenName, out string whyNotValid)
        {
            IsNameValidCommon(screenName, out whyNotValid);

            return string.IsNullOrEmpty(whyNotValid);

        }

        public bool IsComponentNameValid(string componentName, out string whyNotValid)
        {
            IsNameValidCommon(componentName, out whyNotValid);

            return string.IsNullOrEmpty(whyNotValid);
        }

        public bool IsInstanceNameValid(string instanceName, out string whyNotValid)
        {
            IsNameValidCommon(instanceName, out whyNotValid);

            return string.IsNullOrEmpty(whyNotValid);
        }

        private void IsNameValidCommon(string folderName, out string whyNotValid)
        {
            whyNotValid = null;
            if (string.IsNullOrEmpty(folderName))
            {
                whyNotValid = "Empty names are not valid";
            }

        }

    }
}
