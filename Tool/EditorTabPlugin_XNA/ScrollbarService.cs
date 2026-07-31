using Gum.Controls;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using WpfScrollBar = System.Windows.Controls.Primitives.ScrollBar;

namespace Gum.Plugins.ScrollBarPlugin;

public class ScrollbarService
{
    ScrollBarControlLogic scrollBarControlLogic;
    private readonly ISelectedState _selectedState;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IProjectManager _projectManager;

    public ScrollbarService(
        ISelectedState selectedState,
        IWireframeObjectManager wireframeObjectManager,
        IProjectManager projectManager)
    {
        _selectedState = selectedState;
        _wireframeObjectManager = wireframeObjectManager;
        _projectManager = projectManager;
    }
    
    public void HandleElementSelected(ElementSave obj)
    {

        GraphicalUiElement? ipso = null;

        if(obj != null)
        {
            ipso = _wireframeObjectManager.GetRepresentation(obj);
        }

        float minX = -_projectManager.GumProjectSave.DefaultCanvasWidth/2;
        float maxX = _projectManager.GumProjectSave.DefaultCanvasWidth;

        float minY = -_projectManager.GumProjectSave.DefaultCanvasHeight / 2;
        float maxY = _projectManager.GumProjectSave.DefaultCanvasHeight;


        if(ipso != null)
        {
            var asGue = ipso;

            List<IRenderableIpso> toLoop = new List<IRenderableIpso>();

            if(_selectedState.SelectedScreen != null)
            {
                toLoop.AddRange(asGue.ContainedElements);
            }
            else if(asGue.Children != null)
            {
                toLoop.AddRange(asGue.Children);
            }

            foreach(var item in toLoop)
            {
                UpdateMinMaxRecursively(item, ref minX, ref maxX, ref minY, ref maxY);
            }
        }

        scrollBarControlLogic.SetDisplayedArea((int)maxX, (int)maxY);

    }

    private void UpdateMinMaxRecursively(IRenderableIpso item, ref float minX, ref float maxX, ref float minY, ref float maxY)
    {
        minX = Math.Min(minX, item.GetAbsoluteLeft());
        maxX = Math.Max(maxX, item.GetAbsoluteRight());

        minY = Math.Min(minY, item.GetAbsoluteTop());
        maxY = Math.Max(maxY, item.GetAbsoluteBottom());

        if(item.Children != null)
        {
            // this could be an invalid instance
            foreach(var child in item.Children)
            {
                UpdateMinMaxRecursively(child, ref minX, ref maxX, ref minY, ref maxY);
            }
        }
    }

    /// <summary>
    /// The vertical scroll bar for the wireframe canvas. Null until
    /// <see cref="HandleWireframeInitialized"/> runs; the caller is responsible for placing it in
    /// the editor's visual tree.
    /// </summary>
    public WpfScrollBar? VerticalScrollBar { get; private set; }

    /// <summary>
    /// The horizontal scroll bar for the wireframe canvas. See <see cref="VerticalScrollBar"/>.
    /// </summary>
    public WpfScrollBar? HorizontalScrollBar { get; private set; }

    /// <summary>
    /// Creates the wireframe canvas's scroll bars and the logic driving them, over
    /// <paramref name="scrollSurface"/>.
    /// </summary>
    public void HandleWireframeInitialized(IScrollSurface scrollSurface)
    {
        VerticalScrollBar = new WpfScrollBar { Orientation = Orientation.Vertical };
        HorizontalScrollBar = new WpfScrollBar { Orientation = Orientation.Horizontal };

        scrollBarControlLogic = new ScrollBarControlLogic(
            new WpfScrollBarAdapter(HorizontalScrollBar),
            new WpfScrollBarAdapter(VerticalScrollBar),
            scrollSurface);
        scrollBarControlLogic.SetDisplayedArea(800, 600);
    }

    public void HandleCameraChanged()
    {
        scrollBarControlLogic.UpdateScrollBars();
        scrollBarControlLogic.UpdateScrollBarsToCameraPosition();
    }

    public void HandleXnaInitialized()
    {
        scrollBarControlLogic.Camera = global::RenderingLibrary.SystemManagers.Default.Renderer.Camera;
        scrollBarControlLogic.UpdateScrollBars();
    }

    public void HandleWireframeResized()
    {
        scrollBarControlLogic.UpdateScrollBars();
    }
}
