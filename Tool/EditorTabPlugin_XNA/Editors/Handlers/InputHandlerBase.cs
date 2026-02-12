using System.Windows.Forms;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace Gum.Wireframe.Editors.Handlers;

/// <summary>
/// Base class for input handlers providing common functionality.
/// </summary>
public abstract class InputHandlerBase : IInputHandler
{
    protected EditorContext Context { get; }

    protected InputHandlerBase(EditorContext context)
    {
        Context = context;
    }

    public abstract int Priority { get; }

    public virtual bool IsActive { get; protected set; }

    public abstract bool HasCursorOver(float worldX, float worldY);

    public virtual Cursor? GetCursorToShow(float worldX, float worldY) => null;

    public virtual bool HandlePush(float worldX, float worldY)
    {
        if (HasCursorOver(worldX, worldY))
        {
            IsActive = true;
            OnPush(worldX, worldY);
            return true;
        }
        return false;
    }

    protected virtual void OnPush(float worldX, float worldY) { }

    public virtual void HandleDrag()
    {
        if (!IsActive) return;

        if (Context.GrabbedState.HasMovedEnough)
        {
            OnDrag();
        }
    }

    protected virtual void OnDrag() { }

    public virtual void HandleRelease()
    {
        if (!IsActive) return;

        OnRelease();
        IsActive = false;
    }

    protected virtual void OnRelease() { }

    public virtual void UpdateHover(float worldX, float worldY) { }

    public virtual void OnSelectionChanged() { }

    public virtual bool TryHandleDelete() => false;

    #region Helper Methods

    protected float GetCursorXChange()
    {
        return InputLibrary.Cursor.Self.XChange / Renderer.Self.Camera.Zoom;
    }

    protected float GetCursorYChange()
    {
        return InputLibrary.Cursor.Self.YChange / Renderer.Self.Camera.Zoom;
    }

    protected void MarkAsChanged()
    {
        Context.HasChangedAnythingSinceLastPush = true;
    }

    #endregion
}
