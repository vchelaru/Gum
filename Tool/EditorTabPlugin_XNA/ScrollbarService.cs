using Gum.Controls;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Plugins.InternalPlugins.EditorTab.Views;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

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

    public void HandleWireframeInitialized(WireframeControl wireframeControl1, System.Windows.Forms.Panel gumEditorPanel)
    {
        // this used to be in MainWindow.cs,
        // but was moved to a plugin. This changes
        // the order of this code which had a comment
        // about needing to be done in a particular order
        // but it seems to be working okay. Adding this comment
        // just in case the order does in fact matter.
        ThemedScrollBar verticalScrollBar = new() { Orientation = ScrollOrientationEx.Vertical, Dock = DockStyle.Right };
        gumEditorPanel.Controls.Add(verticalScrollBar);

        ThemedScrollBar horizontalScrollBar = new() { Orientation = ScrollOrientationEx.Horizontal, Dock = DockStyle.Bottom };
        gumEditorPanel.Controls.Add(horizontalScrollBar);

        scrollBarControlLogic = new ScrollBarControlLogic(
            horizontalScrollBar,
            verticalScrollBar,
            new ControlScrollSurfaceAdapter(wireframeControl1));
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
