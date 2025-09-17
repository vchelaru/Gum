using Gum.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Gum.Controls;

[TemplatePart(Name = "PART_Image", Type = typeof(Image))]
[TemplatePart(Name = "PART_Path", Type = typeof(System.Windows.Shapes.Path))]
public class GumIcon : Control
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(GumIconKind), typeof(GumIcon),
            new PropertyMetadata(GumIconKind.None, OnIconChanged));

    public GumIconKind Icon
    {
        get => (GumIconKind)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty StretchProperty =
        DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(GumIcon),
            new PropertyMetadata(Stretch.Uniform));

    public Stretch Stretch
    {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    Image? _image;
    System.Windows.Shapes.Path? _path;

    static GumIcon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GumIcon),
            new FrameworkPropertyMetadata(typeof(GumIcon)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _image = GetTemplateChild("PART_Image") as Image;
        _path = GetTemplateChild("PART_Path") as System.Windows.Shapes.Path;
        UpdateVisual();
    }

    static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((GumIcon)d).UpdateVisual();

    void UpdateVisual()
    {
        if (_image is null && _path is null) return;

        // Look up the resource (Icons.xaml must be merged into scope or App resources)
        var key = GumIconKindMap.GetResourceKey(Icon);
        object? res = key is null ? null : TryFindResource(key) ?? Application.Current?.Resources[key!];

        // Reset
        if (_image != null) { _image.Source = null; _image.Visibility = Visibility.Collapsed; }
        if (_path != null) { _path.Data = null; _path.Visibility = Visibility.Collapsed; }

        switch (res)
        {
            case Geometry geo when _path != null:
                _path.Data = geo;       // single-color via Foreground
                _path.Visibility = Visibility.Visible;
                break;

            case DrawingImage di when _image != null:
                _image.Source = di;
                _image.Visibility = Visibility.Visible;
                break;

            case ImageSource src when _image != null:
                _image.Source = src;
                _image.Visibility = Visibility.Visible;
                break;

            default:
                // Silent fallback: nothing shown. You could draw a box or warning in DEBUG.
                break;
        }
    }
}