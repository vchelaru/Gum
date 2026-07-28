#pragma warning disable CS0618, GUM001 // Default visuals intentionally use deprecated MonoGameGum.GueDeriving shim types for backward compatibility until V1/V2/V3 visuals are retired. See issue #2715.
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for a <see cref="ColorPicker"/> control. Builds a saturation/value square and
/// a hue bar, each backed by a procedurally generated texture, plus the indicators the control
/// moves to reflect the selected color. This visual is XNA-family only (MonoGame/KNI/FNA); other
/// backends require their own texture-generation implementation.
/// </summary>
public class ColorPickerVisual : InteractiveGue, IColorPickerVisual
{
    private const int SaturationValueSize = 160;
    private const int HueBarWidth = 20;
    private const int HueBarHeight = 160;
    private const int Spacing = 4;

    /// <summary>
    /// The interactive square displaying every saturation/value combination for the current hue.
    /// </summary>
    public ContainerRuntime SaturationValueContainer { get; private set; }

    /// <summary>
    /// The interactive vertical bar displaying the full range of hues.
    /// </summary>
    public ContainerRuntime HueContainer { get; private set; }

    private SpriteRuntime _saturationValueDisplay;
    private SpriteRuntime _hueDisplay;

    private Texture2D? _saturationValueTexture;
    private Texture2D? _hueTexture;
    private Color[]? _saturationValuePixels;

    /// <summary>
    /// Returns the strongly-typed ColorPicker Forms control backing this visual.
    /// </summary>
    public ColorPicker FormsControl => (ColorPicker)FormsControlAsObject;

    /// <summary>
    /// Creates a new ColorPickerVisual, optionally building the backing Forms control.
    /// </summary>
    public ColorPickerVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        Width = SaturationValueSize + Spacing + HueBarWidth;
        Height = SaturationValueSize;
        WidthUnits = DimensionUnitType.Absolute;
        HeightUnits = DimensionUnitType.Absolute;

        SaturationValueContainer = new ContainerRuntime();
        SaturationValueContainer.Name = "SaturationValueContainer";
        SaturationValueContainer.Width = SaturationValueSize;
        SaturationValueContainer.Height = SaturationValueSize;
        SaturationValueContainer.HasEvents = true;
        SaturationValueContainer.ClipsChildren = true;
        this.AddChild(SaturationValueContainer);

        _saturationValueDisplay = new SpriteRuntime();
        _saturationValueDisplay.Name = "SaturationValueDisplay";
        _saturationValueDisplay.Width = 0;
        _saturationValueDisplay.WidthUnits = DimensionUnitType.RelativeToParent;
        _saturationValueDisplay.Height = 0;
        _saturationValueDisplay.HeightUnits = DimensionUnitType.RelativeToParent;
        _saturationValueDisplay.TextureAddress = global::Gum.Managers.TextureAddress.EntireTexture;
        SaturationValueContainer.AddChild(_saturationValueDisplay);

        AddOutline(SaturationValueContainer);
        AddSaturationValueIndicator(SaturationValueContainer);

        HueContainer = new ContainerRuntime();
        HueContainer.Name = "HueContainer";
        HueContainer.X = SaturationValueSize + Spacing;
        HueContainer.Width = HueBarWidth;
        HueContainer.Height = HueBarHeight;
        HueContainer.HasEvents = true;
        HueContainer.ClipsChildren = true;
        this.AddChild(HueContainer);

        _hueDisplay = new SpriteRuntime();
        _hueDisplay.Name = "HueDisplay";
        _hueDisplay.Width = 0;
        _hueDisplay.WidthUnits = DimensionUnitType.RelativeToParent;
        _hueDisplay.Height = 0;
        _hueDisplay.HeightUnits = DimensionUnitType.RelativeToParent;
        _hueDisplay.TextureAddress = global::Gum.Managers.TextureAddress.EntireTexture;
        HueContainer.AddChild(_hueDisplay);

        AddOutline(HueContainer);
        AddHueIndicator(HueContainer);

        GenerateHueTexture();
        RefreshSaturationValueBackground(0);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ColorPicker(this);
        }
    }

    private static void AddOutline(ContainerRuntime parent)
    {
        RectangleRuntime outline = new RectangleRuntime();
        outline.IsFilled = false;
        outline.StrokeColor = new Color((byte)80, (byte)80, (byte)80);
        outline.Width = 0;
        outline.WidthUnits = DimensionUnitType.RelativeToParent;
        outline.Height = 0;
        outline.HeightUnits = DimensionUnitType.RelativeToParent;
        parent.AddChild(outline);
    }

    private static void AddSaturationValueIndicator(ContainerRuntime parent)
    {
        ContainerRuntime indicator = new ContainerRuntime();
        indicator.Name = "SaturationValueIndicator";
        indicator.Width = 11;
        indicator.Height = 11;
        indicator.XOrigin = HorizontalAlignment.Center;
        indicator.YOrigin = VerticalAlignment.Center;
        parent.AddChild(indicator);

        AddContrastingOutlinePair(indicator);
    }

    private static void AddHueIndicator(ContainerRuntime parent)
    {
        ContainerRuntime indicator = new ContainerRuntime();
        indicator.Name = "HueIndicator";
        indicator.Width = HueBarWidth;
        indicator.Height = 5;
        indicator.YOrigin = VerticalAlignment.Center;
        parent.AddChild(indicator);

        AddContrastingOutlinePair(indicator);
    }

    // A black outline with a white outline inset one pixel, so the indicator reads against both
    // light and dark backgrounds.
    private static void AddContrastingOutlinePair(ContainerRuntime parent)
    {
        RectangleRuntime outer = new RectangleRuntime();
        outer.IsFilled = false;
        outer.StrokeColor = new Color((byte)0, (byte)0, (byte)0);
        outer.Width = 0;
        outer.WidthUnits = DimensionUnitType.RelativeToParent;
        outer.Height = 0;
        outer.HeightUnits = DimensionUnitType.RelativeToParent;
        parent.AddChild(outer);

        RectangleRuntime inner = new RectangleRuntime();
        inner.IsFilled = false;
        inner.StrokeColor = new Color((byte)255, (byte)255, (byte)255);
        inner.X = 1;
        inner.Y = 1;
        inner.Width = -2;
        inner.WidthUnits = DimensionUnitType.RelativeToParent;
        inner.Height = -2;
        inner.HeightUnits = DimensionUnitType.RelativeToParent;
        parent.AddChild(inner);
    }

    /// <inheritdoc/>
    public void RefreshSaturationValueBackground(float hue)
    {
        GraphicsDevice? graphicsDevice = SystemManagers.Default?.Renderer?.GraphicsDevice;
        if (graphicsDevice == null)
        {
            return;
        }

        int width = SaturationValueSize;
        int height = SaturationValueSize;
        _saturationValuePixels ??= new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float value = (1f - (float)y / (height - 1)) * 100f;
            for (int x = 0; x < width; x++)
            {
                float saturation = (float)x / (width - 1) * 100f;
                (byte r, byte g, byte b) = ColorPicker.HsvToRgb(hue, saturation, value);
                _saturationValuePixels[y * width + x] = new Color(r, g, b);
            }
        }

        _saturationValueTexture ??= new Texture2D(graphicsDevice, width, height);
        _saturationValueTexture.SetData(_saturationValuePixels);
        _saturationValueDisplay.Texture = _saturationValueTexture;
    }

    private void GenerateHueTexture()
    {
        GraphicsDevice? graphicsDevice = SystemManagers.Default?.Renderer?.GraphicsDevice;
        if (graphicsDevice == null)
        {
            return;
        }

        int width = HueBarWidth;
        int height = HueBarHeight;
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float hue = (float)y / (height - 1) * 360f;
            (byte r, byte g, byte b) = ColorPicker.HsvToRgb(hue, 100f, 100f);
            Color color = new Color(r, g, b);
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = color;
            }
        }

        _hueTexture = new Texture2D(graphicsDevice, width, height);
        _hueTexture.SetData(pixels);
        _hueDisplay.Texture = _hueTexture;
    }
}
