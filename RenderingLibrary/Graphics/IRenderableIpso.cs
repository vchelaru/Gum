#if SKIA
using Gum.GueDeriving;
using SkiaGum.Renderables;
using SkiaSharp;
#endif
using Gum;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace RenderingLibrary.Graphics;

public interface IRenderableIpso : IRenderable, IPositionedSizedObject, IVisible
{
    bool IsRenderTarget { get; }

    int Alpha { get; }
    bool ClipsChildren { get;  }
    new IRenderableIpso? Parent { get; set; }
    ObservableCollection<IRenderableIpso> Children { get; }
    ColorOperation ColorOperation { get; }

    void SetParentDirect(IRenderableIpso? newParent);

}

/// <summary>
/// Implemented by renderables that can act as a render target: their children are drawn into an
/// offscreen texture which is then blitted back to the screen, optionally post-processed. This is
/// the shared home for render-target state that is NOT universal to every renderable, so the
/// <c>Renderer</c> can drive render-target rendering without knowing the concrete renderable type.
/// Both the runtime container renderable (<see cref="RenderableBase"/>, e.g.
/// <c>InvisibleRenderable</c>) and the Gum editor's container renderable
/// (<see cref="RenderingLibrary.Math.Geometry.LineRectangle"/>, which carries the editor outline)
/// implement it. The universal render-target inputs — <c>IsRenderTarget</c> and <c>Alpha</c> —
/// already live on <see cref="IRenderableIpso"/>; this interface is the place to add further
/// render-target-only state in the future.
/// </summary>
public interface IRenderTargetRenderable
{
    /// <summary>
    /// Optional post-process effect applied when the render target's cached texture is blitted back
    /// to the screen (issue #816). Typed <c>object?</c> so the shared rendering layer stays
    /// backend-agnostic; the xnalike <c>Renderer</c> casts it to a MonoGame <c>Effect</c>. Null
    /// means the container is blitted unshaded.
    /// </summary>
    object? RenderTargetEffect { get; set; }
}

/// <summary>
/// Implemented by a renderable (or its runtime wrapper) that displays another container's baked
/// render-target texture as its pixel source. The <c>Renderer</c>'s render-target detection walk
/// (<c>CollectReferencedRenderTargets</c>) tests for this interface to discover which containers
/// must bake this frame — even invisible ones, as long as a visible referencer points at them.
/// Type-neutral (the source is an <see cref="IRenderableIpso"/>, not a backend texture type) so it
/// lives here in GumCommon and every backend — xnalike, raylib, Skia — implements the one path.
/// </summary>
public interface IRenderTargetTextureReferencer
{
    IRenderableIpso? RenderTargetTextureSource { get; }
}


public static class IRenderableIpsoExtensions
{
    public static bool IsInRenderTargetRecursively(this IRenderableIpso ipso)
    {
        if(ipso.IsRenderTarget)
        {
            return true;
        }
        else if(ipso.Parent != null)
        {
            return ipso.Parent.IsInRenderTargetRecursively();
        }
        else
        {
            return false;
        }
    }
}

/// <summary>
/// An ObservableCollection that throws on any mutation attempt.
/// Used as a shared empty sentinel so callers never receive null.
/// </summary>
public class FrozenObservableCollection<T> : ObservableCollection<T>
{
    protected override void InsertItem(int index, T item) =>
        throw new NotSupportedException("Collection is read-only.");
    protected override void RemoveItem(int index) =>
        throw new NotSupportedException("Collection is read-only.");
    protected override void SetItem(int index, T item) =>
        throw new NotSupportedException("Collection is read-only.");
    protected override void ClearItems() =>
        throw new NotSupportedException("Collection is read-only.");
    protected override void MoveItem(int oldIndex, int newIndex) =>
        throw new NotSupportedException("Collection is read-only.");
}

public class ObservableCollectionNoReset<T> : ObservableCollection<T>
{
    protected override void ClearItems()
    {
        List<T> removed = new List<T>(this);
        if(this.Count > 0)
        {
            base.ClearItems();
            var args = new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, 
                removed,
                0);
            base.OnCollectionChanged(args);
        }
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Reset)
            base.OnCollectionChanged(e);
    }

    /// <summary>
    /// Inserts an item without raising CollectionChanged. Used by wrappers that mirror this
    /// collection and raise their own notification, so an event here would only allocate args for
    /// a handler that ignores them.
    /// </summary>
    public void InsertWithoutNotification(int index, T item) => Items.Insert(index, item);

    /// <summary>
    /// Removes the item at the specified index without raising CollectionChanged. See
    /// <see cref="InsertWithoutNotification"/> for when to use this.
    /// </summary>
    public void RemoveAtWithoutNotification(int index) => Items.RemoveAt(index);

    /// <summary>
    /// Replaces the item at the specified index without raising CollectionChanged. See
    /// <see cref="InsertWithoutNotification"/> for when to use this.
    /// </summary>
    public void SetWithoutNotification(int index, T item) => Items[index] = item;

    /// <summary>
    /// Removes all items without raising CollectionChanged. See
    /// <see cref="InsertWithoutNotification"/> for when to use this.
    /// </summary>
    public void ClearWithoutNotification() => Items.Clear();

    /// <summary>
    /// Moves an item without raising CollectionChanged. See
    /// <see cref="InsertWithoutNotification"/> for when to use this.
    /// </summary>
    public void MoveWithoutNotification(int oldIndex, int newIndex)
    {
        T item = Items[oldIndex];
        Items.RemoveAt(oldIndex);
        Items.Insert(newIndex, item);
    }
}