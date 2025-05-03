using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;
public class LayerService
{
    public Layer OverlayLayer { get; private set; }
    public Layer MainEditorLayer { get; private set; }

    public Layer RulerLayer { get; private set; }

    public LayerService()
    {
    }

    public void Initialize()
    {
        MainEditorLayer = Renderer.Self.AddLayer();
        MainEditorLayer.Name = "Main Editor Layer";


        OverlayLayer = Renderer.Self.AddLayer();
        OverlayLayer.Name = "Overlay Layer";

        RulerLayer = Renderer.Self.AddLayer();
        RulerLayer.LayerCameraSettings = new LayerCameraSettings();
        RulerLayer.LayerCameraSettings.IsInScreenSpace = true;
        RulerLayer.Name = "Ruler Layer";
    }
}
