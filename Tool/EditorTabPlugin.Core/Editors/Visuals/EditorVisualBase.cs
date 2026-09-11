using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Collections.Generic;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Base class for visual components providing common functionality.
/// </summary>
public abstract class EditorVisualBase : IEditorVisual
{
    protected EditorContext Context { get; }
    protected Layer OverlayLayer { get; }

    private bool _visible = true;

    protected EditorVisualBase(EditorContext context)
    {
        Context = context;
        OverlayLayer = context.OverlayLayer;
    }

    public virtual bool Visible
    {
        get => _visible;
        set
        {
            if (_visible != value)
            {
                _visible = value;
                OnVisibilityChanged(value);
            }
        }
    }

    /// <summary>
    /// Called when visibility changes. Override to update child shape visibility.
    /// </summary>
    protected virtual void OnVisibilityChanged(bool isVisible) { }

    public virtual void Update() { }

    public virtual void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects) { }

    public abstract void Destroy();

    #region Helper Methods

    /// <summary>
    /// Returns the current camera zoom level.
    /// </summary>
    protected float Zoom => Renderer.Self.Camera.Zoom;

    /// <summary>
    /// Scales a size value to be consistent regardless of zoom level.
    /// </summary>
    protected float ScaleByZoom(float sizeAtNoZoom) => sizeAtNoZoom / Zoom;

    #endregion
}
