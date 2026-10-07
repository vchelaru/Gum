using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.RenderingLibrary;
using GumDataTypes.Variables;

using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.ComponentModel;
using ToolsUtilitiesStandard.Helpers;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using GumRuntime;
using Gum.Collections;

#if FRB

using InteractiveGue = Gum.Wireframe.GraphicalUiElement;
#endif


#if !FRB
using Gum.StateAnimation.Runtime;
#endif

namespace Gum.Wireframe;

public partial class GraphicalUiElement
{

    #region Renderable Measurement

    // The values the layout engine reads from the contained renderable.
    struct RenderableMeasurement
    {
        public float TextWidth;
        public float TextHeight;
        public float Descender;
        public float? TextureWidth;
        public float? TextureHeight;
        public float AspectRatio;
    }

    RenderableMeasurement MeasureRenderable()
    {
        RenderableMeasurement measurement = default;
        if (mContainedObjectAsIpso is IText text)
        {
            measurement.TextWidth = text.WrappedTextWidth;
            measurement.TextHeight = text.WrappedTextHeight;
            measurement.Descender = text.DescenderHeight;
        }
        if (mContainedObjectAsIpso is ITextureCoordinate textureCoordinate)
        {
            measurement.TextureWidth = textureCoordinate.TextureWidth;
            measurement.TextureHeight = textureCoordinate.TextureHeight;
        }
        if (mContainedObjectAsIpso is IAspectRatio aspectRatio)
        {
            measurement.AspectRatio = aspectRatio.AspectRatio;
        }
        return measurement;
    }

    /// <summary>
    /// Runs <paramref name="change"/>, which changes what the contained renderable reports as its
    /// size (its texture, source file or font), then calls <see cref="UpdateLayout()"/> if this
    /// element's units read a value that changed. Runtimes call this from such setters; it respects
    /// layout suspension like any other layout call.
    /// </summary>
    /// <param name="state">Passed to <paramref name="change"/>, so a static lambda can be used.</param>
    /// <param name="change">Applies the change to the renderable.</param>
    protected internal void ChangeRenderableAndUpdateLayout<TState>(TState state, Action<TState> change)
    {
        RenderableMeasurement before = MeasureRenderable();
        change(state);
        UpdateLayoutIfRenderableMeasurementChanged(before);
    }

    void UpdateLayoutIfRenderableMeasurementChanged(in RenderableMeasurement before)
    {
        if (DoesLayoutReadChangedMeasurement(before))
        {
            UpdateLayout();
        }
    }

    bool DoesLayoutReadChangedMeasurement(in RenderableMeasurement before)
    {
        if (mContainedObjectAsIpso is IText text)
        {
            bool isSizedFromText = mWidthUnit == DimensionUnitType.RelativeToChildren ||
                mHeightUnit == DimensionUnitType.RelativeToChildren;
            if (isSizedFromText &&
                (text.WrappedTextWidth != before.TextWidth || text.WrappedTextHeight != before.TextHeight))
            {
                return true;
            }
            if (text.DescenderHeight != before.Descender && IsPlacedFromTextBaseline())
            {
                return true;
            }
        }

        if (mContainedObjectAsIpso is ITextureCoordinate textureCoordinate &&
            (textureCoordinate.TextureWidth != before.TextureWidth ||
             textureCoordinate.TextureHeight != before.TextureHeight) &&
            IsSizedOrPlacedFromTexture())
        {
            return true;
        }

        if (mContainedObjectAsIpso is IAspectRatio aspectRatio &&
            !aspectRatio.AspectRatio.Equals(before.AspectRatio) &&
            (mWidthUnit == DimensionUnitType.MaintainFileAspectRatio || mHeightUnit == DimensionUnitType.MaintainFileAspectRatio))
        {
            return true;
        }

        return false;
    }

    // This element's own baseline origin, or a child placed on its baseline, reads the descender.
    bool IsPlacedFromTextBaseline()
    {
        if (mYOrigin == VerticalAlignment.TextBaseline)
        {
            return true;
        }
        IList<GraphicalUiElement> children = Children ?? (IList<GraphicalUiElement>)mWhatThisContains;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i].YUnits == GeneralUnitType.PixelsFromBaseline)
            {
                return true;
            }
        }
        return false;
    }

    bool IsSizedOrPlacedFromTexture() =>
        IsSizedFromTexture(mWidthUnit) || IsSizedFromTexture(mHeightUnit) ||
        mXUnits == GeneralUnitType.PercentageOfFile || mYUnits == GeneralUnitType.PercentageOfFile;

    static bool IsSizedFromTexture(DimensionUnitType units) =>
        units == DimensionUnitType.PercentageOfSourceFile || units == DimensionUnitType.MaintainFileAspectRatio;

    #endregion

    #region Cursor/Position Hit Testing

    protected virtual bool IsOutsideOfBoundsHitTestingEnabled => this.Tag is ScreenSave;

    public virtual bool IsPointInside(float x, float y) => ((IRenderableIpso)this).HasCursorOver(x, y);

    #endregion
}
