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

        Layer MainLayer { get; }

        /// <summary>
        /// Adds a new layer on top of the existing ones and returns it. Add renderables to the returned
        /// layer to draw them above the main layer. The default throws, so a custom renderer that does not
        /// manage layers fails loudly instead of silently dropping them.
        /// </summary>
        /// <exception cref="NotSupportedException">The renderer does not support adding layers.</exception>
        Layer AddLayer() => null!;

        /// <summary>
        /// Removes <paramref name="layer"/>. The default throws.
        /// </summary>
        /// <param name="layer">The layer to remove.</param>
        /// <exception cref="NotSupportedException">The renderer does not support removing layers.</exception>
        void RemoveLayer(Layer layer) { }
    }
}
