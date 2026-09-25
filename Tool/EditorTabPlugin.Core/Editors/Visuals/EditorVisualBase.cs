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
    /// Converts an overlay size in device-independent pixels to world units, so it stays the same
    /// on-screen size at any zoom and matches the OS display scale. Sizes only, never positions.
    /// </summary>
    protected float ToWorldOverlaySize(float overlaySize) => Context.ToWorldOverlaySize(overlaySize);

    #endregion
}
