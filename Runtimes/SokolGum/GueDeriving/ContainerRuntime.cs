using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SokolGum;

namespace Gum.GueDeriving;

/// <summary>
/// Layout-only container. Carries an <see cref="InvisibleRenderable"/> as
/// its RenderableComponent so Gum's layout/hit-test machinery works but
/// nothing draws. Extends <see cref="InteractiveGue"/> so children can
/// receive events; HasEvents defaults to true.
/// </summary>
public sealed class ContainerRuntime : InteractiveGue
{
    public int Alpha
    {
        get => (RenderableComponent as InvisibleRenderable)?.Alpha ?? 255;
        set
        {
            if (RenderableComponent is InvisibleRenderable invisible)
                invisible.Alpha = value;
        }
    }

    public ContainerRuntime(bool fullInstantiation = true)
    {
        if (!fullInstantiation) return;

        SetContainedObject(new InvisibleRenderable());
        HasEvents = true;
        Width = 150;
        Height = 150;
    }
}
