using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary.Graphics
{
    public interface IRenderer
    {
        Camera Camera { get; }

        void RenderLayer(ISystemManagers managers, Layer layer, bool prerender = true);

        ReadOnlyCollection<Layer> Layers { get; }
    }
}
