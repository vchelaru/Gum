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

#if NET6_0_OR_GREATER
        public static ISystemManagers Default { get; set;  }
#endif
    }
}
