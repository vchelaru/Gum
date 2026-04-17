using Gum.Wireframe;
using SokolGum.Renderables;

namespace SokolGum.GueDeriving;

/// <summary>
/// An <see cref="InteractiveGue"/> backed by a <see cref="SolidRectangle"/>.
/// Use this instead of <see cref="ColoredRectangleRuntime"/> when you want
/// to receive Push/Click/RollOn/RollOff events from Gum's input pipeline —
/// plain GraphicalUiElements are skipped by DoUiActivityRecursively.
/// </summary>
public sealed class InteractiveColoredRectangleRuntime : InteractiveGue
{
    private SolidRectangle? _cached;
    private SolidRectangle Contained => _cached ??= (SolidRectangle)this.RenderableComponent;

    public Color Color
    {
        get => Contained.Color;
        set { Contained.Color = value; NotifyPropertyChanged(); }
    }

    public int Red   { get => Contained.Red;   set { Contained.Red   = value; NotifyPropertyChanged(); } }
    public int Green { get => Contained.Green; set { Contained.Green = value; NotifyPropertyChanged(); } }
    public int Blue  { get => Contained.Blue;  set { Contained.Blue  = value; NotifyPropertyChanged(); } }

    public InteractiveColoredRectangleRuntime(bool fullInstantiation = true)
    {
        if (!fullInstantiation) return;

        var solid = new SolidRectangle { Color = Color.White };
        SetContainedObject(solid);
        _cached = solid;

        Width = 50;
        Height = 50;

        // InteractiveGue instances need HasEvents = true to participate in
        // Gum's click/hover dispatch (default is false for raw instances).
        HasEvents = true;
    }
}
