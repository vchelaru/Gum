using Gum.Renderables;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.GueDeriving;
public class SpriteRuntime : GraphicalUiElement
{
    #region Contained Sprite
    Sprite mContainedSprite;
    Sprite ContainedSprite
    {
        get
        {
            if (mContainedSprite == null)
            {
                mContainedSprite = (Sprite)this.RenderableComponent;
            }
            return mContainedSprite;
        }
    }

    #endregion

    #region Color/Blend

    public int Alpha
    {
        get => ContainedSprite.Alpha;
        set
        {
            ContainedSprite.Alpha = value;
            NotifyPropertyChanged();
        }
    }

    public int Red
    {
        get => ContainedSprite.Red;
        set
        {
            ContainedSprite.Red = value;
            NotifyPropertyChanged();
        }
    }

    public int Green
    {
        get => ContainedSprite.Green;
        set
        {
            ContainedSprite.Green = value;
            NotifyPropertyChanged();
        }
    }

    public int Blue
    {
        get => ContainedSprite.Blue;
        set
        {
            ContainedSprite.Blue = value;
            NotifyPropertyChanged();
        }
    }

    public Color Color
    {
        get => ContainedSprite.Color;
        set
        {
            ContainedSprite.Color = value;
            NotifyPropertyChanged();
        }
    }

    #endregion

    public bool FlipVertical
    {
        get => ContainedSprite.FlipVertical;
        set
        {
            ContainedSprite.FlipVertical = value;
            NotifyPropertyChanged();
        }
    }

    public Raylib_cs.Rectangle SourceRectangle
    {
        get => new Raylib_cs.Rectangle(TextureLeft, TextureTop, TextureWidth, TextureHeight);
        set
        {
            TextureLeft = (int)value.X;
            TextureTop = (int)value.Y;
            TextureWidth = (int)value.Width;
            TextureHeight = (int)value.Height;
        }
    }

    #region AnimationChain

    #endregion

    #region Source File/Texture

    public Texture2D? Texture
    {
        get => ContainedSprite.Texture;
        set
        {
            ContainedSprite.Texture = value;
            NotifyPropertyChanged();
        }
    }

    public string SourceFileName
    {
        set
        {
            base.SetProperty("SourceFile", value);
            // todo:
            //if (ContainedSprite.UpdateToCurrentAnimationFrame())
            //{
            //    UpdateTextureValuesFrom(ContainedSprite);
            //}
        }
    }

    #endregion

    public SpriteRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if (fullInstantiation)
        {
            mContainedSprite = new Sprite();
            SetContainedObject(mContainedSprite);
            Width = 100;
            Height = 100;
            WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (SpriteRuntime)base.Clone();

        toReturn.mContainedSprite = null;

        return toReturn;
    }

    /// <inheritdoc cref="GraphicalUiElement.AddToManagers()"/>
    [Obsolete("Use the AddToRoot extension method instead (e.g. mySprite.AddToRoot()).")]
    public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

}
