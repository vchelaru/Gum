using RenderingLibrary.Graphics;
using System;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;
public class LayerService
{
    private Layer? _overlayLayer;
    private Layer? _rulerLayer;

    public Layer OverlayLayer => _overlayLayer ?? throw CreateNotInitializedException();

    /// <summary>
    /// The layer holding the edited element's visuals. Null until <see cref="Initialize"/> runs;
    /// visuals created before then go on the default layer.
    /// </summary>
    public Layer? MainEditorLayer { get; private set; }

    public Layer RulerLayer => _rulerLayer ?? throw CreateNotInitializedException();

    public LayerService()
    {
    }

    public void Initialize()
    {
        MainEditorLayer = Renderer.Self.AddLayer();
        MainEditorLayer.Name = "Main Editor Layer";


        _overlayLayer = Renderer.Self.AddLayer();
        _overlayLayer.Name = "Overlay Layer";

        _rulerLayer = Renderer.Self.AddLayer();
        _rulerLayer.LayerCameraSettings = new LayerCameraSettings();
        _rulerLayer.LayerCameraSettings.IsInScreenSpace = true;
        _rulerLayer.Name = "Ruler Layer";
    }

    private static InvalidOperationException CreateNotInitializedException() =>
        new InvalidOperationException("LayerService.Initialize must be called before its layers are used.");
}
