#if MONOGAME || FNA || KNI
#define XNALIKE
#endif

using Gum.RenderingLibrary;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if XNALIKE
using BlendState = Microsoft.Xna.Framework.Graphics.BlendState;
#endif

#if RAYLIB || SOKOL
using BlendState = Gum.BlendState;
namespace Gum.GueDeriving;
#elif SKIA
namespace SkiaGum.GueDeriving;
#else
namespace MonoGameGum.GueDeriving;
#endif


public class ContainerRuntime : InteractiveGue
{
    public int Alpha
    {
        get => (RenderableComponent as InvisibleRenderable)?.Alpha ?? 255;
        set
        {
            if (RenderableComponent is InvisibleRenderable invisibleRenderable)
            {
                invisibleRenderable.Alpha = value;
            }
        }
    }

    public bool IsRenderTarget
    {
        get => (RenderableComponent as InvisibleRenderable)?.IsRenderTarget ?? false;
        set
        {
            if (RenderableComponent is InvisibleRenderable invisibleRenderable)
            {
                invisibleRenderable.IsRenderTarget = value;
            }
        }
    }


#if !SKIA && !SOKOL
    public BlendState BlendState
    {
#if XNALIKE
        get => RenderableComponent.BlendState.ToXNA();
#else
        get => RenderableComponent.BlendState;
#endif
        set
        {
            if (RenderableComponent is InvisibleRenderable invisibleRenderable)
            {
#if XNALIKE
                invisibleRenderable.BlendState = value.ToGum();
#else
                invisibleRenderable.BlendState = value;
#endif
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(Blend));
            }
        }
    }

    public Gum.RenderingLibrary.Blend? Blend
    {
        get
        {
            return Gum.RenderingLibrary.BlendExtensions.ToBlend(RenderableComponent.BlendState);
        }
        set
        {
            if (value.HasValue)
            {
#if XNALIKE
                BlendState = value.Value.ToBlendState().ToXNA();
#else
                BlendState = value.Value.ToBlendState();
#endif
            }
            // NotifyPropertyChanged handled by BlendState:
        }
    }
#endif

    public ContainerRuntime()
    {
        Instantiate();
    }

    public ContainerRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            Instantiate();
        }
    }

    private void Instantiate()
    {
        SetContainedObject(new InvisibleRenderable());
        HasEvents = true;
        Width = 150;
        Height = 150;
    }

#if !SOKOL
    /// <inheritdoc cref="GraphicalUiElement.AddToManagers()"/>
    [Obsolete("Use the AddToRoot extension method instead (e.g. myContainer.AddToRoot()).")]
    public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);
#endif

    // Container is a transparent wrapper whose own Render is a no-op (InvisibleRenderable).
    // BatchKey must match what StartBatch actually begins — and StartBatch begins nothing
    // here. Returning a child's BatchKey to "pre-claim" a batch is a broken peephole: the
    // claim suppresses the first child's batch transition (keys match, transition skipped),
    // but the matching batch was never actually started. The first descendant shape then
    // queues into a stale or absent batch, producing intermittent draw-order artifacts that
    // depend on whatever state leaked in from the prior Renderer.Begin/End cycle. Empty key
    // lets each child fire its own transition normally.
    public override string BatchKey => string.Empty;
}
