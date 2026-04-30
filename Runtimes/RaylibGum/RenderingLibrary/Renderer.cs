using Gum;
using Gum.Wireframe;
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

    List<Layer> _layers;
    ReadOnlyCollection<Layer> _layersReadOnly;

#if XNALIKE
    SpriteRenderer spriteRenderer = new SpriteRenderer();
#endif

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


    public Layer MainLayer => _layers[0];
    public ReadOnlyCollection<Layer> Layers => _layersReadOnly;

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

        var camera2D = new Camera2D
        {
            Zoom = _camera.Zoom,
            Target = new System.Numerics.Vector2(_camera.X, _camera.Y),
            Offset = _camera.CameraCenterOnScreen == CameraCenterOnScreen.Center
                ? new System.Numerics.Vector2(_camera.ClientWidth / 2f, _camera.ClientHeight / 2f)
                : System.Numerics.Vector2.Zero,
            Rotation = 0,
        };

        Raylib.BeginMode2D(camera2D);

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

        Raylib.EndMode2D();
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
            var zoom = _camera.Zoom;
            var offsetX = _camera.CameraCenterOnScreen == CameraCenterOnScreen.Center
                ? _camera.ClientWidth / 2f : 0f;
            var offsetY = _camera.CameraCenterOnScreen == CameraCenterOnScreen.Center
                ? _camera.ClientHeight / 2f : 0f;

            var scissorX = (int)((element.GetAbsoluteLeft() - _camera.X) * zoom + offsetX);
            var scissorY = (int)((element.GetAbsoluteTop() - _camera.Y) * zoom + offsetY);
            var scissorWidth = (int)((element.GetAbsoluteRight() - element.GetAbsoluteLeft()) * zoom);
            var scissorHeight = (int)((element.GetAbsoluteBottom() - element.GetAbsoluteTop()) * zoom);
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
