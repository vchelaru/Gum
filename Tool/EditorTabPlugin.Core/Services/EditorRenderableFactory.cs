using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Creates the editor canvas's renderable for a standard element base type. Defers to the runtime's
/// <see cref="FallbackRenderableFactory"/>, then applies the editor-only look: a Container's outline
/// is dotted and painted the project's outline color (issue #4849).
/// </summary>
public class EditorRenderableFactory
{
    private readonly IProjectState _projectState;
    private readonly List<WeakReference<LineRectangle>> _outlines = new();
    private bool _areOutlinesHiddenForExport;
    private readonly ConditionalWeakTable<LineRectangle, object> _outlinesOfOtherElements = new();

    public EditorRenderableFactory(IProjectState projectState)
    {
        _projectState = projectState;
    }

    /// <summary>
    /// Hides every Container/Component outline this factory made, for a frame that must show only
    /// what a game would draw (Export as Image). Uses <see cref="LineRectangle.LocalVisible"/> so
    /// the outline's children keep drawing.
    /// </summary>
    public bool AreOutlinesHiddenForExport
    {
        get => _areOutlinesHiddenForExport;
        set
        {
            _areOutlinesHiddenForExport = value;
            _outlines.RemoveAll(reference => !reference.TryGetTarget(out _));
            foreach (WeakReference<LineRectangle> reference in _outlines)
            {
                if (reference.TryGetTarget(out LineRectangle? outline))
                {
                    outline.LocalVisible = IsOutlineShown(outline);
                }
            }
        }
    }

    private bool IsOutlineShown(LineRectangle outline) =>
        !_areOutlinesHiddenForExport && !_outlinesOfOtherElements.TryGetValue(outline, out _);

    /// <summary>
    /// Shows only the outlines of containers defined in the element being edited (<paramref name="root"/>
    /// itself, or an instance whose ElementGueContainingThis is <paramref name="root"/>). A container
    /// that lives inside a nested component instance (the clipping container inside a TextBox on a
    /// screen) is hidden, so it only outlines while that component is the one being edited. Call after
    /// every build of a root; export hiding still wins.
    /// </summary>
    public void ShowOnlyOutlinesOwnedBy(GraphicalUiElement root)
    {
        ApplyOwnership(root, root);
    }

    private void ApplyOwnership(GraphicalUiElement root, GraphicalUiElement current)
    {
        if (current.RenderableComponent is LineRectangle outline)
        {
            bool owned = current == root || current.ElementGueContainingThis == root;
            _outlinesOfOtherElements.Remove(outline);
            if (!owned)
            {
                _outlinesOfOtherElements.Add(outline, this);
            }
            if (_outlines.Exists(reference => reference.TryGetTarget(out LineRectangle? tracked) && tracked == outline))
            {
                outline.LocalVisible = IsOutlineShown(outline);
            }
        }
        foreach (GraphicalUiElement contained in current.ContainedElements)
        {
            ApplyOwnership(root, contained);
        }
    }

    public IRenderableIpso? CreateRenderableForType(string type, ISystemManagers? managers)
    {
        IRenderable? renderable = FallbackRenderableFactory.TryHandleAsBaseType(type, managers);

        // The fallback returns a LineRectangle for "Rectangle" too; only a Container outline is dotted.
        if (type is "Container" or "Component" && renderable is LineRectangle outline)
        {
            outline.IsDotted = true;
            outline.Color = Color.FromArgb(
                255,
                _projectState.OutlineColorR,
                _projectState.OutlineColorG,
                _projectState.OutlineColorB);
            outline.LocalVisible = IsOutlineShown(outline);
            _outlines.Add(new WeakReference<LineRectangle>(outline));
        }

        return renderable as IRenderableIpso;
    }
}
