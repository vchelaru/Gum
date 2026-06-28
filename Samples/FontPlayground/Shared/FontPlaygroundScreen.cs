using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;

namespace FontPlayground;

/// <summary>
/// Builds the interactive dynamic-font playground UI. This class is written purely against
/// the Gum.Forms API so it compiles unchanged on every backend (MonoGame, raylib, etc.).
/// The host is responsible for initializing Gum and registering an
/// <see cref="RenderingLibrary.Graphics.Fonts.IInMemoryFontCreator"/> (e.g. KernSmith) so the
/// preview text can be re-rasterized live as the font settings change.
/// </summary>
public class FontPlaygroundScreen
{
    /// <summary>
    /// The curated list of system font families offered in the family drop-down. These all
    /// rasterize via KernSmith's GenerateFromSystem path on Windows; no bundled .ttf is needed.
    /// </summary>
    private static readonly List<string> FontFamilies = new()
    {
        "Arial",
        "Times New Roman",
        "Courier New",
        "Verdana",
        "Georgia",
        "Comic Sans MS",
    };

    private ComboBox _fontFamilyComboBox = null!;
    private Slider _fontSizeSlider = null!;
    private Label _fontSizeLabel = null!;
    private CheckBox _boldCheckBox = null!;
    private CheckBox _italicCheckBox = null!;
    private Slider _outlineSlider = null!;
    private Label _outlineLabel = null!;
    private CheckBox _smoothingCheckBox = null!;
    private CheckBox _dropshadowCheckBox = null!;
    private Slider _dropshadowOffsetXSlider = null!;
    private Slider _dropshadowOffsetYSlider = null!;
    private Slider _dropshadowBlurSlider = null!;
    private Label _dropshadowOffsetXLabel = null!;
    private Label _dropshadowOffsetYLabel = null!;
    private Label _dropshadowBlurLabel = null!;
    private TextRuntime _previewText = null!;

    /// <summary>
    /// Builds the font-playground controls and preview text into the given root and wires up
    /// live updates. Returns the created screen so the host can keep it alive if desired; the
    /// host can also safely ignore the return value because the wired event handlers keep the
    /// instance reachable through the visual tree.
    /// </summary>
    /// <param name="root">The root visual to add the playground UI to (e.g. GumService.Default.Root).</param>
    public static FontPlaygroundScreen Build(InteractiveGue root)
    {
        FontPlaygroundScreen screen = new FontPlaygroundScreen();
        screen.BuildInternal(root);
        return screen;
    }

    private void BuildInternal(InteractiveGue root)
    {
        StackPanel controlsPanel = new StackPanel();
        controlsPanel.Orientation = Orientation.Vertical;
        controlsPanel.Spacing = 6;
        controlsPanel.Visual.X = 16;
        controlsPanel.Visual.Y = 16;
        root.AddChild(controlsPanel);

        // Font family
        Label fontFamilyLabel = new Label();
        fontFamilyLabel.Text = "Font Family";
        controlsPanel.AddChild(fontFamilyLabel);

        _fontFamilyComboBox = new ComboBox();
        _fontFamilyComboBox.Width = 240;
        foreach (string family in FontFamilies)
        {
            _fontFamilyComboBox.Items.Add(family);
        }
        _fontFamilyComboBox.SelectedObject = "Arial";
        _fontFamilyComboBox.SelectionChanged += (_, _) => ApplyFontSettings();
        controlsPanel.AddChild(_fontFamilyComboBox);

        // Font size
        _fontSizeLabel = new Label();
        controlsPanel.AddChild(_fontSizeLabel);

        _fontSizeSlider = new Slider();
        _fontSizeSlider.Width = 240;
        _fontSizeSlider.Minimum = 8;
        _fontSizeSlider.Maximum = 96;
        _fontSizeSlider.IsSnapToTickEnabled = true;
        _fontSizeSlider.TicksFrequency = 1;
        _fontSizeSlider.Value = 24;
        _fontSizeSlider.ValueChanged += (_, _) => ApplyFontSettings();
        controlsPanel.AddChild(_fontSizeSlider);

        // Bold / Italic
        _boldCheckBox = new CheckBox();
        _boldCheckBox.Text = "Bold";
        _boldCheckBox.Width = 240;
        _boldCheckBox.Checked += (_, _) => ApplyFontSettings();
        _boldCheckBox.Unchecked += (_, _) => ApplyFontSettings();
        controlsPanel.AddChild(_boldCheckBox);

        _italicCheckBox = new CheckBox();
        _italicCheckBox.Text = "Italic";
        _italicCheckBox.Width = 240;
        _italicCheckBox.Checked += (_, _) => ApplyFontSettings();
        _italicCheckBox.Unchecked += (_, _) => ApplyFontSettings();
        controlsPanel.AddChild(_italicCheckBox);

        // Outline thickness
        _outlineLabel = new Label();
        controlsPanel.AddChild(_outlineLabel);

        _outlineSlider = new Slider();
        _outlineSlider.Width = 240;
        _outlineSlider.Minimum = 0;
        _outlineSlider.Maximum = 8;
        _outlineSlider.IsSnapToTickEnabled = true;
        _outlineSlider.TicksFrequency = 1;
        _outlineSlider.Value = 0;
        _outlineSlider.ValueChanged += (_, _) => ApplyFontSettings();
        controlsPanel.AddChild(_outlineSlider);

        // Smoothing
        _smoothingCheckBox = new CheckBox();
        _smoothingCheckBox.Text = "Use Font Smoothing";
        _smoothingCheckBox.Width = 240;
        _smoothingCheckBox.IsChecked = true;
        _smoothingCheckBox.Checked += (_, _) => ApplyFontSettings();
        _smoothingCheckBox.Unchecked += (_, _) => ApplyFontSettings();
        controlsPanel.AddChild(_smoothingCheckBox);

        // Drop shadow (KernSmith baked atlas)
        _dropshadowCheckBox = new CheckBox();
        _dropshadowCheckBox.Text = "Drop Shadow";
        _dropshadowCheckBox.Width = 240;
        _dropshadowCheckBox.Checked += (_, _) => ApplyFontSettings();
        _dropshadowCheckBox.Unchecked += (_, _) => ApplyFontSettings();
        controlsPanel.AddChild(_dropshadowCheckBox);

        _dropshadowOffsetXLabel = new Label();
        controlsPanel.AddChild(_dropshadowOffsetXLabel);

        _dropshadowOffsetXSlider = new Slider();
        _dropshadowOffsetXSlider.Width = 240;
        _dropshadowOffsetXSlider.Minimum = 0;
        _dropshadowOffsetXSlider.Maximum = 8;
        _dropshadowOffsetXSlider.IsSnapToTickEnabled = true;
        _dropshadowOffsetXSlider.TicksFrequency = 1;
        _dropshadowOffsetXSlider.Value = 2;
        _dropshadowOffsetXSlider.ValueChanged += (_, _) => ApplyFontSettings();
        controlsPanel.AddChild(_dropshadowOffsetXSlider);

        _dropshadowOffsetYLabel = new Label();
        controlsPanel.AddChild(_dropshadowOffsetYLabel);

        _dropshadowOffsetYSlider = new Slider();
        _dropshadowOffsetYSlider.Width = 240;
        _dropshadowOffsetYSlider.Minimum = 0;
        _dropshadowOffsetYSlider.Maximum = 8;
        _dropshadowOffsetYSlider.IsSnapToTickEnabled = true;
        _dropshadowOffsetYSlider.TicksFrequency = 1;
        _dropshadowOffsetYSlider.Value = 2;
        _dropshadowOffsetYSlider.ValueChanged += (_, _) => ApplyFontSettings();
        controlsPanel.AddChild(_dropshadowOffsetYSlider);

        _dropshadowBlurLabel = new Label();
        controlsPanel.AddChild(_dropshadowBlurLabel);

        _dropshadowBlurSlider = new Slider();
        _dropshadowBlurSlider.Width = 240;
        _dropshadowBlurSlider.Minimum = 0;
        _dropshadowBlurSlider.Maximum = 8;
        _dropshadowBlurSlider.IsSnapToTickEnabled = true;
        _dropshadowBlurSlider.TicksFrequency = 1;
        _dropshadowBlurSlider.Value = 2;
        _dropshadowBlurSlider.ValueChanged += (_, _) => ApplyFontSettings();
        controlsPanel.AddChild(_dropshadowBlurSlider);

        // Preview
        _previewText = new TextRuntime();
        _previewText.Text = "The quick brown fox 0123";
        _previewText.X = 290;
        _previewText.Y = 24;
        // White text. Red/Green/Blue/Alpha are platform-neutral int properties (unlike the
        // backend-specific Color property), so setting them here keeps this file portable.
        _previewText.Red = 255;
        _previewText.Green = 255;
        _previewText.Blue = 255;
        _previewText.Alpha = 255;
        root.Children.Add(_previewText);

        ApplyFontSettings();
    }

    /// <summary>
    /// Pushes the current control values onto the preview <see cref="TextRuntime"/>. Each font
    /// property setter triggers Gum's UpdateToFontValues, which asks the registered in-memory
    /// font creator to (re)generate the atlas, producing the live re-render.
    /// </summary>
    private void ApplyFontSettings()
    {
        int fontSize = (int)_fontSizeSlider.Value;
        int outlineThickness = (int)_outlineSlider.Value;
        int shadowOffsetX = (int)_dropshadowOffsetXSlider.Value;
        int shadowOffsetY = (int)_dropshadowOffsetYSlider.Value;
        int shadowBlur = (int)_dropshadowBlurSlider.Value;

        _fontSizeLabel.Text = $"Font Size: {fontSize}";
        _outlineLabel.Text = $"Outline Thickness: {outlineThickness}";
        _dropshadowOffsetXLabel.Text = $"Shadow Offset X: {shadowOffsetX}";
        _dropshadowOffsetYLabel.Text = $"Shadow Offset Y: {shadowOffsetY}";
        _dropshadowBlurLabel.Text = $"Shadow Blur: {shadowBlur}";

        _previewText.Font = _fontFamilyComboBox.SelectedObject as string ?? "Arial";
        _previewText.FontSize = fontSize;
        _previewText.IsBold = _boldCheckBox.IsChecked == true;
        _previewText.IsItalic = _italicCheckBox.IsChecked == true;
        _previewText.OutlineThickness = outlineThickness;
        _previewText.UseFontSmoothing = _smoothingCheckBox.IsChecked == true;
        _previewText.HasDropshadow = _dropshadowCheckBox.IsChecked == true;
        _previewText.DropshadowOffsetX = shadowOffsetX;
        _previewText.DropshadowOffsetY = shadowOffsetY;
        _previewText.DropshadowBlur = shadowBlur;
    }
}
