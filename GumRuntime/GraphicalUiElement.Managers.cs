using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.RenderingLibrary;
using GumDataTypes.Variables;

using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.ComponentModel;
using ToolsUtilitiesStandard.Helpers;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using GumRuntime;
using Gum.Collections;

#if FRB

using InteractiveGue = Gum.Wireframe.GraphicalUiElement;
#endif


#if !FRB
using Gum.StateAnimation.Runtime;
#endif

namespace Gum.Wireframe;

public partial class GraphicalUiElement
{

    partial void CustomAddToManagers();

    /// <summary>
    /// Adds this as a renderable to the default SystemManagers if not already added. If already added,
    /// this does not perform any operations — it can be safely called multiple times.
    /// This method exists for FlatRedBall compatibility. In all other environments, use the
    /// <c>AddToRoot</c> extension method instead, which adds this element to the GumService root container.
    /// </summary>

#if !FRB
    [Obsolete("Use the AddToRoot extension method instead (e.g. myElement.AddToRoot()). " +
        "AddToRoot adds this element to the GumService root container, which is the recommended " +
        "approach for MonoGame, KNI, FNA, and raylib projects.")]
#endif
    public virtual void AddToManagers()
    {

        AddToManagers(ISystemManagers.Default, null);

    }

    /// <summary>
    /// Adds this as a renderable to the specified <paramref name="managers"/> on the given
    /// <paramref name="layer"/> if not already added. If already added, this does not perform
    /// any operations — it can be safely called multiple times, but calling it multiple times
    /// will not move this to a different layer.
    /// This overload is needed when multiple Gum instances run simultaneously (e.g. SkiaGum),
    /// each with their own SystemManagers.
    /// </summary>
    /// <param name="managers">The SystemManagers instance to register with.</param>
    /// <param name="layer">The layer to add to, or <c>null</c> for the default layer.</param>
    public virtual void AddToManagers(ISystemManagers managers, Layer? layer = null)
    {
#if FULL_DIAGNOSTICS
        if (managers == null)
        {
            throw new ArgumentNullException("managers cannot be null");
        }
#endif
        // If mManagers isn't null, it's already been added
        if (mManagers == null)
        {
            mLayer = layer;
            mManagers = managers;

            AddContainedRenderableToManagers(managers, layer);

            RecursivelyAddIManagedChildren(this);

            // Custom should be called before children have their Custom called
            CustomAddToManagers();

            // that means this is a screen, so the children need to be added directly to managers
            if (this.mContainedObjectAsIpso == null)
            {
                AddChildren(managers, layer);
            }
            else
            {
                CustomAddChildren();
            }
        }
    }

    /// <summary>
    /// Sets this instance's <see cref="Managers"/> so that <see cref="EffectiveManagers"/>
    /// resolves for this element and its children, without registering any renderable with a
    /// Layer — unlike <see cref="AddToManagers(ISystemManagers, Layer)"/>. Use this for
    /// containers that are drawn through some other, external render pass rather than Gum's
    /// own Renderer.Draw pipeline, but which still need EffectiveManagers-dependent behavior
    /// (such as closing a ComboBox/MenuItem popup on an outside click) to work correctly.
    /// Do not also call <see cref="AddToManagers(ISystemManagers, Layer)"/> on the same
    /// instance afterward expecting it to now register for real Gum-driven drawing — its
    /// "already added" guard treats <see cref="Managers"/> being non-null as sufficient, so
    /// it will silently no-op.
    /// </summary>
    /// <param name="managers">The SystemManagers instance to resolve as this element's EffectiveManagers.</param>
    public void AttachManagersOnly(ISystemManagers managers)
    {
        mManagers = managers;
    }

    private static void RecursivelyAddIManagedChildren(GraphicalUiElement gue)
    {
        if (gue.ElementSave != null && gue.ElementSave is ScreenSave)
        {

            //Recursively add children to the managers
            foreach (var child in gue.mWhatThisContains)
            {
                if (child is IManagedObject managedObject)
                {
                    managedObject.AddToManagers();
                }
                RecursivelyAddIManagedChildren(child);
            }
        }
        else if (gue.Children != null)
        {
            foreach (var child in gue.Children)
            {
                if (child is IManagedObject managedObject)
                {
                    managedObject.AddToManagers();
                }

                RecursivelyAddIManagedChildren(child);

            }
        }
    }

    private void CustomAddChildren()
    {
        foreach (var child in this.mWhatThisContains)
        {
            child.mManagers = this.mManagers;
            child.CustomAddToManagers();

            child.CustomAddChildren();
        }
    }

    private void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            // Index rather than foreach: NotifyCollectionChangedEventArgs.NewItems is a non-generic
            // IList whose enumerator boxes, which is measurable under add/remove churn (#1934).
            var newItems = e.NewItems;
            if (newItems != null)
            {
                for (int i = 0; i < newItems.Count; i++)
                {
                    var newItem = newItems[i];
#if FULL_DIAGNOSTICS
                    if (newItem == null)
                    {
                        throw new InvalidOperationException($"Attempting to add a null child to {this}");
                    }
                    if(newItem == this)
                    {
                        throw new InvalidOperationException($"{this} cannot be added as a child of itself");
                    }
#endif
                    var ipso = (GraphicalUiElement)newItem!;

                    if (ipso.Parent != this && !ipso._isSettingParent)
                    {
                        ipso.Parent = this;

                    }
                }

            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Move)
        {
            // for now let's just do a layout on this and the children
            UpdateLayout();
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            var oldItems = e.OldItems;
            if (oldItems != null)
            {
                // Clear() removes every child in one event; suspend so this lays out once, not per child.
                var shouldSuspend = oldItems.Count > 1 && !mIsLayoutSuspended;
                if (shouldSuspend)
                {
                    SuspendLayout();
                }
                for (int i = 0; i < oldItems.Count; i++)
                {
                    var child = (GraphicalUiElement)oldItems[i]!;
                    if (child.Parent == this && !child._isSettingParent)
                    {
                        child.Parent = null;
                    }
                }
                if (shouldSuspend)
                {
                    ResumeLayout();
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            var oldItems = e.OldItems;
            if (oldItems != null)
            {
                for (int i = 0; i < oldItems.Count; i++)
                {
                    var child = (GraphicalUiElement)oldItems[i]!;
                    if (child.Parent == this && !child._isSettingParent)
                    {
                        child.Parent = null;
                    }
                }
            }
            else
            {
#if FULL_DIAGNOSTICS
                var message = "STOP!!! The GraphicalUiElement " + this + " has been reset, but the Children ObservableCollection " +
                    "did not include e.OldItems, so the old children cannot have their Parent set to null. This can cause memory leaks through " +
                    "events, and other references. You should consider implementing a Children backing field that instead loops through and removes each child through a .Remove call.";

                System.Diagnostics.Debug.WriteLine(message);
#endif
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Replace)
        {
            var oldItems = e.OldItems;
            if (oldItems != null)
            {
                for (int i = 0; i < oldItems.Count; i++)
                {
                    var child = (GraphicalUiElement)oldItems[i]!;
                    if (child.Parent == this && !child._isSettingParent)
                    {
                        child.Parent = null;
                    }
                }
            }
            var newItems = e.NewItems;
            if (newItems != null)
            {
                for (int i = 0; i < newItems.Count; i++)
                {
                    var ipso = (IRenderableIpso)newItems[i]!;
                    if (ipso.Parent != this)
                    {
                        ipso.Parent = this;

                    }
                }
            }
        }
    }

    private void AddChildren(ISystemManagers managers, Layer? layer)
    {
        // In a simple situation we'd just loop through the
        // ContainedElements and add them to the manager.  However,
        // this means that the container will dictate the Layer that
        // its children reside on.  This is not what we want if we have
        // two children, one of which is attached to the other, and the parent
        // instance clips its children.  Therefore, we should make sure that we're
        // only adding direct children and letting instances handle their own children

        if (this.ElementSave is ScreenSave || this.Children == null)
        {

            //Recursively add children to the managers
            foreach (var child in this.mWhatThisContains)
            {
                // July 27, 2014
                // Is this an unnecessary check?
                // if (child is GraphicalUiElement)
                {
                    // December 1, 2014
                    // I think that when we
                    // add a screen we should
                    // add all of the children of
                    // the screen.  There's nothing
                    // "above" that.
                    if (child.Parent == null || child.Parent == this)
                    {
                        child.AddToManagers(managers, layer);
                    }
                    else
                    {
                        child.mManagers = this.mManagers;

                        child.CustomAddToManagers();

                        child.CustomAddChildren();
                    }
                }
            }
        }
        else if (this.Children != null)
        {
            foreach (var child in this.Children)
            {
                if (child is GraphicalUiElement)
                {
                    if (child.Parent == null || child.Parent == this)
                    {
                        child.AddToManagers(managers, layer);
                    }
                    else
                    {
                        child.mManagers = this.mManagers;

                        child.CustomAddToManagers();

                        child.CustomAddChildren();
                    }
                }
            }

            // If a Component contains a child and that child is parented to the screen bounds then we should still add it
            foreach (var child in this.mWhatThisContains)
            {
                var childGue = child as GraphicalUiElement;

                // We'll check if this child has a parent, and if that parent isn't part of this component. If not, then
                // we'll add it
                if (child.Parent != null && this.mWhatThisContains.Contains(child.Parent) == false)
                {
                    childGue.AddToManagers(managers, layer);
                }
                else
                {
                    childGue.mManagers = this.mManagers;

                    childGue.CustomAddToManagers();

                    childGue.CustomAddChildren();
                }
            }
        }
    }


    private void AddContainedRenderableToManagers(ISystemManagers managers, Layer? layer)
    {
        // This may be a Screen
        if (mContainedObjectAsIpso != null)
        {
            AddRenderableToManagers?.Invoke(mContainedObjectAsIpso, managers, layer);

        }
    }

    // todo:  This should be called on instances and not just on element saves.  This is messing up animation
    public void AddExposedVariable(string variableName, string underlyingVariable)
    {
        mExposedVariables[variableName] = underlyingVariable;
    }

    public bool IsExposedVariable(string variableName)
    {
        return this.mExposedVariables.ContainsKey(variableName);
    }

    partial void CustomRemoveFromManagers();

    public void MoveToLayer(Layer? layer)
    {
        var layerToRemoveFrom = mLayer;
        if (mLayer == null && mManagers != null)
        {
            layerToRemoveFrom = mManagers.Renderer.Layers[0];
        }

        var layerToAddTo = layer;
        if (layerToAddTo == null)
        {
            layerToAddTo = mManagers?.Renderer.Layers[0];
        }

        bool hasContainedObject = mContainedObjectAsIpso != null;
        if (hasContainedObject)
        {
#if !FRB
            // FRB1's PositionedObjectGueWrapper (GumCoreShared\FlatRedBall\Embedded\PositionedObjectGueWrapper.cs)
            // deliberately parents every FRB-attached Gum object to a synthetic, never-added-to-managers
            // GraphicalUiElement whose sole job is translating FRB world position into Gum coordinates. That
            // proxy is never itself drawn, so there is no double-render risk - this guard only makes sense
            // for standalone Gum, where a real structural parent is the thing walking Children during Draw.
            if (Parent != null)
            {
                var parentDescription = string.IsNullOrEmpty(Parent.Name)
                    ? $"an unnamed {Parent.GetType().Name}"
                    : Parent.Name;
                throw new InvalidOperationException(
                    $"Cannot move {this} to a different layer because it is parented to {parentDescription}. " +
                    "MoveToLayer only re-homes top-level layer members; a parented element is already " +
                    "drawn through its parent's render tree, so also adding it to a layer would double-render it. " +
                    "Remove it from its parent first, or use AddToManagers instead.");
            }
#endif
            if(layerToAddTo == null)
            {
                throw new InvalidOperationException($"Cannot move {this} to a different layer because it is not currently on a layer and no layer was provided");
            }
            if (layerToRemoveFrom != null)
            {
                layerToRemoveFrom.Remove(mContainedObjectAsIpso!);
            }
            layerToAddTo.Add(mContainedObjectAsIpso!);
            mLayer = layerToAddTo;
        }
        else
        {
            // move all contained objects:
            foreach (var containedInstance in this.ContainedElements)
            {
                var containedAsGue = containedInstance as GraphicalUiElement;
                // If it's got a parent, the parent will handle it
                if (containedAsGue.Parent == null)
                {
                    containedAsGue.MoveToLayer(layer);
                }
            }

        }
    }

    public virtual void RemoveFromManagers()
    {
        foreach (var child in this.mWhatThisContains)
        {
            if (child is GraphicalUiElement)
            {
                (child as GraphicalUiElement).RemoveFromManagers();
            }
        }

        // if mManagers is null, then it was never added to the managers
        if (mManagers != null)
        {
            RemoveRenderableFromManagers?.Invoke(mContainedObjectAsIpso!, mManagers);

            CustomRemoveFromManagers();

            mManagers = null;
        }
    }
}
