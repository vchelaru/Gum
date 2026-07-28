using System;
using Gum.Wireframe;
// System.Drawing.Color is a plain BCL struct (no GDI). The control lives in the runtime-agnostic
// GumCommon layer alongside the other Forms controls, so it exposes a backend-neutral color rather
// than any one runtime's native color type.
using Color = System.Drawing.Color;

namespace Gum.Forms.Controls;

/// <summary>
/// A control for visually selecting a color through a saturation/value square and a hue bar.
/// The selected color is available through <see cref="SelectedColor"/>, and changes raise
/// <see cref="SelectedColorChanged"/>.
/// </summary>
public class ColorPicker : FrameworkElement
{
    #region Fields/Properties

    // Internal texture resolution for the procedurally generated backgrounds. The display sprites
    // stretch it to their on-screen size, so it's independent of layout.
    private const int SaturationValueTextureSize = 256;
    private const int HueTextureHeight = 256;
    private const string HueBarCacheKey = "GumColorPickerHueBar";

    // The sprite children whose textures are generated at runtime. Found by name so any visual --
    // code-only or tool-authored -- works, as long as it contains sprites with these names.
    private GraphicalUiElement? _saturationValueDisplay;
    private GraphicalUiElement? _hueDisplay;

    // The interactive areas the user drags on, plus the indicators moved to reflect the selection.
    private InteractiveGue? _saturationValueContainer;
    private InteractiveGue? _hueContainer;
    private GraphicalUiElement? _saturationValueIndicator;
    private GraphicalUiElement? _hueIndicator;

    // Guards against re-entrant sync when a property setter derives and stores the other
    // representation of the same color.
    private bool _isUpdatingColor;

    private byte _red;
    private byte _green;
    private byte _blue;
    private float _hue;
    private float _saturation;
    private float _value;

    /// <summary>
    /// The currently selected color. Setting this value updates the hue/saturation/value state,
    /// repositions the indicators, and raises <see cref="SelectedColorChanged"/>.
    /// </summary>
    public Color SelectedColor
    {
        get => Color.FromArgb(_red, _green, _blue);
        set
        {
            if (value.R != _red || value.G != _green || value.B != _blue)
            {
                _red = value.R;
                _green = value.G;
                _blue = value.B;

                (_hue, _saturation, _value) = RgbToHsv(_red, _green, _blue);

                RefreshVisuals();
                RaiseSelectedColorChanged();
            }
        }
    }

    /// <summary>
    /// The hue of the selected color in degrees (0-360).
    /// </summary>
    public float Hue
    {
        get => _hue;
        set => SetHsv(value, _saturation, _value);
    }

    /// <summary>
    /// The saturation of the selected color (0-100).
    /// </summary>
    public float Saturation
    {
        get => _saturation;
        set => SetHsv(_hue, value, _value);
    }

    /// <summary>
    /// The value (brightness) of the selected color (0-100).
    /// </summary>
    public float Value
    {
        get => _value;
        set => SetHsv(_hue, _saturation, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised whenever <see cref="SelectedColor"/> changes, whether through user interaction or
    /// by setting a property in code.
    /// </summary>
    public event EventHandler? SelectedColorChanged;

    #endregion

    #region Initialize

    /// <summary>
    /// Creates a new ColorPicker using the default registered visual.
    /// </summary>
    public ColorPicker() : base()
    {
    }

    /// <summary>
    /// Creates a new ColorPicker using the provided visual.
    /// </summary>
    public ColorPicker(InteractiveGue visual) : base(visual)
    {
    }

    /// <inheritdoc/>
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();

        _saturationValueContainer = Visual.GetGraphicalUiElementByName("SaturationValueContainer") as InteractiveGue;
        _hueContainer = Visual.GetGraphicalUiElementByName("HueContainer") as InteractiveGue;
        _saturationValueDisplay = Visual.GetGraphicalUiElementByName("SaturationValueDisplay");
        _hueDisplay = Visual.GetGraphicalUiElementByName("HueDisplay");
        _saturationValueIndicator = Visual.GetGraphicalUiElementByName("SaturationValueIndicator");
        _hueIndicator = Visual.GetGraphicalUiElementByName("HueIndicator");

        PaintHueBar();

        // Push handles the initial click; Dragging continues to fire on the pushed element every
        // frame the cursor moves, even once it leaves the element's bounds, so a drag keeps tracking
        // the cursor outside the picker (matching Slider/ScrollBar).
        if (_saturationValueContainer != null)
        {
            _saturationValueContainer.Push += HandleSaturationValueInput;
            _saturationValueContainer.Dragging += HandleSaturationValueInput;
        }

        if (_hueContainer != null)
        {
            _hueContainer.Push += HandleHueInput;
            _hueContainer.Dragging += HandleHueInput;
        }

        RefreshVisuals();
    }

    #endregion

    #region Event Handlers

    private void HandleSaturationValueInput(object? sender, EventArgs e)
    {
        PickSaturationValueFromCursor();
    }

    private void HandleHueInput(object? sender, EventArgs e)
    {
        PickHueFromCursor();
    }

    #endregion

    #region Cursor Picking

    private void PickSaturationValueFromCursor()
    {
        if (_saturationValueContainer == null)
        {
            return;
        }

        float width = _saturationValueContainer.AbsoluteWidth;
        float height = _saturationValueContainer.AbsoluteHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        float relativeX = MainCursor.XRespectingGumZoomAndBounds() - _saturationValueContainer.AbsoluteLeft;
        float relativeY = MainCursor.YRespectingGumZoomAndBounds() - _saturationValueContainer.AbsoluteTop;

        relativeX = Math.Clamp(relativeX, 0, width);
        relativeY = Math.Clamp(relativeY, 0, height);

        float saturation = relativeX / width * 100f;
        float value = (1f - relativeY / height) * 100f;

        SetHsv(_hue, saturation, value);
    }

    private void PickHueFromCursor()
    {
        if (_hueContainer == null)
        {
            return;
        }

        float height = _hueContainer.AbsoluteHeight;
        if (height <= 0)
        {
            return;
        }

        float relativeY = MainCursor.YRespectingGumZoomAndBounds() - _hueContainer.AbsoluteTop;
        relativeY = Math.Clamp(relativeY, 0, height);

        float hue = relativeY / height * 360f;

        SetHsv(hue, _saturation, _value);
    }

    #endregion

    #region Color State

    private void SetHsv(float hue, float saturation, float value)
    {
        hue = Math.Clamp(hue, 0f, 360f);
        saturation = Math.Clamp(saturation, 0f, 100f);
        value = Math.Clamp(value, 0f, 100f);

        (byte r, byte g, byte b) = HsvToRgb(hue, saturation, value);

        bool colorChanged = r != _red || g != _green || b != _blue;

        _hue = hue;
        _saturation = saturation;
        _value = value;
        _red = r;
        _green = g;
        _blue = b;

        RefreshVisuals();

        if (colorChanged)
        {
            RaiseSelectedColorChanged();
        }
    }

    private void RaiseSelectedColorChanged()
    {
        if (!_isUpdatingColor)
        {
            SelectedColorChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void RefreshVisuals()
    {
        _isUpdatingColor = true;
        try
        {
            PaintSaturationValue();

            if (_saturationValueContainer != null && _saturationValueIndicator != null)
            {
                float width = _saturationValueContainer.AbsoluteWidth;
                float height = _saturationValueContainer.AbsoluteHeight;
                _saturationValueIndicator.X = _saturation / 100f * width;
                _saturationValueIndicator.Y = (1f - _value / 100f) * height;
            }

            if (_hueContainer != null && _hueIndicator != null)
            {
                float height = _hueContainer.AbsoluteHeight;
                _hueIndicator.Y = _hue / 360f * height;
            }
        }
        finally
        {
            _isUpdatingColor = false;
        }
    }

    #endregion

    #region Texture Generation

    private void PaintHueBar()
    {
        if (_hueDisplay == null)
        {
            return;
        }

        // The hue bar is identical for every picker, so it's generated once and shared via the cache.
        GraphicalUiElement.ApplyCachedTextureFromPixelData?.Invoke(
            _hueDisplay, HueBarCacheKey, BuildHueBarPixels(), 1, HueTextureHeight);
    }

    private void PaintSaturationValue()
    {
        if (_saturationValueDisplay == null)
        {
            return;
        }

        // The saturation/value square depends on the current hue, so each picker gets its own pooled
        // texture (keyed by this control's Visual) rather than a shared one.
        GraphicalUiElement.ApplyPooledTextureFromPixelData?.Invoke(
            _saturationValueDisplay, Visual, BuildSaturationValuePixels(_hue),
            SaturationValueTextureSize, SaturationValueTextureSize);
    }

    /// <summary>
    /// Builds the saturation/value square as RGBA bytes for a hue: X is saturation (0-100), Y is value
    /// (100 at top to 0 at bottom).
    /// </summary>
    internal static byte[] BuildSaturationValuePixels(float hue)
    {
        int size = SaturationValueTextureSize;
        byte[] rgba = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
        {
            float value = (1f - (float)y / (size - 1)) * 100f;
            for (int x = 0; x < size; x++)
            {
                float saturation = (float)x / (size - 1) * 100f;
                (byte r, byte g, byte b) = HsvToRgb(hue, saturation, value);
                int i = (y * size + x) * 4;
                rgba[i] = r;
                rgba[i + 1] = g;
                rgba[i + 2] = b;
                rgba[i + 3] = 255;
            }
        }
        return rgba;
    }

    /// <summary>
    /// Builds the hue bar as a 1-pixel-wide RGBA column: hue runs 0-360 from top to bottom at full
    /// saturation and value.
    /// </summary>
    internal static byte[] BuildHueBarPixels()
    {
        int height = HueTextureHeight;
        byte[] rgba = new byte[height * 4];
        for (int y = 0; y < height; y++)
        {
            float hue = (float)y / (height - 1) * 360f;
            (byte r, byte g, byte b) = HsvToRgb(hue, 100f, 100f);
            int i = y * 4;
            rgba[i] = r;
            rgba[i + 1] = g;
            rgba[i + 2] = b;
            rgba[i + 3] = 255;
        }
        return rgba;
    }

    #endregion

    #region Color Conversion

    /// <summary>
    /// Converts RGB (0-255) to HSV, returning hue (0-360), saturation (0-100) and value (0-100).
    /// </summary>
    internal static (float Hue, float Saturation, float Value) RgbToHsv(byte r, byte g, byte b)
    {
        float rf = r / 255f;
        float gf = g / 255f;
        float bf = b / 255f;
        float max = MathF.Max(rf, MathF.Max(gf, bf));
        float min = MathF.Min(rf, MathF.Min(gf, bf));
        float delta = max - min;

        float hue;
        if (delta == 0)
        {
            hue = 0;
        }
        else if (max == rf)
        {
            hue = 60f * (((gf - bf) / delta) % 6f);
        }
        else if (max == gf)
        {
            hue = 60f * ((bf - rf) / delta + 2f);
        }
        else
        {
            hue = 60f * ((rf - gf) / delta + 4f);
        }

        if (hue < 0)
        {
            hue += 360f;
        }

        float saturation = max == 0 ? 0 : delta / max;

        return (hue, saturation * 100f, max * 100f);
    }

    /// <summary>
    /// Converts HSV (hue 0-360, saturation 0-100, value 0-100) to RGB bytes (0-255).
    /// </summary>
    internal static (byte R, byte G, byte B) HsvToRgb(float hue, float saturation, float value)
    {
        float s = saturation / 100f;
        float v = value / 100f;

        float c = v * s;
        float x = c * (1f - MathF.Abs(hue / 60f % 2f - 1f));
        float m = v - c;

        float rf;
        float gf;
        float bf;
        if (hue < 60)
        {
            rf = c;
            gf = x;
            bf = 0;
        }
        else if (hue < 120)
        {
            rf = x;
            gf = c;
            bf = 0;
        }
        else if (hue < 180)
        {
            rf = 0;
            gf = c;
            bf = x;
        }
        else if (hue < 240)
        {
            rf = 0;
            gf = x;
            bf = c;
        }
        else if (hue < 300)
        {
            rf = x;
            gf = 0;
            bf = c;
        }
        else
        {
            rf = c;
            gf = 0;
            bf = x;
        }

        return (
            (byte)MathF.Round((rf + m) * 255f),
            (byte)MathF.Round((gf + m) * 255f),
            (byte)MathF.Round((bf + m) * 255f));
    }

    #endregion
}
