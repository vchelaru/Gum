using Gum;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary.Graphics;
public class Renderer : IRenderer
{
    /// <summary>
    /// Whether renderable objects should call Render
    /// on contained children. This is true by default, 
    /// results in a hierarchical rendering order.
    /// </summary>
    public static bool RenderUsingHierarchy = true;

    public static Renderer Self
    {
        get
        {
            // Why is this using a singleton instead of system managers default? This seems bad...

            //if (mSelf == null)
            //{
            //    mSelf = new Renderer();
            //}
            //return mSelf;
            if (SystemManagers.Default == null)
            {
                throw new InvalidOperationException(
                    "The SystemManagers.Default is null. You must either specify the default SystemManagers, or use a custom SystemsManager if your app has multiple SystemManagers.");
            }
            return SystemManagers.Default.Renderer;

        }
    }
    Camera _camera = new Camera();
    public Camera Camera => _camera;

    public static BlendState NormalBlendState
    {
        get;
        set;
    } = BlendState.NonPremultiplied;

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


    internal void Draw(SystemManagers systemManagers)
    {
        //ClearPerformanceRecordingVariables();

        if (systemManagers == null)
        {
            systemManagers = SystemManagers.Default;
        }

        Draw(systemManagers, _layers);
    }

    private void Draw(SystemManagers managers, List<Layer> layers)
    {
        _camera.ClientWidth = Raylib.GetScreenWidth();
        _camera.ClientHeight = Raylib.GetScreenHeight();

        for (int i = 0; i < layers.Count; i++)
        {
            Layer layer = layers[i];
            if (layer.IsLinearFilteringEnabled != null)
            {
                //mRenderStateVariables.Filtering = layer.IsLinearFilteringEnabled.Value;
            }
            else
            {
                //mRenderStateVariables.Filtering = TextureFilter == TextureFilter.Linear;
            }
            RenderLayer(managers, layer, prerender: false);
        }
    }
    public void RenderLayer(ISystemManagers managers, Layer layer, bool prerender = true)
    {

        layer.SortRenderables();

        Render(layer.Renderables, managers, layer);
    }

    private void Render(ReadOnlyCollection<IRenderableIpso> renderables, ISystemManagers managers, Layer layer)
    {
        for(int i = 0; i < renderables.Count; i++)
        {
            IRenderableIpso renderable = renderables[i];

            DrawGumRecursively(renderable);

            //if (renderable is GraphicalUiElement graphicalUiElement)
            //{
            //    DrawGumRecursively(graphicalUiElement);

            //    //if (RenderUsingHierarchy)
            //    //{
            //    //    DrawGumRecursively(graphicalUiElement);
            //    //}
            //    //else
            //    //{
            //    //    graphicalUiElement.Render(null);
            //    //}
            //}
            //else
            //{
            //    renderable.Render(null);
            //}
        }
    }

    private void DrawGumRecursively(IRenderableIpso element)
    {
        element.Render(null);

        if (element.ClipsChildren)
        {
            var scissorX = (int)element.GetAbsoluteX();
            var scissorY = (int)element.GetAbsoluteY();
            var scissorWidth = (int)(element.GetAbsoluteRight() - element.GetAbsoluteLeft());
            var scissorHeight = (int)(element.GetAbsoluteBottom() - element.GetAbsoluteTop());
            Raylib.BeginScissorMode(scissorX, scissorY, scissorWidth, scissorHeight);
        }

        if (element.Children != null)
        {
            foreach (var child in element.Children)
            {
                if (child is GraphicalUiElement childGue && childGue.Visible)
                {
                    DrawGumRecursively(childGue);
                }
            }
        }

        if (element.ClipsChildren)
        {
            Raylib.EndScissorMode();
        }

    }

}
