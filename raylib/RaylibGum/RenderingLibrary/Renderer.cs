using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum.RenderingLibrary;
public class Renderer : IRenderer
{
    Camera _camera = new Camera();
    public Camera Camera => _camera;

    List<Layer> _layers;
    ReadOnlyCollection<Layer> _layersReadOnly;
    public ReadOnlyCollection<Layer> Layers => _layersReadOnly;

    public Layer MainLayer => _layers[0];

    public Renderer()
    {
        _layers = new List<Layer>();
        _layersReadOnly = new ReadOnlyCollection<Layer>(_layers);
        _layers.Add(new Layer());

    }

    public void RenderLayer(ISystemManagers managers, Layer layer, bool prerender = true)
    {

    }
}
