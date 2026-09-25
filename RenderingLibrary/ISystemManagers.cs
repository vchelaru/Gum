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

        // Set by Gum initialization (e.g. GumService.Initialize); not usable before that.
        public static ISystemManagers Default { get; set; } = null!;
    }
}
