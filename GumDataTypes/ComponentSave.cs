using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.DataTypes
{
    public class ComponentSave : ElementSave
    {
        public override string FileExtension
        {
            get { return GumProjectSave.ComponentExtension; }
        }


        public override string Subfolder
        {
            get { return ElementReference.ComponentSubfolder; }
        }

    }
}
