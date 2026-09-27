using Gum.Input;

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

    public virtual GumCursorKind? GetCursorToShow(float worldX, float worldY) => null;

    public virtual bool HandlePush(float worldX, float worldY)
    {
        if (Context.IsSelectionLocked())
        {
            return false;
        }
        // Selecting a category (rather than a state) leaves no state for an edit to write to.
        if (Context.SelectedState.SelectedStateSave == null)
        {
            return false;
        }
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
            Context.GrabbedState.BeginDragFrame();
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

    /// <summary>
    /// This drag frame's cursor movement in world units. Handlers read this rather than the
    /// cursor's own frame change, which leaves out the movement made inside the dead zone.
    /// </summary>
    protected float GetCursorXChange()
    {
        return Context.GrabbedState.DragXChange / Context.Camera.Zoom;
    }

    /// <inheritdoc cref="GetCursorXChange"/>
    protected float GetCursorYChange()
    {
        return Context.GrabbedState.DragYChange / Context.Camera.Zoom;
    }

    protected void MarkAsChanged()
    {
        Context.HasChangedAnythingSinceLastPush = true;
    }

    #endregion
}
