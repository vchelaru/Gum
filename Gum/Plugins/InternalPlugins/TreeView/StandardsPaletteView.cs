using Gum.Managers;
using System;
using System.Collections.Generic;
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
    private readonly Func<string, ImageSource?> _iconResolver;
    private readonly UniformGrid _chipsPanel;

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

        DockPanel header = new DockPanel { Margin = new Thickness(2, 0, 2, 6) };
        TextBlock title = new TextBlock
        {
            Text = "STANDARDS",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        title.SetResourceReference(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground");
        TextBlock hint = new TextBlock
        {
            Text = "drag onto tree or canvas",
            FontSize = 10,
            FontStyle = FontStyles.Italic,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.6
        };
        hint.SetResourceReference(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground");
        DockPanel.SetDock(hint, Dock.Right);
        header.Children.Add(hint);
        header.Children.Add(title);

        _chipsPanel = new UniformGrid { Columns = 2 };

        root.Children.Add(header);
        root.Children.Add(_chipsPanel);
        Child = root;
    }

    /// <summary>
    /// Rebuilds the chips from the given standard type names (in display order). Safe to call on
    /// project load and whenever the available standards change.
    /// </summary>
    public void RefreshChips(IReadOnlyList<string> standardTypeNames)
    {
        _chipsPanel.Children.Clear();
        foreach (string typeName in standardTypeNames)
        {
            _chipsPanel.Children.Add(CreateChip(typeName));
        }
    }

    private Border CreateChip(string typeName)
    {
        StackPanel content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        ImageSource? icon = _iconResolver(typeName + ".png");
        if (icon != null)
        {
            content.Children.Add(new Image
            {
                Source = icon,
                Width = 14,
                Height = 14,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
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

        // Hover highlight.
        chip.MouseEnter += (_, _) =>
        {
            if (Application.Current?.TryFindResource("Frb.Brushes.Primary") is Brush primary)
            {
                chip.BorderBrush = primary;
            }
        };
        chip.MouseLeave += (_, _) => chip.SetResourceReference(Border.BorderBrushProperty, "Frb.Brushes.Border");

        WireDrag(chip, typeName);
        chip.ContextMenu = CreateChipContextMenu(typeName);

        return chip;
    }

    private void WireDrag(UIElement chip, string typeName)
    {
        Point startPoint = default;
        bool pressed = false;

        chip.PreviewMouseLeftButtonDown += (_, e) =>
        {
            pressed = true;
            startPoint = e.GetPosition(null);
        };
        chip.PreviewMouseLeftButtonUp += (_, _) => pressed = false;
        chip.MouseMove += (sender, e) =>
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
            DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Copy);
        };
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
