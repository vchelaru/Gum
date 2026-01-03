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

    public static readonly DependencyProperty DesignSizeProperty =
        DependencyProperty.Register(nameof(DesignSize), typeof(double), typeof(GumIcon),
            new PropertyMetadata(32.0));
    public static readonly DependencyProperty DesignPaddingProperty =
        DependencyProperty.Register(nameof(DesignPadding), typeof(double), typeof(GumIcon),
            new PropertyMetadata(2.0));

    public double DesignSize { get => (double)GetValue(DesignSizeProperty); set => SetValue(DesignSizeProperty, value); }
    public double DesignPadding { get => (double)GetValue(DesignPaddingProperty); set => SetValue(DesignPaddingProperty, value); }

    public static readonly DependencyProperty SecondaryOpacityProperty =
        DependencyProperty.Register(nameof(SecondaryOpacity), typeof(double), typeof(GumIcon),
            new PropertyMetadata(0.32));
    public double SecondaryOpacity { get => (double)GetValue(SecondaryOpacityProperty); set => SetValue(SecondaryOpacityProperty, value); }

    System.Windows.Shapes.Path? _pathSecondary;


    Image? _image;
    Canvas? _box;
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
        _pathSecondary = GetTemplateChild("PART_PathSecondary") as System.Windows.Shapes.Path;
        _box = GetTemplateChild("PART_Box") as Canvas;

        if (_box != null)
        {
            double inner = Math.Max(0, DesignSize - 2 * DesignPadding); // 32 - 4 = 28
            _box.Width = inner;
            _box.Height = inner;
        }
        UpdateVisual();
    }

    static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((GumIcon)d).UpdateVisual();

    Geometry ToDesignBox(Geometry src, double pad)
    {
        var g = src.CloneCurrentValue();
        var tg = new TransformGroup();
        if (g.Transform is Transform ex && !ex.Value.IsIdentity) tg.Children.Add(ex);
        tg.Children.Add(new TranslateTransform(-pad, -pad)); // (-2, -2)
        g.Transform = tg;
        return g;
    }


    void UpdateVisual()
    {
        if (_image is null && _path is null) return;

        var key = GumIconKindMap.GetResourceKey(Icon);
        object? res = key is null ? null : TryFindResource(key) ?? Application.Current?.Resources[key!];

        // reset
        if (_image != null) { _image.Source = null; _image.Visibility = Visibility.Collapsed; }
        if (_path != null) { _path.Data = null; _path.Visibility = Visibility.Collapsed; _path.ClearValue(UIElement.ClipProperty); }
        if (_pathSecondary != null) { _pathSecondary.Data = null; _pathSecondary.Visibility = Visibility.Collapsed; _pathSecondary.ClearValue(UIElement.ClipProperty); }

        switch (res)
        {
            case Geometry geo when _path != null:
            {
                // PRIMARY
                var gPrimary = ToDesignBox(geo, DesignPadding);
                _path.Stretch = Stretch.None;             // important: same for both
                Canvas.SetLeft(_path, 0); Canvas.SetTop(_path, 0);
                _path.Data = gPrimary;
                _path.Visibility = Visibility.Visible;

                // SECONDARY (apply the SAME transform)
                var key2 = key + ".Secondary";
                if (_pathSecondary != null &&
                    (TryFindResource(key2) ?? Application.Current?.Resources[key2]) is Geometry g2raw)
                {
                    var gSecondary = ToDesignBox(g2raw, DesignPadding);
                    _pathSecondary.Stretch = Stretch.None;
                    Canvas.SetLeft(_pathSecondary, 0); Canvas.SetTop(_pathSecondary, 0);
                    _pathSecondary.Data = gSecondary;
                    _pathSecondary.Visibility = Visibility.Visible;
                }
                break;
            }

            case DrawingImage di when _image != null:
                _image.Source = di; _image.Visibility = Visibility.Visible;
                break;

            case ImageSource src when _image != null:
                _image.Source = src; _image.Visibility = Visibility.Visible;
                break;
        }
    }
}