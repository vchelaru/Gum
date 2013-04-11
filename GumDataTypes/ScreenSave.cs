using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.DataTypes
{
    public class ScreenSave : ElementSave
    {
        public override string FileExtension
        {
            get { return GumProjectSave.ScreenExtension; }
        }

        public override string Subfolder
        {
            get { return ElementReference.ScreenSubfolder; }
        }
    }
}
