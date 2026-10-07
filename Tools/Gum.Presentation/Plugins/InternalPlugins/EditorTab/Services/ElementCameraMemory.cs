using System.Collections.Generic;
using Gum.DataTypes;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>Where the Editor tab's camera looks: its position and zoom.</summary>
public readonly record struct CameraView(float X, float Y, int ZoomPercent);

/// <summary>
/// Session-only <see cref="IElementCameraMemory"/>. An element gets a camera only when the camera
/// moved while it was shown; one never moved keeps whatever the camera shows.
/// </summary>
public class ElementCameraMemory : IElementCameraMemory
{
    private readonly Dictionary<ElementSave, CameraView> _views;
    private ElementSave? _shownElement;
    private CameraView _viewWhenShown;

    public ElementCameraMemory()
    {
        _views = new Dictionary<ElementSave, CameraView>();
    }

    /// <inheritdoc/>
    public CameraView? Show(ElementSave? element, CameraView current)
    {
        if (element == _shownElement)
        {
            return null;
        }

        if (_shownElement != null && current != _viewWhenShown)
        {
            _views[_shownElement] = current;
        }

        _shownElement = element;
        if (element != null && _views.TryGetValue(element, out CameraView remembered))
        {
            _viewWhenShown = remembered;
            return remembered;
        }

        _viewWhenShown = current;
        return null;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _views.Clear();
        _shownElement = null;
    }

    /// <inheritdoc/>
    public void Forget(ElementSave element)
    {
        _views.Remove(element);
        if (element == _shownElement)
        {
            _shownElement = null;
        }
    }
}
