using Apos.Shapes;
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;

namespace MonoGameAndGum.Renderables;

public abstract class RenderableShapeBase : RenderableBase
{
    protected ShapeRenderer ShapeRenderer => ShapeRenderer.Self;


    // this is the default in Skia renderables so use that here:
    public Color Color { get; set; } = Color.Red;
    public int Alpha
    {
        get => Color.A;
        set
        {
            this.Color = new Color(this.Color.R, this.Color.G, this.Color.B, (byte)value);
        }
    }

    public int Blue
    {
        get => Color.B;
        set
        {
            this.Color = new Color(this.Color.R, this.Color.G, (byte)value, this.Color.A);
        }
    }

    public int Green
    {
        get => Color.G;
        set
        {
            this.Color = new Color(this.Color.R, (byte)value, this.Color.B, this.Color.A);
        }
    }

    public int Red
    {
        get => Color.R;
        set
        {
            this.Color = new Color((byte)value, this.Color.G, this.Color.B, this.Color.A);
        }
    }

    #region Gradient

    private bool _useGradient;
    public bool UseGradient
    {
        get => _useGradient;
        set
        {
            _useGradient = value;
        }
    }

    private GradientType _gradientType;
    public GradientType GradientType
    {
        get => _gradientType;
        set
        {
            _gradientType = value;
        }
    }

    private int _alpha1 = 255;
    public int Alpha1
    {
        get => _alpha1;
        set
        {
            _alpha1 = value;
        }
    }
    private int _red1;
    public int Red1
    {
        get => _red1; set
        {
            _red1 = value;
        }
    }
    private int _green1;
    public int Green1
    {
        get => _green1;
        set
        {
            _green1 = value;
        }
    }
    private int _blue1;
    public int Blue1
    {
        get => _blue1;
        set
        {
            _blue1 = value;
        }
    }

    private int _alpha2 = 255;
    public int Alpha2
    {
        get => _alpha2;
        set
        {
            _alpha2 = value;
        }
    }
    private int _red2;
    public int Red2
    {
        get => _red2;
        set
        {
            _red2 = value;
        }
    }
    private int _green2;
    public int Green2
    {
        get => _green2;
        set
        {
            _green2 = value;
        }
    }
    private int _blue2;
    public int Blue2
    {
        get => _blue2;
        set
        {
            _blue2 = value;
        }
    }

    private float _gradientX1;
    public float GradientX1
    {
        get => _gradientX1;
        set
        {
            _gradientX1 = value;
        }
    }
    private GeneralUnitType _gradientX1Units;
    public GeneralUnitType GradientX1Units
    {
        get => _gradientX1Units;
        set
        {
            _gradientX1Units = value;
        }
    }
    private float _gradientY1;
    public float GradientY1
    {
        get => _gradientY1;
        set
        {
            _gradientY1 = value;
        }
    }
    private GeneralUnitType _gradientY1Units;
    public GeneralUnitType GradientY1Units
    {
        get => _gradientY1Units;
        set
        {
            _gradientY1Units = value;
        }
    }

    private float _gradientX2;
    public float GradientX2
    {
        get => _gradientX2;
        set
        {
            _gradientX2 = value;
        }
    }

    private GeneralUnitType _gradientX2Units;
    public GeneralUnitType GradientX2Units
    {
        get => _gradientX2Units;
        set
        {
            _gradientX2Units = value;
        }
    }

    private float _gradientY2;
    public float GradientY2
    {
        get => _gradientY2;
        set
        {
            _gradientY2 = value;
        }
    }
    private GeneralUnitType _gradientY2Units;
    public GeneralUnitType GradientY2Units
    {
        get => _gradientY2Units;
        set
        {
            _gradientY2Units = value;
        }
    }


    private float _gradientInnerRadius;
    public float GradientInnerRadius
    {
        get => _gradientInnerRadius;
        set
        {
            _gradientInnerRadius = value;
        }
    }
    private DimensionUnitType _gradientInnerRadiusUnits;
    public DimensionUnitType GradientInnerRadiusUnits
    {
        get => _gradientInnerRadiusUnits;
        set
        {
            _gradientInnerRadiusUnits = value;
        }
    }

    private float _gradientOuterRadius;
    public float GradientOuterRadius
    {
        get => _gradientOuterRadius;
        set
        {
            _gradientOuterRadius = value;
        }
    }
    private DimensionUnitType _gradientOuterRadiusUnits;
    public DimensionUnitType GradientOuterRadiusUnits
    {
        get => _gradientOuterRadiusUnits;
        set
        {
            _gradientOuterRadiusUnits = value;
        }
    }

    #endregion

    #region Dropshadow

    Color _dropshadowColor;

    public Color DropshadowColor
    {
        get => _dropshadowColor;
        set
        {
            _dropshadowColor = value;
        }
    }

    public int DropshadowAlpha
    {
        get => DropshadowColor.A;
        set
        {
            this.DropshadowColor = new Color(this.DropshadowColor.R, this.DropshadowColor.G, this.DropshadowColor.B, (byte)value);
        }
    }

    public int DropshadowBlue
    {
        get => DropshadowColor.B;
        set
        {
            this.DropshadowColor = new Color(this.DropshadowColor.R, this.DropshadowColor.G, (byte)value, this.DropshadowColor.A);
        }
    }

    public int DropshadowGreen
    {
        get => DropshadowColor.G;
        set
        {
            this.DropshadowColor = new Color(this.DropshadowColor.R, (byte)value, this.DropshadowColor.B, this.DropshadowColor.A);
        }
    }

    public int DropshadowRed
    {
        get => DropshadowColor.R;
        set
        {
            this.DropshadowColor = new Color((byte)value, this.DropshadowColor.G, this.DropshadowColor.B, this.DropshadowColor.A);
        }
    }

    private bool _hasDropshadow;

    public bool HasDropshadow
    {
        get => _hasDropshadow;
        set
        {
            _hasDropshadow = value;
        }
    }

    private float _dropshadowOffsetX;

    public float DropshadowOffsetX
    {
        get => _dropshadowOffsetX;
        set
        {
            _dropshadowOffsetX = value;
        }
    }

    private float _dropshadowOffsetY;

    public float DropshadowOffsetY
    {
        get => _dropshadowOffsetY;
        set
        {
            _dropshadowOffsetY = value;
        }
    }

    private float _dropshadowBlurX;

    public float DropshadowBlurX
    {
        get => _dropshadowBlurX;
        set
        {
            _dropshadowBlurX = value;
        }
    }

    private float _dropshadowBlurY;

    public float DropshadowBlurY
    {
        get => _dropshadowBlurY;
        set
        {
            _dropshadowBlurY = value;
        }
    }

    #endregion




    bool _isFilled = true;
    public bool IsFilled
    {
        get => _isFilled;
        set
        {
            _isFilled = value;
        }
    }

    float _strokeWidth = 2;
    public float StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            _strokeWidth = value;
        }
    }

    public override void PreRender()
    {
        //do nothing?
    }

    protected Gradient GetGradient(float absoluteLeft, float absoluteTop)
    {
        var firstColor = new Microsoft.Xna.Framework.Color(
                (byte)Red1, (byte)Green1, (byte)Blue1, (byte)Alpha1);
        var secondColor = new Microsoft.Xna.Framework.Color(
            (byte)Red2, (byte)Green2, (byte)Blue2, (byte)Alpha2);

        var effectiveGradientX1 = absoluteLeft + GradientX1;
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


        var effectiveGradientY1 = absoluteTop + GradientY1;
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


        if(_gradientType == GradientType.Linear)
        {
            var effectiveGradientX2 = absoluteLeft + GradientX2;
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
            var effectiveGradientY2 = absoluteTop + GradientY2;
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

            return new Gradient(new Vector2(effectiveGradientX1, effectiveGradientY1),
                firstColor,
                new Vector2(effectiveGradientX2, effectiveGradientY2),
                secondColor
                );
        }
        else
        {
            var effectiveGradientX2 = effectiveGradientX1 + _gradientOuterRadius;
            var effectiveGradientY2 = effectiveGradientY1;
            return new Gradient(new Vector2(effectiveGradientX1, effectiveGradientY1), 
                firstColor,
                new Vector2(effectiveGradientX2, effectiveGradientY2),
                secondColor,
                s:Gradient.Shape.Radial);
        }

        // todo - eventually support rotation
        //var rectToUse = boundingRect;
        //if (absoluteRotation != 0)
        //{
        //    rectToUse = Unrotate(boundingRect, absoluteRotation);
        //}
        //else
        //{
        //    // If we apply rotation, then the camera coordinates are adjusted such that the gradient coordiantes are relative to the object.
        //    // Otherwise, they are not so we need to offset:
        //    effectiveGradientX1 += rectToUse.Left;
        //    effectiveGradientY1 += rectToUse.Top;
        //    effectiveGradientX2 += rectToUse.Left;
        //    effectiveGradientY2 += rectToUse.Top;
        //}

    }

    public override string BatchKey => "Apos.Shapes";

    public override void StartBatch(ISystemManagers systemManagers)
    {
        var sb = ShapeRenderer.ShapeBatch;
        sb.Begin();
    }

    public override void EndBatch(ISystemManagers systemManagers)
    {
        var sb = ShapeRenderer.ShapeBatch;
        sb.End();
    }
}
