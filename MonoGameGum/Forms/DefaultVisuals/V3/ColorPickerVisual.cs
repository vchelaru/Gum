#pragma warning disable CS0618, GUM001 // Default visuals intentionally use deprecated MonoGameGum.GueDeriving shim types for backward compatibility until V1/V2/V3 visuals are retired. See issue #2715.
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for a <see cref="ColorPicker"/> control. Builds the saturation/value square, the
/// hue bar, and the indicators the control moves to reflect the selected color. The square/bar
/// textures are generated at runtime by the control (via the sprites named "SaturationValueDisplay"
/// and "HueDisplay"), so this visual is purely structural and backend-agnostic.
/// </summary>
public class ColorPickerVisual : InteractiveGue
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

        AddDisplaySprite(SaturationValueContainer, "SaturationValueDisplay");
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

        AddDisplaySprite(HueContainer, "HueDisplay");
        AddOutline(HueContainer);
        AddHueIndicator(HueContainer);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ColorPicker(this);
        }
    }

    // The sprite that fills the container and whose texture the control generates at runtime.
    private static void AddDisplaySprite(ContainerRuntime parent, string name)
    {
        SpriteRuntime display = new SpriteRuntime();
        display.Name = name;
        display.Width = 0;
        display.WidthUnits = DimensionUnitType.RelativeToParent;
        display.Height = 0;
        display.HeightUnits = DimensionUnitType.RelativeToParent;
        display.TextureAddress = global::Gum.Managers.TextureAddress.EntireTexture;
        parent.AddChild(display);
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
}
