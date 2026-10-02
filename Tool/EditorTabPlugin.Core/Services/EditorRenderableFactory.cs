using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

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
                    outline.LocalVisible = !value;
                }
            }
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
            outline.LocalVisible = !_areOutlinesHiddenForExport;
            _outlines.Add(new WeakReference<LineRectangle>(outline));
        }

        return renderable as IRenderableIpso;
    }
}
