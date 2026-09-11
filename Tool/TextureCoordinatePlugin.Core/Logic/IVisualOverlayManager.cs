using RenderingLibrary;

namespace TextureCoordinateSelectionPlugin.Logic;

public interface IVisualOverlayManager
{
    void Initialize(SystemManagers systemManagers);
    void Refresh();
}
