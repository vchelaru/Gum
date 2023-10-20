using RenderingLibrary.Graphics;

namespace RenderingLibrary
{
    public interface ISurfaceInvalidatable
    {
        void InvalidateSurface();
    }

    public interface ISystemManagers : ISurfaceInvalidatable
    {
        void InvalidateSurface();
        bool EnableTouchEvents { get; set; }
        IRenderer Renderer { get; }
    }
}
