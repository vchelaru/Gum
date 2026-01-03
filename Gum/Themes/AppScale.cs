using System.Windows;

namespace Gum.Themes;

public sealed class AppScale : DependencyObject
{
    private const double BaseFontSizeDefault = 12.0;

    private const double BodyScale = 1;
    private const double CaptionScale = 0.85;
    private const double H2Scale = 1.25;
    private const double H1Scale = 1.6;
    private const double H3Scale = 1.16;
    private const double IconInlineScale = 1.25;
    private const double IconButtonScale = 1.667;
    private const double ToggleDisplayIconScale = 2.333;

    public double BaseFontSize
    {
        get => (double)GetValue(BaseFontSizeProperty);
        set => SetValue(BaseFontSizeProperty, value);
    }

    public static readonly DependencyProperty BaseFontSizeProperty =
        DependencyProperty.Register(nameof(BaseFontSize), typeof(double), typeof(AppScale),
            new PropertyMetadata(BaseFontSizeDefault, OnBaseChanged));

    // Text tokens (add any you need)
    public double Body { get => (double)GetValue(BodyProperty); private set => SetValue(BodyProperty, value); }
    public double Caption { get => (double)GetValue(CaptionProperty); private set => SetValue(CaptionProperty, value); }
    public double H1 { get => (double)GetValue(H1Property); private set => SetValue(H1Property, value); }
    public double H2 { get => (double)GetValue(H2Property); private set => SetValue(H2Property, value); }
    public double H3
    {
        get => (double)GetValue(H3Property); private set => SetValue(H3Property, value);
    }

    // Icon tokens
    public double IconInline { get => (double)GetValue(IconInlineProperty); private set => SetValue(IconInlineProperty, value); }
    public double IconButton { get => (double)GetValue(IconButtonProperty); private set => SetValue(IconButtonProperty, value); }
    public double ToggleDisplayIcon { get => (double)GetValue(ToggleDisplayIconProperty); private set => SetValue(ToggleDisplayIconProperty, value); }

    public static readonly DependencyProperty BodyProperty = DP(nameof(Body), BaseFontSizeDefault * BodyScale);
    public static readonly DependencyProperty CaptionProperty = DP(nameof(Caption), BaseFontSizeDefault * CaptionScale);
    public static readonly DependencyProperty H1Property = DP(nameof(H1), BaseFontSizeDefault * H1Scale);
    public static readonly DependencyProperty H2Property = DP(nameof(H2), BaseFontSizeDefault * H2Scale);
    public static readonly DependencyProperty H3Property = DP(nameof(H3), BaseFontSizeDefault * H3Scale);
    public static readonly DependencyProperty IconInlineProperty = DP(nameof(IconInline), BaseFontSizeDefault * IconInlineScale);
    public static readonly DependencyProperty IconButtonProperty = DP(nameof(IconButton), BaseFontSizeDefault * IconButtonScale);
    public static readonly DependencyProperty ToggleDisplayIconProperty = DP(nameof(ToggleDisplayIcon), BaseFontSizeDefault * ToggleDisplayIconScale);

    static DependencyProperty DP(string name, double? defaultValue = 0d) =>
        DependencyProperty.Register(name, typeof(double), typeof(AppScale), new PropertyMetadata(defaultValue));

    static void OnBaseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var s = (AppScale)d;
        var b = s.BaseFontSize;

        // Text scale (pick factors you like; examples shown)
        s.Body = b;             // base body
        s.Caption = b * CaptionScale;      // small
        s.H2 = b * H2Scale;      // subheader
        s.H1 = b * H1Scale;       // header
        s.H3 = b * H3Scale;      // header

        // Icon scale tokens tied to *current* base
        s.IconInline = b * IconInlineScale;   // inline with text
        s.IconButton = b * IconButtonScale;  // on icon-only or button icons
        s.ToggleDisplayIcon = b * ToggleDisplayIconScale; // large icon for toggle display
    }
}