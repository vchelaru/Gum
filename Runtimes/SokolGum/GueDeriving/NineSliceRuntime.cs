using Gum.Wireframe;
using SokolGum.Renderables;

namespace SokolGum.GueDeriving;

public sealed class NineSliceRuntime : GraphicalUiElement
{
    private NineSlice? _cached;
    private NineSlice Contained => _cached ??= (NineSlice)this.RenderableComponent;

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

    public int Alpha { get => Contained.Alpha; set { Contained.Alpha = value; NotifyPropertyChanged(); } }
    public int Red   { get => Contained.Red;   set { Contained.Red   = value; NotifyPropertyChanged(); } }
    public int Green { get => Contained.Green; set { Contained.Green = value; NotifyPropertyChanged(); } }
    public int Blue  { get => Contained.Blue;  set { Contained.Blue  = value; NotifyPropertyChanged(); } }

    public float? CustomFrameTextureCoordinateWidth
    {
        get => Contained.CustomFrameTextureCoordinateWidth;
        set { Contained.CustomFrameTextureCoordinateWidth = value; NotifyPropertyChanged(); }
    }

    public NineSliceRuntime(bool fullInstantiation = true)
    {
        if (!fullInstantiation) return;

        var nineSlice = new NineSlice();
        SetContainedObject(nineSlice);
        _cached = nineSlice;

        Width = 100;
        Height = 100;
    }
}
