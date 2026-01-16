using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;

namespace SkiaGum.GueDeriving;

public abstract class SkiaRuntime<T> : BindableGue
    where T : class, IRenderable
{
    protected SkiaRuntime(T contained, bool fullInstantiation = true)
    {
        ArgumentNullException.ThrowIfNull(contained);

        if (fullInstantiation)
            SetContainedObject(contained);
    }

    protected T Renderable
        => RenderableComponent as T
           ?? throw new InvalidOperationException(
               $"Expected contained object of type {typeof(T).Name} but found {RenderableComponent?.GetType().Name ?? "null"}.");
}