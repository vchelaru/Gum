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

    #region Get/Add Child/Element

    public void AddChild(GraphicalUiElement child) => this.Children.Add(child);

    public void RemoveChild(GraphicalUiElement child) => this.Children.Remove(child);

#if !FRB && (NET6_0_OR_GREATER || NETSTANDARD2_1)
    /// <summary>
    /// Adds this element as a child of the active runtime's root container (resolved via
    /// <see cref="IGumService.Default"/>), making it a top-level element that will be rendered
    /// and receive layout updates. This is the recommended way to display a root-level
    /// element — prefer this over the obsolete <c>AddToManagers</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no <see cref="IGumService"/> has been initialized.
    /// </exception>
    public void AddToRoot()
    {
        if (IGumService.Default?.IsInitialized != true)
        {
            throw new InvalidOperationException(
                "Cannot call AddToRoot because IGumService.Default is not initialized — " +
                "did you remember to initialize Gum first (GumService.Default.Initialize)?");
        }
        IGumService.Default.Root.Children.Add(this);
    }

    /// <summary>
    /// Removes this element from its parent, effectively removing it from the visual tree.
    /// This reverses a previous <see cref="AddToRoot"/> call.
    /// </summary>
    public void RemoveFromRoot()
    {
        Parent = null;
    }
#endif

    /// <summary>
    /// Searches recursively for and returns a GraphicalUiElement in this instance by name. Returns null
    /// if not found.
    /// </summary>
    /// <param name="name">The case-sensitive name to search for.</param>
    /// <returns>The found GraphicalUiElement, or null if no match is found.</returns>
    public GraphicalUiElement? GetGraphicalUiElementByName(string name)
    {
        var containsDots = ToolsUtilities.StringFunctions.ContainsNoAlloc(name, '.');
        if (containsDots)
        {
            // rare, so we can do allocation calls here:
            var indexOfDot = name.IndexOf('.');

            var prefix = name.Substring(0, indexOfDot);

            GraphicalUiElement? container = null;
            for (int i = mWhatThisContains.Count - 1; i > -1; i--)
            {
                var item = mWhatThisContains[i];
                if (item.name == prefix)
                {
                    container = item;
                    break;
                }
            }

            var suffix = name.Substring(indexOfDot + 1);

            return container?.GetGraphicalUiElementByName(suffix);
        }
        else
        {
            if (this.Children?.Count > 0 && mWhatThisContains.Count == 0)
            {
                // This is a regular item that hasn't had its mWhatThisContains populated
                return this.FindByName(name);
            }
            else
            {
                for (int i = mWhatThisContains.Count - 1; i > -1; i--)
                {
                    var item = mWhatThisContains[i];
                    if (item.name == name)
                    {
                        return item;
                    }
                    // This causes a problem - if we do this recursively we will find the wrong objects
                    // like if we have a ListBox with a Background but also a button with a Background. This
                    // could find the button background...
                    // Either we don't do this recurisvely or we do top level first, then recursive. This hasn't
                    // been recursive for a long time so maybe we need to keep it not recursive for now...

                    //else
                    //{
                    //    var foundChild = item.GetChildByNameRecursively(name) as GraphicalUiElement;

                    //    if(foundChild != null)
                    //    {
                    //        return foundChild;
                    //    }
                    //}
                }
            }
        }

        return null;
    }


    /// <summary>
    /// Performs a recursive search for graphical UI elements, where eacn name in the parameters
    /// is the name of a GraphicalUiElement one level deeper than the last.
    /// </summary>
    /// <param name="names">The names to search for, allowing retrieval multiple levels deep.</param>
    /// <returns>The found element, or null if no match is found.</returns>
    public GraphicalUiElement? GetGraphicalUiElementByName(params string[] names)
    {
        if (names.Length > 0)
        {
            var directChild = GetGraphicalUiElementByName(names[0]);

            if (names.Length == 1)
            {
                return directChild;
            }
            else
            {
                var subArray = names.Skip(1).ToArray();

                return directChild?.GetGraphicalUiElementByName(subArray);
            }
        }
        return null;
    }

    public GraphicalUiElement? GetChildByName(string name)
    {
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (child.Name == name)
            {
                return child;
            }
        }
        return null;
    }

    public GraphicalUiElement? GetChildByType(Type type)
    {
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (child.GetType().Equals(type))
            {
                return child;
            }
        }
        return null;
    }

    [Obsolete("Use FindByName(name) instead. The new extension method is shallowest-first like this method, returns the same type, and composes with LINQ via Descendants().")]
    public GraphicalUiElement? GetChildByNameRecursively(string name)
    {
        return GetChildByName(Children, name);
    }

    private GraphicalUiElement? GetChildByName(ObservableCollection<GraphicalUiElement> children, string name)
    {
        // This is a recursive call, but we want to find the most-shallow child
        // first before going deeper. This is important for controls like ListBox
        // which may have a FocusedIndicator at the top level, and each individual
        // ListBoxItem has a FocusedIndicator too.
        foreach (var child in children)
        {
            if (child.Name == name)
            {
                return child;
            }
        }

        foreach (var child in children)
        {
            var subChild = GetChildByName(child.Children, name);
            if (subChild != null)
            {
                return subChild;
            }
        }
        return null;
    }

    [Obsolete("Use Find<T>() instead, or Descendants().OfType<T>().FirstOrDefault() for non-generic cases. Note: Find<T>() matches subclasses (is-T semantics); this method matches exact type only.")]
    public GraphicalUiElement? GetChildByTypeRecursively(Type type)
    {
        return GetChildByType(Children, type);
    }

    private GraphicalUiElement? GetChildByType(ObservableCollection<GraphicalUiElement> children, Type type)
    {
        // This is a recursive call, but we want to find the most-shallow child
        // first before going deeper. This is important for controls like ListBox
        // which may have a FocusedIndicator at the top level, and each individual
        // ListBoxItem has a FocusedIndicator too.
        foreach (var child in children)
        {
            if (child.GetType().Equals(type))
            {
                return child;
            }
        }

        foreach (var child in children)
        {
            var subChild = GetChildByType(child.Children, type);
            if (subChild != null)
            {
                return subChild;
            }
        }
        return null;
    }

    [Obsolete("Use Ancestors().FirstOrDefault(a => a.Name == name) instead.")]
    public GraphicalUiElement? GetParentByNameRecursively(string name)
    {
        return GetParentByName(this, name);
    }

    private GraphicalUiElement? GetParentByName(GraphicalUiElement element, string name)
    {
        if (element.Parent != null)
        {
            if (element.Parent.Name == name)
            {
                return element.Parent as GraphicalUiElement;
            }
            else
            {
                return GetParentByName(element.Parent, name);
            }
        }
        else
        {
            return null;
        }
    }

    [Obsolete("Use Ancestors().OfType<T>().FirstOrDefault() instead. Note: OfType<T>() matches subclasses (is-T semantics); this method matches exact type only.")]
    public GraphicalUiElement? GetParentByTypeRecursively(Type type)
    {
        return GetParentByType(this, type);
    }

    private GraphicalUiElement? GetParentByType(GraphicalUiElement element, Type type)
    {
        if (element.Parent != null)
        {
            if (element.Parent.GetType().Equals(type))
            {
                return element.Parent as GraphicalUiElement;
            }
            else
            {
                return GetParentByType(element.Parent, type);
            }
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Populates a list with all the children matching the argument type. Performs the search in a recursive fashion.
    /// </summary>
    /// <param name="listToFill">List to populate. The type to search for is inferred from the element type and must be an <see cref="IRenderableIpso"/>.
    /// The user has the responsability of instantiating and clearing this list.</param>
    [Obsolete("Use listToFill.AddRange(this.Descendants().OfType<T>()) instead. Note: Descendants().OfType<T>() matches subclasses (is-T semantics); this method matches exact type only.")]
    public void FillListWithChildrenByTypeRecursively<T>(List<T> listToFill) where T : GraphicalUiElement
    {
        FillListWithChildrenByType(Children, listToFill);
    }

    /// <summary>
    /// Returns a list with all the children matching the argument type. Performs the search in a recursive fashion.
    /// </summary>
    /// <typeparam name="T">Type to search for. Must be an <see cref="GraphicalUiElement"/>.</typeparam>
    /// <returns></returns>
    [Obsolete("Use this.Descendants().OfType<T>().ToList() instead. Note: OfType<T>() matches subclasses (is-T semantics); this method matches exact type only.")]
    public List<T> FillListWithChildrenByTypeRecursively<T>() where T : GraphicalUiElement
    {
        var list = new List<T>();
#pragma warning disable CS0618
        FillListWithChildrenByTypeRecursively(list);
#pragma warning restore CS0618
        return list;
    }

    private void FillListWithChildrenByType<T>(ObservableCollection<GraphicalUiElement> children, List<T> listToFill) where T : GraphicalUiElement
    {
        foreach (var child in children)
        {
            if (child.GetType().Equals(typeof(T)))
            {
                listToFill.Add((T)child);
            }

            FillListWithChildrenByType(child.Children, listToFill);
        }
    }

    #endregion
}
