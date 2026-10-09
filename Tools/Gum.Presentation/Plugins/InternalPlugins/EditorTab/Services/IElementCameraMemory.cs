using Gum.DataTypes;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Remembers the Editor tab's camera per element for the session, so switching away from an
/// element and back returns to where the user was (#5854).
/// </summary>
public interface IElementCameraMemory
{
    /// <summary>
    /// Records the camera of the element being left if it moved, then returns the camera to restore
    /// for <paramref name="element"/>, or null to leave the camera where it is.
    /// </summary>
    CameraView? Show(ElementSave? element, CameraView current);

    /// <summary>Forgets every element's camera, as when another project loads.</summary>
    void Clear();

    /// <summary>Forgets <paramref name="element"/>'s camera, as when it is deleted.</summary>
    void Forget(ElementSave element);
}
