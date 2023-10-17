namespace SkiaGum.Managers
{
    public interface ISurfaceInvalidatable
    {
        void InvalidateSurface();
    }

    public interface ISystemManagers : ISurfaceInvalidatable
    {
        void InvalidateSurface();
        bool EnableTouchEvents { get; set; }
        RenderingLibrary.Graphics.Renderer Renderer { get; }
    }
}
