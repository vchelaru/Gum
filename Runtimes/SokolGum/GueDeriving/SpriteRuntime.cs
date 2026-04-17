using Gum.Wireframe;
using SokolGum.Renderables;

namespace SokolGum.GueDeriving;

public sealed class SpriteRuntime : GraphicalUiElement
{
    private Sprite? _cached;
    private Sprite Contained => _cached ??= (Sprite)this.RenderableComponent;

    public Texture2D? Texture
    {
        get => Contained.Texture;
        set { Contained.Texture = value; NotifyPropertyChanged(); }
    }

    public Color Tint
    {
        get => Contained.Tint;
        set { Contained.Tint = value; NotifyPropertyChanged(); }
    }

    // `new` hides GraphicalUiElement.FlipHorizontal, which is the design-time
    // flag normally routed to the renderable via CustomSetPropertyOnRenderable
    // (not wired in Phase 2). Our wrapper forwards directly to the Sprite.
    public new bool FlipHorizontal
    {
        get => Contained.FlipHorizontal;
        set { Contained.FlipHorizontal = value; NotifyPropertyChanged(); }
    }

    public bool FlipVertical
    {
        get => Contained.FlipVertical;
        set { Contained.FlipVertical = value; NotifyPropertyChanged(); }
    }

    public SpriteRuntime(bool fullInstantiation = true)
    {
        if (!fullInstantiation) return;

        var sprite = new Sprite();
        SetContainedObject(sprite);
        _cached = sprite;
    }
}
