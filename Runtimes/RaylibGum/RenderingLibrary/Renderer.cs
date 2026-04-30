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

    // Raylib's BeginScissorMode replaces the active scissor rather than intersecting,
    // so we must track ancestors and intersect manually for nested ClipsChildren.
    Stack<System.Drawing.Rectangle> _scissorStack = new();

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
        _scissorStack.Clear();
        for(int i = 0; i < renderables.Count; i++)
        {
            IRenderableIpso renderable = renderables[i];

            DrawGumRecursively(renderable, layer);

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

    private void DrawGumRecursively(IRenderableIpso element, Layer layer)
    {
        element.Render(null);

        if (element.ClipsChildren)
        {
            var rect = _camera.GetScissorRectangleFor(layer, element);
            var effective = _scissorStack.Count > 0
                ? System.Drawing.Rectangle.Intersect(_scissorStack.Peek(), rect)
                : rect;
            _scissorStack.Push(effective);
            Raylib.BeginScissorMode(effective.X, effective.Y, effective.Width, effective.Height);
        }

        if (element.Children != null)
        {
            foreach (var child in element.Children)
            {
                if (child is GraphicalUiElement childGue && childGue.Visible)
                {
                    DrawGumRecursively(childGue, layer);
                }
            }
        }

        if (element.ClipsChildren)
        {
            _scissorStack.Pop();
            if (_scissorStack.Count > 0)
            {
                var parent = _scissorStack.Peek();
                Raylib.BeginScissorMode(parent.X, parent.Y, parent.Width, parent.Height);
            }
            else
            {
                Raylib.EndScissorMode();
            }
        }
    }

}
