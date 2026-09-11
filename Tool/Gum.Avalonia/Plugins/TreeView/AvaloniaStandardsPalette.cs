using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Avalonia.Services;

namespace Gum.Avalonia.Plugins.TreeView;

/// <summary>
/// The experimental Standards chip palette pinned below the element tree when the
/// UseStandardsPalette setting is on. Each chip is a standard type that can be dragged onto the
/// tree or canvas, Ctrl+clicked to add it to the current element, or right-clicked for more. The
/// counterpart of the WPF <c>StandardsPaletteView</c>.
/// </summary>
public sealed class AvaloniaStandardsPalette : Border
{
    private const double TwoColumnMinWidth = 160;
    private const double ChipIconSize = 14;
    private const double DragThreshold = 4;

    private static readonly IBrush ChipBorderBrush = new SolidColorBrush(Color.FromArgb(0xff, 0x55, 0x55, 0x55));
    private static readonly IBrush PrimaryBrush = new SolidColorBrush(Color.Parse("#3e9ece"));
    private static readonly IBrush SelectedFillBrush = new SolidColorBrush(Color.FromArgb(0x40, 0x3e, 0x9e, 0xce));

    private readonly UniformGrid _chipsPanel;
    private readonly Dictionary<string, Border> _chipsByType;
    private readonly List<string> _currentTypeNames;
    private string? _selectedTypeName;

    /// <summary>Called with the standard type when "Add to current ..." is chosen on a chip.</summary>
    public Action<string>? AddToCurrentRequested { get; set; }

    /// <summary>Called with the standard type when "Edit defaults..." is chosen on a chip.</summary>
    public Action<string>? EditDefaultsRequested { get; set; }

    /// <summary>Returns the open Screen/Component name, or null if none is open.</summary>
    public Func<string?>? CurrentElementNameProvider { get; set; }

    /// <summary>Builds an empty palette.</summary>
    public AvaloniaStandardsPalette()
    {
        _chipsByType = new Dictionary<string, Border>();
        _currentTypeNames = new List<string>();
        BorderThickness = new Thickness(0, 1, 0, 0);
        BorderBrush = ChipBorderBrush;
        Padding = new Thickness(4, 6);
        Margin = new Thickness(0, 4, 0, 0);

        WrapPanel header = new WrapPanel { Margin = new Thickness(2, 0, 2, 6) };
        header.Children.Add(new TextBlock
        {
            Text = "STANDARDS",
            FontSize = 10.5,
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
        });
        header.Children.Add(new TextBlock
        {
            Text = "drag onto tree or canvas",
            FontSize = 10,
            FontStyle = FontStyle.Italic,
            Opacity = 0.6,
            VerticalAlignment = VerticalAlignment.Center,
        });

        _chipsPanel = new UniformGrid { Columns = 2 };
        _chipsPanel.SizeChanged += (_, e) => _chipsPanel.Columns = e.NewSize.Width >= TwoColumnMinWidth ? 2 : 1;

        StackPanel root = new StackPanel();
        root.Children.Add(header);
        root.Children.Add(_chipsPanel);
        Child = root;
    }

    /// <summary>Rebuilds the chips for the given standard types, in display order. Does nothing when they are unchanged.</summary>
    public void RefreshChips(IReadOnlyList<string> standardTypeNames)
    {
        if (standardTypeNames.SequenceEqual(_currentTypeNames))
        {
            return;
        }

        _chipsPanel.Children.Clear();
        _chipsByType.Clear();
        _currentTypeNames.Clear();
        _currentTypeNames.AddRange(standardTypeNames);
        foreach (string typeName in standardTypeNames)
        {
            Border chip = CreateChip(typeName);
            _chipsByType[typeName] = chip;
            _chipsPanel.Children.Add(chip);
        }
        SetSelectedStandardType(_selectedTypeName);
    }

    /// <summary>Highlights the chip being edited, or none when <paramref name="typeName"/> is null.</summary>
    public void SetSelectedStandardType(string? typeName)
    {
        _selectedTypeName = typeName;
        foreach (KeyValuePair<string, Border> pair in _chipsByType)
        {
            ApplySelectionVisual(pair.Value, pair.Key == typeName);
        }
    }

    private static void ApplySelectionVisual(Border chip, bool isSelected)
    {
        chip.BorderBrush = isSelected ? PrimaryBrush : ChipBorderBrush;
        chip.Background = isSelected ? SelectedFillBrush : Brushes.Transparent;
    }

    private Border CreateChip(string typeName)
    {
        StackPanel content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        if (AvaloniaTreeIcons.CreateIcon(typeName + ".png", ChipIconSize) is { } icon)
        {
            content.Children.Add(icon);
        }
        content.Children.Add(new TextBlock
        {
            Text = typeName,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });

        Border chip = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 5),
            Margin = new Thickness(2),
            Background = Brushes.Transparent,
            BorderBrush = ChipBorderBrush,
            Child = content,
        };
        ToolTip.SetTip(chip, $"Drag onto a Screen/Component or the canvas to add a {typeName}.\nCtrl+click to add it to the current Screen/Component.\nRight-click for more options.");
        chip.PointerEntered += (_, _) => chip.BorderBrush = PrimaryBrush;
        chip.PointerExited += (_, _) => ApplySelectionVisual(chip, _selectedTypeName == typeName);
        chip.ContextMenu = CreateChipContextMenu(typeName);
        WireDrag(chip, typeName);
        return chip;
    }

    private void WireDrag(Border chip, string typeName)
    {
        Point startPoint = default;
        bool pressed = false;

        chip.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(chip).Properties.IsLeftButtonPressed)
            {
                return;
            }
            // Ctrl+click adds an instance of this standard to the open Screen/Component without dragging.
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                AddToCurrentRequested?.Invoke(typeName);
                e.Handled = true;
                return;
            }
            pressed = true;
            startPoint = e.GetPosition(null);
        };
        chip.PointerReleased += (_, _) => pressed = false;
        chip.PointerMoved += async (_, e) =>
        {
            if (!pressed || !e.GetCurrentPoint(chip).Properties.IsLeftButtonPressed)
            {
                return;
            }
            Point current = e.GetPosition(null);
            if (Math.Abs(current.X - startPoint.X) < DragThreshold && Math.Abs(current.Y - startPoint.Y) < DragThreshold)
            {
                return;
            }

            pressed = false;
            chip.Background = SelectedFillBrush;
            try
            {
                DataTransfer data = new DataTransfer();
                data.Add(DataTransferItem.Create(AvaloniaDragFormats.StandardElementName, typeName));
                await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Copy);
            }
            catch (Exception)
            {
                // A failed or cancelled drag must never destabilize the tool.
            }
            finally
            {
                ApplySelectionVisual(chip, _selectedTypeName == typeName);
            }
        };
    }

    private ContextMenu CreateChipContextMenu(string typeName)
    {
        MenuItem addToCurrent = new MenuItem();
        addToCurrent.Click += (_, _) => AddToCurrentRequested?.Invoke(typeName);
        MenuItem editDefaults = new MenuItem { Header = "Edit defaults..." };
        editDefaults.Click += (_, _) => EditDefaultsRequested?.Invoke(typeName);

        ContextMenu menu = new ContextMenu();
        // The open element changes over the palette's lifetime, so resolve the label on each open.
        menu.Opening += (_, _) =>
        {
            string? currentElementName = CurrentElementNameProvider?.Invoke();
            addToCurrent.IsEnabled = currentElementName != null;
            addToCurrent.Header = currentElementName != null ? $"Add to {currentElementName}" : "Add to current element";
        };
        menu.Items.Add(addToCurrent);
        menu.Items.Add(new Separator());
        menu.Items.Add(editDefaults);
        return menu;
    }
}
