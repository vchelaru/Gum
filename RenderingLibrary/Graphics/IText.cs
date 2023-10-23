using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary.Graphics
{
    public interface IText
    {
        void SetNeedsRefreshToTrue();

        void UpdatePreRenderDimensions();
    }
}
