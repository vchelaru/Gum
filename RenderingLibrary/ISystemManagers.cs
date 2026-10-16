using Gum;
using RenderingLibrary.Graphics;

namespace RenderingLibrary
{
    public interface ISurfaceInvalidatable
    {
        void InvalidateSurface();
    }

    public interface ISystemManagers : ISurfaceInvalidatable
    {
        bool EnableTouchEvents { get; set; }
        IRenderer Renderer { get; }

        /// <summary>
        /// The <see cref="GumContext"/> of the service these managers belong to, or null when they do not
        /// belong to a service. Building an element from these managers activates it.
        /// </summary>
        GumContext? Context => null;

        // Set by Gum initialization (e.g. GumService.Initialize); not usable before that. With several services,
        // assigning IGumService.Default moves this to the new default's managers, so it is a plain static.
        public static ISystemManagers Default { get; set; } = null!;
    }
}
