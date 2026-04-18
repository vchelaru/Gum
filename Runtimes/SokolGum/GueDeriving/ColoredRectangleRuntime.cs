using Gum.Wireframe;
using Gum.Renderables;
using SokolGum;

namespace Gum.GueDeriving;

/// <summary>
/// Runtime wrapper for ColoredRectangle. Holds a <see cref="SolidRectangle"/>
/// as its contained renderable so Gum's layout system positions/sizes it.
/// </summary>
public sealed class ColoredRectangleRuntime : GraphicalUiElement
{
    public const float DefaultWidth = 50f;
    public const float DefaultHeight = 50f;

    private SolidRectangle? _cachedSolid;

    private SolidRectangle ContainedRectangle
        => _cachedSolid ??= (SolidRectangle)this.RenderableComponent;

    public Color Color
    {
        get => ContainedRectangle.Color;
        set { ContainedRectangle.Color = value; NotifyPropertyChanged(); }
    }

    public int Red   { get => ContainedRectangle.Red;   set { ContainedRectangle.Red   = value; NotifyPropertyChanged(); } }
    public int Green { get => ContainedRectangle.Green; set { ContainedRectangle.Green = value; NotifyPropertyChanged(); } }
    public int Blue  { get => ContainedRectangle.Blue;  set { ContainedRectangle.Blue  = value; NotifyPropertyChanged(); } }

    public ColoredRectangleRuntime(bool fullInstantiation = true)
    {
        if (!fullInstantiation) return;

        var solid = new SolidRectangle { Color = Color.White };
        SetContainedObject(solid);
        _cachedSolid = solid;

        Width = DefaultWidth;
        Height = DefaultHeight;
    }
}
