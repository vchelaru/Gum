using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.DataTypes
{



    public enum StandardElementTypes
    {
        Text,
        Sprite,
        Container,
        NineSlice,
        ColoredRectangle,
        Polygon,
        Circle,
        Rectangle
    }

    public class StandardElementSave : ElementSave
    {
        public override string FileExtension
        {
            get { return GumProjectSave.StandardExtension; }
        }

        public override string Subfolder
        {
            get { return ElementReference.StandardSubfolder; }
        }
    }
}
