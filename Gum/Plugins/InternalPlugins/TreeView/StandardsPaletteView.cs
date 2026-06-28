using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Gum.Plugins.InternalPlugins.TreeView;

/// <summary>
/// The experimental "Standards palette": a chip palette pinned to the bottom of the Project panel
/// that replaces the Standard folder in the element tree. Each chip represents a standard type
/// (Container, Text, Sprite, ...) and can be dragged onto the tree or wireframe to create an
/// instance, or right-clicked to add it to the current element or edit its project-wide defaults.
/// </summary>
internal class StandardsPaletteView : Border
{
    // Below this per-cell width a chip can't fit its icon + label, so it collapses to a centered
    // icon. Below TwoColumnMinWidth the grid drops from two columns to one.
    private const double LabelMinCellWidth = 66;
    private const double TwoColumnMinWidth = 160;

    private readonly Func<string, ImageSource?> _iconResolver;
    private readonly UniformGrid _chipsPanel;
    private readonly Dictionary<string, Border> _chipsByType = new();
    private readonly List<ChipVisual> _chipVisuals = new();
    private readonly List<string> _currentTypeNames = new();
    private string? _selectedTypeName;
    private Border? _draggingChip;

    /// <summary>References to a chip's adjustable parts, used to adapt its layout as the panel resizes.</summary>
    private sealed class ChipVisual
    {
        public Border Chip = null!;
        public StackPanel Content = null!;
        public Image? Icon;
        public TextBlock Label = null!;
    }

    /// <summary>Raised with the standard type name when "Add to current ..." is clicked on a chip.</summary>
    public Action<string>? AddToCurrentRequested { get; set; }

    /// <summary>Raised with the standard type name when "Edit defaults..." is clicked on a chip.</summary>
    public Action<string>? EditDefaultsRequested { get; set; }

    /// <summary>Returns the currently-open Screen/Component name, or null if none is open.</summary>
    public Func<string?>? CurrentElementNameProvider { get; set; }

    /// <param name="iconResolver">Resolves a standard type name's icon key (e.g. "Text.png") to a themed image.</param>
    public StandardsPaletteView(Func<string, ImageSource?> iconResolver)
    {
        _iconResolver = iconResolver;

        BorderThickness = new Thickness(0, 1, 0, 0);
        SetResourceReference(Border.BorderBrushProperty, "Frb.Brushes.Border");
        Padding = new Thickness(4, 6, 4, 6);
        Margin = new Thickness(0, 4, 0, 0);

        StackPanel root = new StackPanel();

        // WrapPanel so the hint flows onto its own line instead of overlapping the title when the
        // Project panel is narrow.
        WrapPanel header = new WrapPanel { Margin = new Thickness(2, 0, 2, 6) };
        TextBlock title = new TextBlock
        {
            Text = "STANDARDS",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        title.SetResourceReference(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground");
        TextBlock hint = new TextBlock
        {
            Text = "drag onto tree or canvas",
            FontSize = 10,
            FontStyle = FontStyles.Italic,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.6
        };
        hint.SetResourceReference(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground");
        header.Children.Add(title);
        header.Children.Add(hint);

        _chipsPanel = new UniformGrid { Columns = 2 };
        _chipsPanel.SizeChanged += (_, _) => UpdateChipLayout();

        root.Children.Add(header);
        root.Children.Add(_chipsPanel);
        Child = root;
    }

    /// <summary>
    /// Adapts the chip grid to the available width: two columns when wide, one when narrow, and a
    /// centered icon-only chip when a cell is too tight to fit the label. This keeps the icon visible
    /// (and unclipped) instead of clamping every chip to a fixed half-width cell.
    /// </summary>
    private void UpdateChipLayout()
    {
        double available = _chipsPanel.ActualWidth;
        if (available <= 0 || _chipVisuals.Count == 0)
        {
            return;
        }

        int columns = available >= TwoColumnMinWidth ? 2 : 1;
        _chipsPanel.Columns = columns;

        double cellWidth = available / columns;
        bool compact = cellWidth < LabelMinCellWidth;

        foreach (ChipVisual visual in _chipVisuals)
        {
            visual.Label.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            visual.Content.HorizontalAlignment = compact ? HorizontalAlignment.Center : HorizontalAlignment.Left;
            visual.Chip.Padding = compact ? new Thickness(4) : new Thickness(8, 5, 8, 5);
            if (visual.Icon != null)
            {
                visual.Icon.Margin = compact ? new Thickness(0) : new Thickness(0, 0, 6, 0);
            }
        }
    }

    /// <summary>
    /// Rebuilds the chips from the given standard type names (in display order). Safe to call on
    /// project load and whenever the available standards change.
    /// </summary>
    public void RefreshChips(IReadOnlyList<string> standardTypeNames)
    {
        // Idempotent: callers refresh on every tree rebuild, but the standard set rarely changes.
        // Skipping the rebuild when the names are unchanged avoids needless WPF churn and preserves
        // hover / selection highlight state.
        if (standardTypeNames.SequenceEqual(_currentTypeNames))
        {
            return;
        }

        _chipsPanel.Children.Clear();
        _chipsByType.Clear();
        _chipVisuals.Clear();
        _currentTypeNames.Clear();
        _currentTypeNames.AddRange(standardTypeNames);
        foreach (string typeName in standardTypeNames)
        {
            Border chip = CreateChip(typeName);
            _chipsByType[typeName] = chip;
            _chipsPanel.Children.Add(chip);
        }
        // A rebuild drops the highlight; re-apply it for the still-selected type.
        if (_selectedTypeName != null && _chipsByType.TryGetValue(_selectedTypeName, out Border? selectedChip))
        {
            ApplyChipSelectionVisual(selectedChip, isSelected: true);
        }
        UpdateChipLayout();
    }

    /// <summary>
    /// Highlights the chip for the given standard type to show it is the one currently being edited
    /// (its defaults are selected), or clears the highlight when <paramref name="typeName"/> is null.
    /// </summary>
    public void SetSelectedStandardType(string? typeName)
    {
        _selectedTypeName = typeName;
        foreach (var pair in _chipsByType)
        {
            ApplyChipSelectionVisual(pair.Value, pair.Key == typeName);
        }
    }

    private static void ApplyChipSelectionVisual(Border chip, bool isSelected)
    {
        if (isSelected)
        {
            if (Application.Current?.TryFindResource("Frb.Brushes.Primary") is Brush primary)
            {
                chip.BorderBrush = primary;
            }
            chip.Background = Application.Current?.TryFindResource("Frb.Brushes.Primary.Transparent") is Brush fill
                ? fill
                : Brushes.Transparent;
        }
        else
        {
            chip.SetResourceReference(Border.BorderBrushProperty, "Frb.Brushes.Border");
            chip.Background = Brushes.Transparent;
        }
    }

    private Border CreateChip(string typeName)
    {
        StackPanel content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        Image? iconImage = null;
        ImageSource? icon = _iconResolver(typeName + ".png");
        if (icon != null)
        {
            iconImage = new Image
            {
                Source = icon,
                Width = 14,
                Height = 14,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            content.Children.Add(iconImage);
        }

        TextBlock label = new TextBlock
        {
            Text = typeName,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        label.SetResourceReference(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground");
        content.Children.Add(label);

        Border chip = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 5, 8, 5),
            Margin = new Thickness(2),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            Child = content,
            ToolTip = $"Drag onto a Screen/Component or the canvas to add a {typeName}.\nRight-click for more options."
        };
        chip.SetResourceReference(Border.BorderBrushProperty, "Frb.Brushes.Border");

        // Hover highlight (the selected chip keeps its highlight when the mouse leaves). While this
        // chip is the active drag source, the hover handlers must not touch its look — the drag
        // begins by the cursor leaving the chip, and a MouseLeave reset would wipe the drag fill.
        chip.MouseEnter += (_, _) =>
        {
            if (_draggingChip == chip)
            {
                return;
            }
            if (Application.Current?.TryFindResource("Frb.Brushes.Primary") is Brush primary)
            {
                chip.BorderBrush = primary;
            }
        };
        chip.MouseLeave += (_, _) =>
        {
            if (_draggingChip == chip)
            {
                return;
            }
            ApplyChipSelectionVisual(chip, _selectedTypeName == typeName);
        };

        WireDrag(chip, typeName);
        chip.ContextMenu = CreateChipContextMenu(typeName);

        _chipVisuals.Add(new ChipVisual
        {
            Chip = chip,
            Content = content,
            Icon = iconImage,
            Label = label
        });

        return chip;
    }

    private void WireDrag(Border chip, string typeName)
    {
        Point startPoint = default;
        bool pressed = false;

        chip.PreviewMouseLeftButtonDown += (_, e) =>
        {
            pressed = true;
            startPoint = e.GetPosition(null);
        };
        chip.PreviewMouseLeftButtonUp += (_, _) => pressed = false;
        chip.MouseMove += (_, e) =>
        {
            if (!pressed || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point current = e.GetPosition(null);
            if (Math.Abs(current.X - startPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(current.Y - startPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            pressed = false;
            DataObject data = new DataObject();
            data.SetData(DragDropManager.StandardElementNameDataFormat, typeName);

            // Highlight the source chip for the duration of the (blocking) drag so it's clear which
            // standard is being dragged, then restore its normal/selected look when the drop finishes.
            // _draggingChip suppresses the hover handlers so the immediate MouseLeave (the cursor
            // leaves the chip as the drag starts) doesn't wipe the drag fill.
            _draggingChip = chip;
            SetChipDragActive(chip);
            try
            {
                DragDrop.DoDragDrop(chip, data, DragDropEffects.Copy);
            }
            finally
            {
                _draggingChip = null;
                ApplyChipSelectionVisual(chip, _selectedTypeName == typeName);
            }
        };
    }

    private static void SetChipDragActive(Border chip)
    {
        // Only the fill/border colors change (never thickness or size), so the chip emphasizes in
        // place without shifting its neighbors. The caller restores the normal / selected look via
        // ApplyChipSelectionVisual when the drag ends.
        if (Application.Current?.TryFindResource("Frb.Brushes.Primary") is SolidColorBrush primary)
        {
            chip.BorderBrush = primary;
            // A theme-derived, semi-transparent primary wash: stands out clearly as the active drag
            // source while keeping the label readable, and adapts to dark/light since it comes from
            // the theme's Primary color.
            Color c = primary.Color;
            SolidColorBrush fill = new SolidColorBrush(Color.FromArgb(0x80, c.R, c.G, c.B));
            fill.Freeze();
            chip.Background = fill;
        }
    }

    private ContextMenu CreateChipContextMenu(string typeName)
    {
        ContextMenu menu = new ContextMenu();

        MenuItem addToCurrent = new MenuItem();
        addToCurrent.Click += (_, _) => AddToCurrentRequested?.Invoke(typeName);

        MenuItem editDefaults = new MenuItem { Header = "Edit defaults..." };
        editDefaults.Click += (_, _) => EditDefaultsRequested?.Invoke(typeName);

        // Resolve the "Add to current ..." label/enabled state each time the menu opens, since the
        // open element changes over the palette's lifetime.
        menu.Opened += (_, _) =>
        {
            string? currentElementName = CurrentElementNameProvider?.Invoke();
            addToCurrent.IsEnabled = currentElementName != null;
            addToCurrent.Header = currentElementName != null
                ? $"Add to {currentElementName}"
                : "Add to current element";
        };

        menu.Items.Add(addToCurrent);
        menu.Items.Add(new Separator());
        menu.Items.Add(editDefaults);
        return menu;
    }
}
