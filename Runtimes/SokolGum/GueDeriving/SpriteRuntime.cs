using Gum.Wireframe;
using Gum.Graphics.Animation;
using Gum.Renderables;
using SokolGum;

namespace Gum.GueDeriving;

public sealed class SpriteRuntime : GraphicalUiElement
{
    private Sprite? _cached;
    private Sprite Contained => _cached ??= (Sprite)this.RenderableComponent;

    public Texture2D? Texture
    {
        get => Contained.Texture;
        set { Contained.Texture = value; NotifyPropertyChanged(); }
    }

    public Color Color
    {
        get => Contained.Color;
        set { Contained.Color = value; NotifyPropertyChanged(); }
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

    public AnimationChainList? AnimationChains
    {
        get => Contained.AnimationChains;
        set { Contained.AnimationChains = value; NotifyPropertyChanged(); }
    }

    public string? CurrentChainName
    {
        get => Contained.CurrentChainName;
        set { Contained.CurrentChainName = value; NotifyPropertyChanged(); }
    }

    public bool Animate
    {
        get => Contained.Animate;
        set { Contained.Animate = value; NotifyPropertyChanged(); }
    }

    public float AnimationSpeed
    {
        get => Contained.AnimationSpeed;
        set { Contained.AnimationSpeed = value; NotifyPropertyChanged(); }
    }

    public SpriteRuntime(bool fullInstantiation = true)
    {
        if (!fullInstantiation) return;

        var sprite = new Sprite();
        SetContainedObject(sprite);
        _cached = sprite;
    }
}
