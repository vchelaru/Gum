using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary.Graphics
{
    public enum TextOverflowHorizontalMode
    {
        TruncateWord,
        EllipsisLetter,
        // eventually?
        //ScaleToFit
    }

    public enum TextOverflowVerticalMode
    {
        SpillOver,
        TruncateLine
    }
}
