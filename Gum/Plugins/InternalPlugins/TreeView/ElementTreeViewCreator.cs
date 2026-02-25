using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.Controls;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Color = System.Drawing.Color;
using DragEventHandler = System.Windows.Forms.DragEventHandler;
using GiveFeedbackEventHandler = System.Windows.Forms.GiveFeedbackEventHandler;
using Grid = System.Windows.Controls.Grid;
using Point = System.Drawing.Point;
using QueryContinueDragEventHandler = System.Windows.Forms.QueryContinueDragEventHandler;
using Size = System.Drawing.Size;
using WpfInput = System.Windows.Input;

namespace Gum.Managers;

/// <summary>
/// Encapsulates all UI creation and theming logic for the element tree view.
/// Extracted from <see cref="ElementTreeViewManager"/> to reduce its size.
/// </summary>
internal class ElementTreeViewCreator
{
    #region Fields

    private Dictionary<string, Image> _originalImages = new();

    #endregion

    #region Properties

    internal MultiSelectTreeView ObjectTreeView { get; private set; } = null!;
    internal System.Windows.Controls.ContextMenu ContextMenu { get; private set; } = null!;
    internal FlatSearchListBox FlatList { get; private set; } = null!;
    internal System.Windows.Forms.Integration.WindowsFormsHost TreeViewHost { get; private set; } = null!;
    internal System.Windows.Controls.TextBox SearchTextBox { get; private set; } = null!;
    internal System.Windows.Controls.CheckBox DeepSearchCheckBox { get; private set; } = null!;
    internal System.Windows.Controls.Button CollapseAllButton { get; private set; } = null!;
    internal System.Windows.Controls.Button CollapseToElementButton { get; private set; } = null!;

    #endregion

    /// <summary>
    /// Builds the full Grid layout containing the tree view, search box, collapse buttons, and flat search list.
    /// </summary>
    /// <param name="onAfterClickSelect">Handler for AfterClickSelect on the tree view.</param>
    /// <param name="onAfterSelect">Handler for AfterSelect on the tree view.</param>
    /// <param name="onKeyDown">Handler for KeyDown on the tree view.</param>
    /// <param name="onKeyPress">Handler for KeyPress on the tree view.</param>
    /// <param name="onMouseClick">Handler for MouseClick on the tree view.</param>
    /// <param name="onMouseMove">Handler for mouse move (x, y) on the tree view.</param>
    /// <param name="onFontChanged">Handler for FontChanged on the tree view.</param>
    /// <param name="onDragOver">Handler for DragOver on the tree view.</param>
    /// <param name="onDragDrop">Handler for DragDrop on the tree view.</param>
    /// <param name="onQueryContinueDrag">Handler for QueryContinueDrag on the tree view.</param>
    /// <param name="onValidateSortingDrop">Handler for ValidateSortingDrop on the tree view.</param>
    /// <param name="onNodeSortingDropped">Handler for NodeSortingDropped on the tree view.</param>
    /// <param name="onGiveFeedback">Handler for GiveFeedback on the tree view.</param>
    /// <param name="onFilterTextChanged">Called when search text changes, with the new text.</param>
    /// <param name="onSearchNodeSelected">Called when a search result is selected.</param>
    /// <param name="onCollapseAll">Called when Collapse All button is clicked.</param>
    /// <param name="onCollapseToElementLevel">Called when Collapse to Element Level button is clicked.</param>
    /// <param name="onDeepSearchChecked">Called when deep search checkbox is checked.</param>
    public Grid CreateView(
        TreeViewEventHandler onAfterClickSelect,
        TreeViewEventHandler onAfterSelect,
        KeyEventHandler onKeyDown,
        KeyPressEventHandler onKeyPress,
        MouseEventHandler onMouseClick,
        Action<int, int> onMouseMove,
        EventHandler onFontChanged,
        DragEventHandler onDragOver,
        DragEventHandler onDragDrop,
        QueryContinueDragEventHandler onQueryContinueDrag,
        EventHandler<MultiSelectTreeView.ValidateDropEventArgs> onValidateSortingDrop,
        EventHandler<MultiSelectTreeView.DroppingEventArgs> onNodeSortingDropped,
        GiveFeedbackEventHandler onGiveFeedback,
        Action<string?> onFilterTextChanged,
        Action<SearchItemViewModel> onSearchNodeSelected,
        Action onCollapseAll,
        Action onCollapseToElementLevel,
        Action onDeepSearchChecked)
    {
        CreateObjectTreeView(
            onAfterClickSelect, onAfterSelect, onKeyDown, onKeyPress,
            onMouseClick, onMouseMove, onFontChanged, onDragOver, onDragDrop,
            onQueryContinueDrag, onValidateSortingDrop, onNodeSortingDropped, onGiveFeedback);

        CreateContextMenu();

        var grid = new Grid();
        grid.Margin = new Thickness(4);
        grid.RowDefinitions.Add(
            new System.Windows.Controls.RowDefinition()
            { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(
            new System.Windows.Controls.RowDefinition()
            { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(
            new System.Windows.Controls.RowDefinition()
                { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(
            new System.Windows.Controls.RowDefinition()
            { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });

        ObjectTreeView.Dock = DockStyle.Fill;

        TreeViewHost = new System.Windows.Forms.Integration.WindowsFormsHost();
        TreeViewHost.Background = System.Windows.Media.Brushes.Transparent;

        ThemedScrollContainer scrollContainer = new()
        {
            AutoComputeExtent = false,
            Dock = DockStyle.Fill,
            EnableHorizontalScroll = true
        };
        scrollContainer.AddContent(ObjectTreeView);
        scrollContainer.WireTreeToScroller(ObjectTreeView);

        TreeViewHost.Child = scrollContainer;
        TreeViewHost.Margin = new Thickness(0, 4, 0, 0);

        Grid.SetRow(TreeViewHost, 3);
        grid.Children.Add(TreeViewHost);

        var buttonPanel = CreateCollapseButtonsPanel(onCollapseAll, onCollapseToElementLevel);
        Grid.SetRow(buttonPanel, 0);
        grid.Children.Add(buttonPanel);

        var searchBarUi = CreateSearchBoxUi(onFilterTextChanged, onSearchNodeSelected);
        Grid.SetRow(searchBarUi, 1);
        grid.Children.Add(searchBarUi);

        var checkBoxUi = CreateSearchCheckBoxUi(onDeepSearchChecked);
        checkBoxUi.Visibility = Visibility.Collapsed;
        checkBoxUi.Focusable = false;
        checkBoxUi.Margin = new Thickness(0, 2, 0, 0);

        Grid.SetRow(checkBoxUi, 2);
        grid.Children.Add(checkBoxUi);

        FlatList = CreateFlatSearchList(onSearchNodeSelected);
        FlatList.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
        FlatList.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
        FlatList.Margin = new(0, 4, 0, 0);
        FlatList.Visibility = Visibility.Collapsed;

        Grid.SetRow(FlatList, 3);
        grid.Children.Add(FlatList);

        searchBarUi.GotKeyboardFocus += (_, _) => UpdateCheckBoxVisibility();
        searchBarUi.LostKeyboardFocus += (_, _) => UpdateCheckBoxVisibility();
        FlatList.IsVisibleChanged += (_, _) => UpdateCheckBoxVisibility();
        void UpdateCheckBoxVisibility()
        {
            bool textBoxFocused = SearchTextBox.IsKeyboardFocusWithin;
            bool listViewVisible = FlatList.Visibility == Visibility.Visible;

            checkBoxUi.Visibility = (textBoxFocused || listViewVisible)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        ApplyThemeColors();

        return grid;
    }

    private void CreateObjectTreeView(
        TreeViewEventHandler onAfterClickSelect,
        TreeViewEventHandler onAfterSelect,
        KeyEventHandler onKeyDown,
        KeyPressEventHandler onKeyPress,
        MouseEventHandler onMouseClick,
        Action<int, int> onMouseMove,
        EventHandler onFontChanged,
        DragEventHandler onDragOver,
        DragEventHandler onDragDrop,
        QueryContinueDragEventHandler onQueryContinueDrag,
        EventHandler<MultiSelectTreeView.ValidateDropEventArgs> onValidateSortingDrop,
        EventHandler<MultiSelectTreeView.DroppingEventArgs> onNodeSortingDropped,
        GiveFeedbackEventHandler onGiveFeedback)
    {
        this.ObjectTreeView = new CommonFormsAndControls.MultiSelectTreeView();
        this.ObjectTreeView.IsSelectingOnPush = false;
        this.ObjectTreeView.AllowDrop = true;
        this.ObjectTreeView.AlwaysHaveOneNodeSelected = false;
        // External drag/drop logic is provided; disable native reorder for this host
        this.ObjectTreeView.EnableNativeReorder = true;
        this.ObjectTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.ObjectTreeView.HotTracking = true;
        this.ObjectTreeView.ImageIndex = 0;
        this.ObjectTreeView.ImageList = ObjectTreeView.ElementTreeImageList;
        this.ObjectTreeView.Location = new System.Drawing.Point(0, 0);
        this.ObjectTreeView.MultiSelectBehavior = CommonFormsAndControls.MultiSelectBehavior.CtrlDown;
        this.ObjectTreeView.Name = "ObjectTreeView";
        this.ObjectTreeView.SelectedImageIndex = 0;
        this.ObjectTreeView.Size = new System.Drawing.Size(196, 621);
        this.ObjectTreeView.TabIndex = 0;
        this.ObjectTreeView.AfterClickSelect += onAfterClickSelect;
        this.ObjectTreeView.AfterSelect += onAfterSelect;
        this.ObjectTreeView.KeyDown += onKeyDown;
        this.ObjectTreeView.KeyPress += onKeyPress;
        this.ObjectTreeView.PreviewKeyDown += (_, _) => { };
        this.ObjectTreeView.MouseClick += onMouseClick;
        this.ObjectTreeView.BackColor =
            Application.Current.TryFindResource("Frb.Colors.SurfaceO1") is System.Windows.Media.Color color
                ? System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B)
                : System.Drawing.SystemColors.Window;
        this.ObjectTreeView.LineColor = ObjectTreeView.BackColor;

        this.ObjectTreeView.MouseMove += (sender, e) => onMouseMove(e.X, e.Y);
        this.ObjectTreeView.FontChanged += onFontChanged;
        this.ObjectTreeView.BorderStyle = BorderStyle.None;

        ObjectTreeView.DragOver += onDragOver;
        ObjectTreeView.DragDrop += onDragDrop;
        ObjectTreeView.QueryContinueDrag += onQueryContinueDrag;
        ObjectTreeView.ValidateSortingDrop += onValidateSortingDrop;
        ObjectTreeView.NodeSortingDropped += onNodeSortingDropped;
        ObjectTreeView.GiveFeedback += onGiveFeedback;
    }

    private void CreateContextMenu()
    {
        this.ContextMenu = new System.Windows.Controls.ContextMenu();
    }

    internal void ApplyThemeColors()
    {
        if (System.Windows.Application.Current is { } current &&
            current.TryFindResource("Frb.Brushes.Foreground") is System.Windows.Media.SolidColorBrush { Color: var fg } &&
            current.TryFindResource("Frb.Surface01") is System.Windows.Media.SolidColorBrush { Color: var field } bgBrush)
        {
            Color foregroundColor = Color.FromArgb(fg.A, fg.R, fg.G, fg.B);
            Color fieldColor = Color.FromArgb(field.A, field.R, field.G, field.B);
            this.ObjectTreeView.ForeColor = foregroundColor;
            this.ObjectTreeView.BackColor = fieldColor;
            this.ObjectTreeView.LineColor = ObjectTreeView.BackColor;
            this.TreeViewHost.Background = bgBrush;
            (TreeViewHost.Child as ThemedScrollContainer)!.BackColor = fieldColor;

            if (current.TryFindResource("Frb.Brushes.Primary.Transparent") is System.Windows.Media.SolidColorBrush
                {
                    Opacity: var primOpacity
                } T &&
                current.TryFindResource("Frb.Brushes.Primary") is System.Windows.Media.SolidColorBrush { Color: var primColor })
            {
                this.ObjectTreeView.HoverBgColor =
                    Color.FromArgb(Map01To255(primOpacity), primColor.R, primColor.G, primColor.B);
                this.ObjectTreeView.SelectedBorderColor =
                    Color.FromArgb(primColor.A, primColor.R, primColor.G, primColor.B);

                const float defaultFontSize = 9f;
                UpdateTreeviewIcons(ObjectTreeView.Font.Size / defaultFontSize);
            }
        }

        static int Map01To255(double value)
        {
            // clamp just in case
            if (value < 0) value = 0;
            if (value > 1) value = 1;

            return (int)Math.Round(value * 255);
        }
    }

    internal void UpdateTreeviewIcons(
        float scale = 1.0f)
    {
        float baseImageSize = 16;

        using (var g = ObjectTreeView.CreateGraphics())
        {
            baseImageSize *= (g.DpiX / 96f);
        }

        var size = new Size((int)(baseImageSize * scale), (int)(baseImageSize * scale));

        InjectDynamicIcons();

        var keyedColors = GetCurrentColorMap();
        Application app = Application.Current;
        Color? defaultColor = null;
        if (app.TryFindResource("Frb.Colors.Primary") is System.Windows.Media.Color dc)
        {
            defaultColor = Color.FromArgb(dc.A, dc.R, dc.G, dc.B);
        }

        ObjectTreeView.ImageList = BuildTintedImageList(_originalImages, size, keyedColors, defaultColor ?? Color.White);

        // for some reason, after the .net upgrade, the indent doesn't auto-adjust to account
        // for the size of the images on first load, so we just force it here every time, despite
        // it playing nice with follow-up size changes.
        ObjectTreeView.Indent = (int)baseImageSize;

        ImageList BuildTintedImageList(
            Dictionary<string, Image> originalImages,
            Size newSize,
            IDictionary<string, Color>? perKeyColors,
            Color fallbackColor)
        {
            var outList = new ImageList
            {
                ImageSize = newSize,
                ColorDepth = ColorDepth.Depth32Bit
            };

            foreach (var kvp in originalImages)
            {
                var key = kvp.Key;
                var src = kvp.Value;

                // pick the color for this key (fallback if none specified)
                var tint = (perKeyColors != null && perKeyColors.TryGetValue(key, out var c)) ? c : fallbackColor;

                // resize + tint in one pass
                var tinted = ResizeAndTint(src, newSize, tint);

                // ImageList takes ownership of the Image; don't dispose tinted here
                outList.Images.Add(key, tinted);
            }

            return outList;
        }

        static Bitmap ResizeAndTint(Image original, Size newSize, Color tint)
        {
            // Normalize multipliers: white(1,1,1) * (r,g,b) => tint
            float r = tint.R / 255f;
            float g = tint.G / 255f;
            float b = tint.B / 255f;
            float a = tint.A / 255f; // scales source alpha; use 1.0f to keep original alpha

            var cm = new ColorMatrix(new float[][]
            {
            new float[] { r, 0, 0, 0, 0 },
            new float[] { 0, g, 0, 0, 0 },
            new float[] { 0, 0, b, 0, 0 },
            new float[] { 0, 0, 0, a, 0 },
            new float[] { 0, 0, 0, 0, 1 }
            });

            using var ia = new ImageAttributes();
            ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            // 32bpp ARGB ensures we keep transparency nice and crisp
            var dest = new Bitmap(newSize.Width, newSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var graphics = System.Drawing.Graphics.FromImage(dest))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                var rect = new Rectangle(Point.Empty, newSize);
                // Draw with the color matrix applied
                graphics.DrawImage(original,
                            destRect: rect,
                            srcX: 0, srcY: 0, srcWidth: original.Width, srcHeight: original.Height,
                            srcUnit: GraphicsUnit.Pixel,
                            imageAttr: ia);
            }

            return dest;
        }

        void InjectDynamicIcons()
        {
            TryInjectIcon("transparent.png",
                "pack://application:,,,/Gum;component/Content/Icons/transparent.png");
            TryInjectIcon("Folder.png",
                "pack://application:,,,/Gum;component/Content/Icons/UpdatedTreeViewIcons/folder.png");
            TryInjectIcon("Component.png",
                "pack://application:,,,/Gum;component/Content/Icons/UpdatedTreeViewIcons/Component.png");
            TryInjectIcon("Instance.png",
                "pack://application:,,,/Gum;component/Content/Icons/UpdatedTreeViewIcons/Instance.png");
            TryInjectIcon("Screen.png",
                "pack://application:,,,/Gum;component/Content/Icons/UpdatedTreeViewIcons/screen.png");
            TryInjectIcon("StandardElement.png",
                "pack://application:,,,/Gum;component/Content/Icons/UpdatedTreeViewIcons/StandardElement.png");
            TryInjectIcon("redExclamation.png",
                "pack://application:,,,/Gum;component/Content/Icons/redExclamation.png");
            TryInjectIcon("state.png",
                "pack://application:,,,/Gum;component/Content/Icons/state.png");
            TryInjectIcon("behavior.png",
                "pack://application:,,,/Gum;component/Content/Icons/UpdatedTreeViewIcons/behavior.png");
            TryInjectIcon("InheritedInstance.png",
                "pack://application:,,,/Gum;component/Content/Icons/InheritedInstance.png");
            TryInjectIcon("instance_locked.png",
                "pack://application:,,,/Gum;component/Content/Icons/UpdatedTreeViewIcons/instance_locked.png");
        }

        void TryInjectIcon(string key, string packUri)
        {
            if (_originalImages.ContainsKey(key)) return;

            var streamInfo = Application.GetResourceStream(new Uri(packUri));
            if (streamInfo == null) return;

            using var stream = streamInfo.Stream;
            _originalImages[key] = Image.FromStream(stream);
        }

        static Dictionary<string, Color> GetCurrentColorMap()
        {
            Application app = Application.Current;

            var manillaColor = (System.Windows.Media.Color)app.FindResource("Frb.Colors.Icon.Manilla");
            var greenColor = (System.Windows.Media.Color)app.FindResource("Frb.Colors.Icon.Green");
            var blueColor = (System.Windows.Media.Color)app.FindResource("Frb.Colors.Icon.Blue");
            var redColor = (System.Windows.Media.Color)app.FindResource("Frb.Colors.Icon.Red");
            var purpleColor = (System.Windows.Media.Color)app.FindResource("Frb.Colors.Icon.Purple");

            var manilla = System.Drawing.Color.FromArgb(manillaColor.A, manillaColor.R, manillaColor.G, manillaColor.B);
            var green = System.Drawing.Color.FromArgb(greenColor.A, greenColor.R, greenColor.G, greenColor.B);
            var blue = System.Drawing.Color.FromArgb(blueColor.A, blueColor.R, blueColor.G, blueColor.B);
            var red = System.Drawing.Color.FromArgb(redColor.A, redColor.R, redColor.G, redColor.B);
            var purple = System.Drawing.Color.FromArgb(purpleColor.A, purpleColor.R, purpleColor.G, purpleColor.B);

            return new()
            {
                ["Folder.png"] = manilla,
                ["Component.png"] = green,
                ["Instance.png"] = blue,
                ["instance_locked.png"] = blue,
                ["Screen.png"] = red,
                ["StandardElement.png"] = purple,
                ["redExclamation.png"] = red,
                ["state.png"] = blue,
                ["behavior.png"] = manilla,
            };
        }
    }

    private FlatSearchListBox CreateFlatSearchList(Action<SearchItemViewModel> onSearchNodeSelected)
    {
        var list = new FlatSearchListBox();
        list.SelectSearchNode += onSearchNodeSelected;
        return list;
    }

    private System.Windows.Controls.TextBox CreateSearchBoxUi(
        Action<string?> onFilterTextChanged,
        Action<SearchItemViewModel> onSearchNodeSelected)
    {
        SearchTextBox = new System.Windows.Controls.TextBox();
        SearchTextBox.SetValue(TextFieldAssist.HasClearButtonProperty, true);
        SearchTextBox.SetValue(HintAssist.HintProperty, "Search...");
        SearchTextBox.SetValue(HintAssist.IsFloatingProperty, false);
        SearchTextBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
        SearchTextBox.TextChanged += (_, _) => onFilterTextChanged(SearchTextBox.Text);
        SearchTextBox.PreviewKeyDown += (sender, args) =>
        {
            bool isCtrlDown = WpfInput.Keyboard.IsKeyDown(WpfInput.Key.LeftCtrl) || WpfInput.Keyboard.IsKeyDown(WpfInput.Key.RightCtrl);

            if (args.Key == WpfInput.Key.Escape)
            {
                SearchTextBox.Text = null;
                args.Handled = true;
                ObjectTreeView.Focus();
            }
            else if (args.Key == WpfInput.Key.Back
             && isCtrlDown)
            {
                SearchTextBox.Text = null;
                args.Handled = true;
            }
            else if (args.Key == WpfInput.Key.Down)
            {
                if(FlatList.FlatList.SelectedIndex < FlatList.FlatList.Items.Count -1)
                {
                    FlatList.FlatList.SelectedIndex++;
                    BringSelectedIntoView();
                }
                args.Handled = true;
            }
            else if (args.Key == WpfInput.Key.Up)
            {
                if (FlatList.FlatList.SelectedIndex > 0)
                {
                    FlatList.FlatList.SelectedIndex--;
                    BringSelectedIntoView();
                }
                args.Handled = true;
            }
            else if (args.Key == WpfInput.Key.Enter)
            {
                args.Handled = true;
                ObjectTreeView.Focus();

                var selectedItem = FlatList.FlatList.SelectedItem as SearchItemViewModel;
                if(selectedItem != null)
                {
                    onSearchNodeSelected(selectedItem);

                    SearchTextBox.Text = null;
                }
            }
        };

        return SearchTextBox;

        void BringSelectedIntoView()
        {
            if (FlatList.FlatList.SelectedItem is { } selected)
            {
                FlatList.Dispatcher.BeginInvoke(() => FlatList.FlatList.ScrollIntoView(selected),
                    DispatcherPriority.Loaded);
            }
        }
    }

    private System.Windows.Controls.StackPanel CreateCollapseButtonsPanel(
        Action onCollapseAll,
        Action onCollapseToElementLevel)
    {
        var panel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 4)
        };

        CollapseAllButton = new System.Windows.Controls.Button
        {
            Content = new PackIcon
            {
                Kind = PackIconKind.UnfoldLessHorizontal,
                Width = 14,
                Height = 14
            },
            Margin = new Thickness(0, 0, 4, 0),
            Padding = new Thickness(4, 2, 4, 2),
            ToolTip = "Collapse all nodes in the tree",
            Style = Application.Current.TryFindResource("MaterialDesignToolForegroundButton") as System.Windows.Style
        };
        RippleAssist.SetIsDisabled(CollapseAllButton, true);
        CollapseAllButton.Click += (_, _) => onCollapseAll();

        CollapseToElementButton = new System.Windows.Controls.Button
        {
            Content = new PackIcon
            {
                Kind = PackIconKind.FileTree,
                Width = 14,
                Height = 14
            },
            Margin = new Thickness(0, 0, 4, 0),
            Padding = new Thickness(4, 2, 4, 2),
            ToolTip = "Collapse to element level (preserves folder expansion state)",
            Style = Application.Current.TryFindResource("MaterialDesignToolForegroundButton") as System.Windows.Style
        };
        RippleAssist.SetIsDisabled(CollapseToElementButton, true);
        CollapseToElementButton.Click += (_, _) => onCollapseToElementLevel();

        panel.Children.Add(CollapseAllButton);
        panel.Children.Add(CollapseToElementButton);

        return panel;
    }

    private const double DefaultBaseFontSize = 12.0;
    private const double DefaultIconHeight = 14.0;

    internal void UpdateCollapseButtonSizes(double baseFontSize)
    {
        double scale = baseFontSize / DefaultBaseFontSize;
        double iconHeight = DefaultIconHeight * scale;

        if (CollapseAllButton?.Content is PackIcon collapseAllIcon)
        {
            collapseAllIcon.Width = iconHeight;
            collapseAllIcon.Height = iconHeight;
        }

        if (CollapseToElementButton?.Content is PackIcon collapseToElementIcon)
        {
            collapseToElementIcon.Width = iconHeight;
            collapseToElementIcon.Height = iconHeight;
        }
    }

    private System.Windows.Controls.CheckBox CreateSearchCheckBoxUi(Action onDeepSearchChecked)
    {
        DeepSearchCheckBox = new System.Windows.Controls.CheckBox();
        DeepSearchCheckBox.IsChecked = false;
        DeepSearchCheckBox.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
        DeepSearchCheckBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
        DeepSearchCheckBox.Content = "Include Variables";
        DeepSearchCheckBox.Checked += (_, _) => onDeepSearchChecked();

        return DeepSearchCheckBox;
    }

    internal void CollapseAll()
    {
        ObjectTreeView.CollapseAll();
    }

    internal void CollapseToElementLevel()
    {
        // Recursively collapse only element nodes (nodes with Tag != null)
        // This preserves the expansion state of all folder nodes
        CollapseElementNodesRecursively(ObjectTreeView.Nodes);
    }

    private void CollapseElementNodesRecursively(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            // If this node has a Tag, it's an element (Screen, Component, Behavior, Instance)
            // so we should collapse it
            if (node.Tag != null && node.Tag is not FolderType)
            {
                node.Collapse();
            }
            // If it's a folder node (top-level or subfolder), leave it alone but recurse into its children
            else if ((node.IsTopElementContainerTreeNode() ||
                      node.IsScreensFolderTreeNode() ||
                      node.IsComponentsFolderTreeNode()) &&
                     node.Nodes.Count > 0)
            {
                CollapseElementNodesRecursively(node.Nodes);
            }
        }
    }
}
