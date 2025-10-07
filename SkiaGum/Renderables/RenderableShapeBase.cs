using Gum.Converters;
using Gum.DataTypes;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System.Collections.ObjectModel;
using ToolsUtilitiesStandard.Helpers;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;
using Gum;
using System;

namespace SkiaGum.Renderables;

public class RenderableShapeBase : IRenderableIpso, IVisible, IDisposable
{
    #region Fields/Properties

    SKColor _color = SKColors.Red;
    public SKColor Color
    {
        get => _color;
        set
        {
            _color = value;
            ClearCachedPaint();
        }
    }

    public int Alpha
    {
        get => Color.Alpha;
        set
        {
            this.Color = new SKColor(this.Color.Red, this.Color.Green, this.Color.Blue, (byte)value);
        }
    }

    public int Blue
    {
        get => Color.Blue;
        set
        {
            this.Color = new SKColor(this.Color.Red, this.Color.Green, (byte)value, this.Color.Alpha);
        }
    }

    public int Green
    {
        get => Color.Green;
        set
        {
            this.Color = new SKColor(this.Color.Red, (byte)value, this.Color.Blue, this.Color.Alpha);
        }
    }

    public int Red
    {
        get => Color.Red;
        set
        {
            this.Color = new SKColor((byte)value, this.Color.Green, this.Color.Blue, this.Color.Alpha);
        }
    }

    Vector2 Position;
    internal protected IRenderableIpso? mParent;

    public IRenderableIpso? Parent
    {
        get { return mParent; }
        set
        {
            if (mParent != value)
            {
                if (mParent != null)
                {
                    mParent.Children.Remove(this);
                }
                mParent = value;
                if (mParent != null)
                {
                    mParent.Children.Add(this);
                }
            }
        }
    }

    internal protected ObservableCollection<IRenderableIpso> mChildren;
    public ObservableCollection<IRenderableIpso> Children
    {
        get { return mChildren; }
    }

    public float X
    {
        get => Position.X;
        set => Position.X = value;
    }

    public float Y
    {
        get { return Position.Y; }
        set { Position.Y = value; }
    }

    public float Z
    {
        get;
        set;
    }

    float _width;
    public float Width
    {
        get => _width;
        set
        {
            var changed = _width != value;
            _width = value;
            if(changed && UseGradient)
            {
                ClearCachedPaint();
            }
        }
    }

    float _height;
    public float Height
    {
        get => _height;
        set
        {
            var changed = _height != value;
            _height = value;
            if(changed && UseGradient)
            {
                ClearCachedPaint();
            }
        }
    }
    public string Name
    {
        get;
        set;
    }
    public float Rotation { get; set; }

    public bool Wrap => false;

    protected bool CanRenderAt0Dimension { get; set; } = false;
    protected bool IsOffsetAppliedForStroke { get; set; } = true;

    public ColorOperation ColorOperation { get; set; } = ColorOperation.Modulate;


    private bool _useGradient;
    public bool UseGradient
    {
        get => _useGradient;
        set
        {
            _useGradient = value;
            ClearCachedPaint();
        }
    }

    private GradientType _gradientType;
    public GradientType GradientType
    {
        get => _gradientType; 
        set
        {
            _gradientType = value;
            ClearCachedPaint();
        }
    }

    private int _alpha1;
    public int Alpha1
    {
        get => _alpha1; 
        set
        {
            _alpha1 = value;
            ClearCachedPaint();
        }
    }
    private int _red1;
    public int Red1
    {
        get => _red1; set
        {
            _red1 = value;
            ClearCachedPaint();
        }
    }
    private int _green1;
    public int Green1
    {
        get => _green1;
        set
        {
            _green1 = value;
            ClearCachedPaint();
        }
    }
    private int _blue1;
    public int Blue1
    {
        get => _blue1;
        set
        {
            _blue1 = value;
            ClearCachedPaint();
        }
    }

    private int _alpha2;
    public int Alpha2
    {
        get => _alpha2;
        set
        {
            _alpha2 = value;
            ClearCachedPaint();
        }
    }
    private int _red2;
    public int Red2
    {
        get => _red2;
        set
        {
            _red2 = value;
            ClearCachedPaint();
        }
    }
    private int _green2;
    public int Green2
    {
        get => _green2;
        set
        {
            _green2 = value;
            ClearCachedPaint();
        }
    }
    private int _blue2;
    public int Blue2
    {
        get => _blue2;
        set
        {
            _blue2 = value;
            ClearCachedPaint();
        }
    }

    private float _gradientX1;
    public float GradientX1
    {
        get => _gradientX1;
        set
        {
            _gradientX1 = value;
            ClearCachedPaint();
        }
    }
    private GeneralUnitType _gradientX1Units;
    public GeneralUnitType GradientX1Units
    {
        get => _gradientX1Units;
        set
        {
            _gradientX1Units = value;
            ClearCachedPaint();
        }
    }
    private float _gradientY1;
    public float GradientY1
    {
        get => _gradientY1;
        set
        {
            _gradientY1 = value;
            ClearCachedPaint();
        }
    }
    private GeneralUnitType _gradientY1Units;
    public GeneralUnitType GradientY1Units
    {
        get => _gradientY1Units;
        set
        {
            _gradientY1Units = value;
            ClearCachedPaint();
        }
    }

    private float _gradientX2;
    public float GradientX2
    {
        get => _gradientX2;
        set
        {
            _gradientX2 = value;
            ClearCachedPaint();
        }
    }

    private GeneralUnitType _gradientX2Units;
    public GeneralUnitType GradientX2Units
    {
        get => _gradientX2Units;
        set
        {
            _gradientX2Units = value;
            ClearCachedPaint();
        }
    }

    private float _gradientY2;
    public float GradientY2
    {
        get => _gradientY2;
        set
        {
            _gradientY2 = value;
            ClearCachedPaint();
        }
    }
    private GeneralUnitType _gradientY2Units;
    public GeneralUnitType GradientY2Units
    {
        get => _gradientY2Units;
        set
        {
            _gradientY2Units = value;
            ClearCachedPaint();
        }
    }


    private float _gradientInnerRadius;
    public float GradientInnerRadius
    {
        get => _gradientInnerRadius;
        set
        {
            _gradientInnerRadius = value;
            ClearCachedPaint();
        }
    }
    private DimensionUnitType _gradientInnerRadiusUnits;
    public DimensionUnitType GradientInnerRadiusUnits
    {
        get => _gradientInnerRadiusUnits;
        set
        {
            _gradientInnerRadiusUnits = value;
            ClearCachedPaint();
        }
    }

    private float _gradientOuterRadius;
    public float GradientOuterRadius
    {
        get => _gradientOuterRadius;
        set
        {
            _gradientOuterRadius = value;
            ClearCachedPaint();
        }
    }
    private DimensionUnitType _gradientOuterRadiusUnits;
    public DimensionUnitType GradientOuterRadiusUnits
    {
        get => _gradientOuterRadiusUnits;
        set
        {
            _gradientOuterRadiusUnits = value;
            ClearCachedPaint();
        }
    }

    bool _isDimmed;
    public bool IsDimmed
    {
        get => _isDimmed;
        set
        {
            _isDimmed = value;
            ClearCachedPaint();
        }
    }


    bool _isFilled = true;
    public bool IsFilled
    {
        get => _isFilled;
        set
        {
            _isFilled = value;
            ClearCachedPaint();
        }
    }

    float _strokeWidth = 2;
    public float StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            _strokeWidth = value;
            ClearCachedPaint();
        }
    }

    #region Dropshadow

    SKColor _dropshadowColor;

    public SKColor DropshadowColor
    {
        get => _dropshadowColor;
        set
        {
            _dropshadowColor = value;
            ClearCachedPaint();
        }
    }

    public int DropshadowAlpha
    {
        get => DropshadowColor.Alpha;
        set
        {
            this.DropshadowColor = new SKColor(this.DropshadowColor.Red, this.DropshadowColor.Green, this.DropshadowColor.Blue, (byte)value);
        }
    }

    public int DropshadowBlue
    {
        get => DropshadowColor.Blue;
        set
        {
            this.DropshadowColor = new SKColor(this.DropshadowColor.Red, this.DropshadowColor.Green, (byte)value, this.DropshadowColor.Alpha);
        }
    }

    public int DropshadowGreen
    {
        get => DropshadowColor.Green;
        set
        {
            this.DropshadowColor = new SKColor(this.DropshadowColor.Red, (byte)value, this.DropshadowColor.Blue, this.DropshadowColor.Alpha);
        }
    }

    public int DropshadowRed
    {
        get => DropshadowColor.Red;
        set
        {
            this.DropshadowColor = new SKColor((byte)value, this.DropshadowColor.Green, this.DropshadowColor.Blue, this.DropshadowColor.Alpha);
        }
    }

    private bool _hasDropshadow;

    public bool HasDropshadow
    {
        get => _hasDropshadow;
        set
        {
            _hasDropshadow = value;
            ClearCachedPaint();
        }
    }

    private float _dropshadowOffsetX;

    public float DropshadowOffsetX
    {
        get => _dropshadowOffsetX;
        set
        {
            _dropshadowOffsetX = value;
            ClearCachedPaint();
        }
    }

    private float _dropshadowOffsetY;

    public float DropshadowOffsetY
    {
        get => _dropshadowOffsetY;
        set
        {
            _dropshadowOffsetY = value;
            ClearCachedPaint();
        }
    }

    private float _dropshadowBlurX;

    public float DropshadowBlurX
    {
        get => _dropshadowBlurX;
        set
        {
            _dropshadowBlurX = value;
            ClearCachedPaint();
        }
    }

    private float _dropshadowBlurY;

    public float DropshadowBlurY
    {
        get => _dropshadowBlurY;
        set
        {
            _dropshadowBlurY = value;
            ClearCachedPaint();
        }
    }

    #endregion


    public bool FlipHorizontal
    {
        get;
        set;
    }

    public bool FlipVertical
    {
        get;
        set;
    }

    public object Tag { get; set; }

    #endregion

    public RenderableShapeBase()
    {
        Width = 32;
        Height = 32; 
        Alpha = 255;
        Alpha1 = 255;
        Alpha2 = 255;
        this.Visible = true;
        mChildren = new ObservableCollection<IRenderableIpso>();

    }

    public bool IsRenderTarget => false;


    public void PreRender() {}

    public void Render(ISystemManagers managers)
    {
        var canvas = ((SystemManagers)managers).Canvas;

        var canRender =
            AbsoluteVisible &&
                ((Width > 0 && Height > 0) || CanRenderAt0Dimension);
        if (canRender)
        {
            var absoluteX = this.GetAbsoluteX();
            var absoluteY = this.GetAbsoluteY();
            var boundingRect = new SKRect(absoluteX, absoluteY, absoluteX + this.Width, absoluteY + this.Height);

            var rotation = this.GetAbsoluteRotation();
            var applyRotation = rotation != 0;
            if (applyRotation)
            {
                var oldX = boundingRect.Left;
                var oldY = boundingRect.Top;

                canvas.Save();

                boundingRect.Left = 0;
                boundingRect.Right -= oldX;
                boundingRect.Top = 0;
                boundingRect.Bottom -= oldY;

                canvas.Translate(oldX, oldY);
                canvas.RotateDegrees(-rotation);
            }

            // If this is stroke-only, then the stroke is centered around the bounds 
            // we pass in. Therefore, we need to move the bounds "in" by half of the 
            // stroke width
            if (IsFilled == false && IsOffsetAppliedForStroke)
            {
                boundingRect.Left += StrokeWidth / 2.0f;
                boundingRect.Top += StrokeWidth / 2.0f;
                boundingRect.Right -= StrokeWidth / 2.0f;
                boundingRect.Bottom -= StrokeWidth / 2.0f;
            }

            DrawBound(boundingRect, canvas, this.GetAbsoluteRotation());

            if (applyRotation)
            {
                canvas.Restore();
            }
        }
    }

    public BlendState BlendState => BlendState.AlphaBlend;

    public bool ClipsChildren { get; set; }

    SKRect _lastBoundingRect;

    SKPaint? _cachedPaint;
    protected void ClearCachedPaint()
    {
        _cachedPaint?.Dispose();
        _cachedPaint = null;

    }



    float _lastAbsoluteRotation;
    protected SKPaint GetCachedPaint(SKRect boundingRect, float absoluteRotation)
    {
        if(boundingRect != _lastBoundingRect || absoluteRotation != _lastAbsoluteRotation)
        {
            _lastBoundingRect = boundingRect;
            _lastAbsoluteRotation = absoluteRotation;
            ClearCachedPaint();
        }
        if(_cachedPaint == null)
        {
            _cachedPaint = GetPaint(boundingRect, absoluteRotation);
        }
        return _cachedPaint;
    }

    protected virtual SKPaint GetPaint(SKRect boundingRect, float absoluteRotation)
    {
        var effectiveColor = this.Color;
        if (IsDimmed)
        {
            const double dimmingMuliplier = .9;

            effectiveColor = new SKColor(
                (byte)(this.Color.Red * dimmingMuliplier),
                (byte)(this.Color.Green * dimmingMuliplier),
                (byte)(this.Color.Blue * dimmingMuliplier),
                this.Color.Alpha);
        }



        var paint = new SKPaint
        {
            Color = effectiveColor,
            Style = IsFilled ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
            StrokeWidth = StrokeWidth,
            IsAntialias = true
        };

        if (HasDropshadow)
        {
            paint.ImageFilter = SKImageFilter.CreateDropShadow(
                        DropshadowOffsetX,
                        // See https://stackoverflow.com/questions/60456526/how-can-i-tell-the-amount-of-space-needed-for-a-skia-dropshadow
                        DropshadowOffsetY,
                        DropshadowBlurX / 3.0f,
                        DropshadowBlurY / 3.0f,
                        DropshadowColor);
        }

        if (UseGradient)
        {
            ApplyGradientToPaint(boundingRect, paint, absoluteRotation);
        }

        return paint;
    }

    public virtual void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {

    }

    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
    {
        mParent = parent;
    }

    #region IVisible Implementation

    public bool Visible
    {
        get;
        set;
    }

    public bool AbsoluteVisible
    {
        get
        {
            if (((IVisible)this).Parent == null)
            {
                return Visible;
            }
            else
            {
                return Visible && ((IVisible)this).Parent.AbsoluteVisible;
            }
        }
    }

    IVisible IVisible.Parent
    {
        get
        {
            return ((IRenderableIpso)this).Parent as IVisible;
        }
    }

    public string BatchKey => string.Empty;

    #endregion

    protected void ApplyGradientToPaint(SKRect boundingRect, SKPaint paint, float absoluteRotation)
    {
        var firstColor = new SKColor((byte)Red1, (byte)Green1, (byte)Blue1, (byte)Alpha1);
        var secondColor = new SKColor((byte)Red2, (byte)Green2, (byte)Blue2, (byte)Alpha2);

        var effectiveGradientX1 = GradientX1;
        switch (this.GradientX1Units)
        {
            case GeneralUnitType.PixelsFromMiddle:
                effectiveGradientX1 += Width / 2.0f;
                break;
            case GeneralUnitType.PixelsFromLarge:
                effectiveGradientX1 += Width;
                break;
            case GeneralUnitType.Percentage:
                effectiveGradientX1 = Width * GradientX1 / 100;
                break;
        }

        var effectiveGradientX2 = GradientX2;
        switch (this.GradientX2Units)
        {
            case GeneralUnitType.PixelsFromMiddle:
                effectiveGradientX2 += Width / 2.0f;
                break;
            case GeneralUnitType.PixelsFromLarge:
                effectiveGradientX2 += Width;
                break;
            case GeneralUnitType.Percentage:
                effectiveGradientX2 = Width * GradientX2 / 100;
                break;
        }

        var effectiveGradientY1 = GradientY1;
        switch (this.GradientY1Units)
        {
            case GeneralUnitType.PixelsFromMiddle:
                effectiveGradientY1 += Height / 2.0f;
                break;
            case GeneralUnitType.PixelsFromLarge:
                effectiveGradientY1 += Height;
                break;
            case GeneralUnitType.Percentage:
                effectiveGradientY1 = Height * GradientY1 / 100;
                break;
        }

        var effectiveGradientY2 = GradientY2;
        switch (this.GradientY2Units)
        {
            case GeneralUnitType.PixelsFromMiddle:
                effectiveGradientY2 += Height / 2.0f;
                break;
            case GeneralUnitType.PixelsFromLarge:
                effectiveGradientY2 += Height;
                break;
            case GeneralUnitType.Percentage:
                effectiveGradientY2 = Height * GradientY2 / 100;
                break;
        }

        var rectToUse = boundingRect;
        if (absoluteRotation != 0)
        {
            rectToUse = Unrotate(boundingRect, absoluteRotation);
        }
        else
        {
            // If we apply rotation, then the camera coordinates are adjusted such that the gradient coordiantes are relative to the object.
            // Otherwise, they are not so we need to offset:
            effectiveGradientX1 += rectToUse.Left;
            effectiveGradientY1 += rectToUse.Top;
            effectiveGradientX2 += rectToUse.Left;
            effectiveGradientY2 += rectToUse.Top;
        }

        if (GradientType == GradientType.Linear)
        {

            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(effectiveGradientX1, effectiveGradientY1), // left, top
                new SKPoint(effectiveGradientX2, effectiveGradientY2), // right, bottom
                new SKColor[] { firstColor, secondColor },
                new float[] { 0, 1 },
                SKShaderTileMode.Clamp);
        }
        else if (GradientType == GradientType.Radial)
        {
            var effectiveOuterRadius = GradientOuterRadius;

            switch (GradientOuterRadiusUnits)
            {
                case Gum.DataTypes.DimensionUnitType.PercentageOfParent:
                    effectiveOuterRadius = Width * GradientOuterRadius / 100;
                    break;
                case Gum.DataTypes.DimensionUnitType.RelativeToParent:
                    effectiveOuterRadius = Width / 2 + GradientOuterRadius;
                    break;
            }

            if (effectiveOuterRadius <= 0)
            {
                effectiveOuterRadius = 100;
            }

            var effectiveInnerRadius = GradientInnerRadius;

            switch (GradientInnerRadiusUnits)
            {
                case Gum.DataTypes.DimensionUnitType.Percentage:
                    effectiveInnerRadius = Width * GradientInnerRadius / 100;
                    break;
                case Gum.DataTypes.DimensionUnitType.RelativeToContainer:
                    effectiveInnerRadius = Width / 2 + GradientInnerRadius;
                    break;
            }

            var innerToOuterRatio = effectiveInnerRadius / effectiveOuterRadius;


            paint.Shader = SKShader.CreateRadialGradient(
                new SKPoint(effectiveGradientX1, effectiveGradientY1), // center
                effectiveOuterRadius,
                new SKColor[] { firstColor, secondColor },
                new float[] { innerToOuterRatio, 1 },
                SKShaderTileMode.Clamp);
        }
    }

    SKRect Unrotate(SKRect beforeRotation, float angleToUndoDegrees)
    {
        var pointBefore = new Vector2(beforeRotation.Left, beforeRotation.Top);

        var middleOffset = new Vector2(beforeRotation.Width / 2.0f, beforeRotation.Height / 2.0f);
        MathFunctions.RotateVector(ref middleOffset, MathHelper.ToRadians(-angleToUndoDegrees));

        var middle = pointBefore + middleOffset;

        // convert to a "positive-Y-is-up" system:
        pointBefore.Y *= -1;
        middle.Y *= -1;
        MathFunctions.RotatePointAroundPoint(middle, ref pointBefore, MathHelper.ToRadians(-angleToUndoDegrees));
        pointBefore.Y *= -1;

        return new SKRect(pointBefore.X, pointBefore.Y, pointBefore.X + beforeRotation.Width, pointBefore.Y + beforeRotation.Height);
    }

    public void Dispose()
    {
        ClearCachedPaint();

        if(this.Children != null)
        {
            foreach(var child in this.Children)
            {
                if (child is IDisposable disposableChild)
                {
                    disposableChild.Dispose();
                }
            }
        }
    }

    public void StartBatch(ISystemManagers systemManagers)
    {
    }

    public void EndBatch(ISystemManagers systemManagers)
    {
    }
}
