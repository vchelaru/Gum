using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Gum.Controls;

/// <summary>
/// Draws one tree-row icon: the artwork for an <see cref="ImageIndex"/>, filled with that icon's
/// theme color.
/// </summary>
/// <remarks>
/// A control rather than a pair of binding converters because the tint comes from a theme resource
/// looked up by a key that varies per icon, which XAML cannot express as a single
/// <c>DynamicResource</c>. Subscribing to <see cref="TreeIconRegistry.ThemeChanged"/> here means a
/// theme switch re-tints every visible row without the tree being rebuilt - the WinForms version
/// had to regenerate an entire <c>ImageList</c> for this.
/// </remarks>
public class TreeNodeIcon : Control
{
    public static readonly DependencyProperty ImageIndexProperty = DependencyProperty.Register(
        nameof(ImageIndex),
        typeof(int),
        typeof(TreeNodeIcon),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender, OnIconChanged));

    private static readonly DependencyPropertyKey IconBrushPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IconBrush), typeof(Brush), typeof(TreeNodeIcon), new PropertyMetadata(null));

    private static readonly DependencyPropertyKey IconMaskPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IconMask), typeof(Brush), typeof(TreeNodeIcon), new PropertyMetadata(null));

    public static readonly DependencyProperty IconBrushProperty = IconBrushPropertyKey.DependencyProperty;
    public static readonly DependencyProperty IconMaskProperty = IconMaskPropertyKey.DependencyProperty;

    public TreeNodeIcon()
    {
        // Subscribed on load rather than in the constructor: rows are created and discarded as the
        // tree is rebuilt, and a constructor-time subscription to a static event would keep every
        // one of them alive.
        Loaded += (_, _) => TreeIconRegistry.ThemeChanged += OnThemeChanged;
        Unloaded += (_, _) => TreeIconRegistry.ThemeChanged -= OnThemeChanged;
        Refresh();
    }

    /// <summary>
    /// Which icon to draw. Values are the shared <see cref="Gum.Managers.TreeNodeImageIndices"/>
    /// constants.
    /// </summary>
    public int ImageIndex
    {
        get => (int)GetValue(ImageIndexProperty);
        set => SetValue(ImageIndexProperty, value);
    }

    /// <summary>
    /// The theme color this icon is tinted with. Read by the template.
    /// </summary>
    public Brush? IconBrush => (Brush?)GetValue(IconBrushProperty);

    /// <summary>
    /// The artwork, as an opacity mask over <see cref="IconBrush"/>. Read by the template.
    /// </summary>
    public Brush? IconMask => (Brush?)GetValue(IconMaskProperty);

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((TreeNodeIcon)d).Refresh();

    private void OnThemeChanged(object? sender, System.EventArgs e) => Refresh();

    private void Refresh()
    {
        SetValue(IconBrushPropertyKey, TreeIconRegistry.GetBrush(ImageIndex));

        ImageSource? source = TreeIconRegistry.GetSource(ImageIndex);
        SetValue(IconMaskPropertyKey, source == null ? null : new ImageBrush(source));
    }
}
