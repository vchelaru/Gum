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

#if RAYLIB
using BlendState = Gum.BlendState;
namespace Gum.GueDeriving;
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

    public Gum.RenderingLibrary.Blend Blend
    {
        get
        {
            return Gum.RenderingLibrary.BlendExtensions.ToBlend(RenderableComponent.BlendState);
        }
        set
        {
#if XNALIKE
            BlendState = value.ToBlendState().ToXNA();
#else
            BlendState = value.ToBlendState();
#endif

            // NotifyPropertyChanged handled by BlendState:
        }
    }

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
        ExposeChildrenEvents = true;
        Width = 150;
        Height = 150;
        Visible = true;

    }

    public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

    public override string BatchKey => Children?.LastOrDefault()?.BatchKey ?? string.Empty;

    public virtual void StartBatch(ISystemManagers systemManagers) => Children?.FirstOrDefault()?.StartBatch(systemManagers);
    public virtual void EndBatch(ISystemManagers systemManagers) => Children?.FirstOrDefault()?.EndBatch(systemManagers);
}
