using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolsUtilities;

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

        public ElementSave Clone()
        {
            ElementSave cloned = FileManager.CloneSaveObject(this);
            return cloned;

        }
    }
}
