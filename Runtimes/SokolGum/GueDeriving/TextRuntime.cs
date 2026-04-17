using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SokolGum.Renderables;

namespace SokolGum.GueDeriving;

public sealed class TextRuntime : GraphicalUiElement
{
    private Text? _cached;
    private Text Contained => _cached ??= (Text)this.RenderableComponent;

    public Font? Font
    {
        get => Contained.Font;
        set { Contained.Font = value; NotifyPropertyChanged(); }
    }

    public string? RawText
    {
        get => Contained.RawText;
        set { Contained.RawText = value; NotifyPropertyChanged(); }
    }

    public float FontSize
    {
        get => Contained.FontSize;
        set { Contained.FontSize = value; NotifyPropertyChanged(); }
    }

    public Color Color
    {
        get => Contained.Color;
        set { Contained.Color = value; NotifyPropertyChanged(); }
    }

    public int Alpha
    {
        get => Contained.Alpha;
        set { Contained.Alpha = value; NotifyPropertyChanged(); }
    }

    public HorizontalAlignment HorizontalAlignment
    {
        get => Contained.HorizontalAlignment;
        set { Contained.HorizontalAlignment = value; NotifyPropertyChanged(); }
    }

    public VerticalAlignment VerticalAlignment
    {
        get => Contained.VerticalAlignment;
        set { Contained.VerticalAlignment = value; NotifyPropertyChanged(); }
    }

    public TextOverflowHorizontalMode TextOverflowHorizontalMode
    {
        get => Contained.TextOverflowHorizontalMode;
        set { Contained.TextOverflowHorizontalMode = value; NotifyPropertyChanged(); }
    }

    public TextOverflowVerticalMode TextOverflowVerticalMode
    {
        get => Contained.TextOverflowVerticalMode;
        set { Contained.TextOverflowVerticalMode = value; NotifyPropertyChanged(); }
    }

    public bool WrapTextInsideBlock
    {
        get => Contained.WrapTextInsideBlock;
        set { Contained.WrapTextInsideBlock = value; NotifyPropertyChanged(); }
    }

    public float LineHeightMultiplier
    {
        get => Contained.LineHeightMultiplier;
        set { Contained.LineHeightMultiplier = value; NotifyPropertyChanged(); }
    }

    public int OutlineThickness
    {
        get => Contained.OutlineThickness;
        set { Contained.OutlineThickness = value; NotifyPropertyChanged(); }
    }

    public Color OutlineColor
    {
        get => Contained.OutlineColor;
        set { Contained.OutlineColor = value; NotifyPropertyChanged(); }
    }

    /// <summary>
    /// Limits the number of rendered characters for typewriter-style reveal
    /// effects. Null draws the entire text (the default). Advance this value
    /// over time to animate.
    /// </summary>
    public int? MaxLettersToShow
    {
        get => Contained.MaxLettersToShow;
        set { Contained.MaxLettersToShow = value; NotifyPropertyChanged(); }
    }

    public TextRuntime(bool fullInstantiation = true)
    {
        if (!fullInstantiation) return;

        var text = new Text();
        SetContainedObject(text);
        _cached = text;

        Width = 200;
        Height = 40;
    }
}
