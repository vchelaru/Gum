using Gum.Wireframe;
using RenderingLibrary.Graphics;

namespace GumToPython
{
    public static class GumToPythonHelpers
    {
        /// <summary>
        /// Try to cast an IRenderableIpso to a GraphicalUiElement. Returns null if it’s not one.
        /// </summary>
        public static GraphicalUiElement AsGue(IRenderableIpso ipso)
        {
            return ipso as GraphicalUiElement;
        }
    }
}
