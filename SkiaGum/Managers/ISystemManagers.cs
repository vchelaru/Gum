namespace SkiaGum.Managers
{
    public interface ISystemManagers
    {
        void InvalidateSurface();
        bool EnableTouchEvents { get; set; }
        RenderingLibrary.Graphics.Renderer Renderer { get; }
    }
}
