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
using BlendStateAlias = global::Microsoft.Xna.Framework.Graphics.BlendState;
#else
using BlendStateAlias = global::Gum.BlendState;
#endif

#if FRB
namespace MonoGameGum.GueDeriving;
#else
namespace Gum.GueDeriving;
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

#if XNALIKE
    /// <summary>
    /// An optional shader applied when this container is drawn back to the screen as a
    /// render target. Only has an effect when <see cref="IsRenderTarget"/> is true. The
    /// effect is bound for the single sprite draw that blits the container's render target
    /// to the screen, so it acts as a post-process over the whole container's contents.
    /// </summary>
    public global::Microsoft.Xna.Framework.Graphics.Effect? RenderTargetEffect
    {
        get => (RenderableComponent as InvisibleRenderable)?.RenderTargetEffect
            as global::Microsoft.Xna.Framework.Graphics.Effect;
        set
        {
            if (RenderableComponent is InvisibleRenderable invisibleRenderable)
            {
                invisibleRenderable.RenderTargetEffect = value;
            }
        }
    }
#elif RAYLIB
    /// <summary>
    /// An optional GLSL <see cref="global::Raylib_cs.Shader"/> applied when this container is drawn
    /// back to the screen as a render target. Only has an effect when <see cref="IsRenderTarget"/>
    /// is true. The shader is bound (<c>BeginShaderMode</c>) for the single texture draw that
    /// composites the container's baked render target, so it acts as a post-process over the whole
    /// container's contents. Load a shader with <c>Raylib.LoadShader</c> /
    /// <c>Raylib.LoadShaderFromMemory</c> and assign it here (raylib loads GLSL directly, so no
    /// shader-compiler dependency is needed).
    /// </summary>
    public global::Raylib_cs.Shader? RenderTargetEffect
    {
        get => (RenderableComponent as InvisibleRenderable)?.RenderTargetEffect
            as global::Raylib_cs.Shader?;
        set
        {
            if (RenderableComponent is InvisibleRenderable invisibleRenderable)
            {
                invisibleRenderable.RenderTargetEffect = value;
            }
        }
    }
#elif SKIA
    /// <summary>
    /// An optional compiled SkSL <see cref="global::SkiaSharp.SKRuntimeEffect"/> applied when this
    /// container is drawn back to the screen as a render target. Only has an effect when
    /// <see cref="IsRenderTarget"/> is true. The effect is image-independent (it declares a
    /// <c>uniform shader inputImage</c> child) — the baked render-target image is bound to that
    /// child and the resulting <c>SKShader</c> built only at composite time, since the image isn't
    /// known until the bake completes. Compile SkSL with
    /// <c>SKRuntimeEffect.CreateShader(sksl, out errors)</c> and assign the result here.
    /// </summary>
    public global::SkiaSharp.SKRuntimeEffect? RenderTargetEffect
    {
        get => (RenderableComponent as InvisibleRenderable)?.RenderTargetEffect
            as global::SkiaSharp.SKRuntimeEffect;
        set
        {
            if (RenderableComponent is InvisibleRenderable invisibleRenderable)
            {
                invisibleRenderable.RenderTargetEffect = value;
            }
        }
    }
#endif

#if XNALIKE || RAYLIB || SKIA
    /// <summary>
    /// A file reference (e.g. a <c>.fx</c> path on XNA-likes, a <c>.fs</c>/<c>.glsl</c> path on
    /// raylib, or a <c>.sksl</c> path on Skia) to a post-process shader applied when this container
    /// is drawn back to the screen as a render target. Mirrors how a Sprite references a texture:
    /// setting this routes through the string property path, which resolves the reference via the
    /// active backend's registered resolver (XNA-like/raylib:
    /// <c>Gum.Wireframe.CustomSetPropertyOnRenderable.RenderTargetEffectResolver</c>; Skia:
    /// <c>SkiaGum.CustomSetPropertyOnRenderable.RenderTargetEffectResolver</c>) and assigns the
    /// result to <see cref="RenderTargetEffect"/>. With no resolver registered the assignment is a
    /// graceful no-op (the container renders unshaded). Only has an effect when
    /// <see cref="IsRenderTarget"/> is true. Write-only: there is no backing field; the resolved
    /// effect is read back via <see cref="RenderTargetEffect"/>.
    /// </summary>
    public string? SourceShaderFile
    {
        set => base.SetProperty("SourceShaderFile", value);
    }
#endif


#if !SOKOL
    public BlendStateAlias BlendState
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
