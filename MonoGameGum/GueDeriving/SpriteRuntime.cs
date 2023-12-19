using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving
{
    public class SpriteRuntime : global::Gum.Wireframe.GraphicalUiElement
    {
        RenderingLibrary.Graphics.Sprite mContainedSprite;
        RenderingLibrary.Graphics.Sprite ContainedSprite
        {
            get
            {
                if (mContainedSprite == null)
                {
                    mContainedSprite = this.RenderableComponent as RenderingLibrary.Graphics.Sprite;
                }
                return mContainedSprite;
            }
        }

        public int Alpha
        {
            get
            {
                return ContainedSprite.Alpha;
            }
            set
            {
                ContainedSprite.Alpha = value;
                NotifyPropertyChanged();
            }
        }
        public Gum.RenderingLibrary.Blend Blend
        {
            get
            {
                return Gum.RenderingLibrary.BlendExtensions.ToBlend(ContainedSprite.BlendState);
            }
            set
            {
                ContainedSprite.BlendState = Gum.RenderingLibrary.BlendExtensions.ToBlendState(value);
                NotifyPropertyChanged();
            }
        }
        public int Blue
        {
            get
            {
                return ContainedSprite.Blue;
            }
            set
            {
                ContainedSprite.Blue = value;
                NotifyPropertyChanged();
            }
        }
        public bool FlipVertical
        {
            get
            {
                return ContainedSprite.FlipVertical;
            }
            set
            {
                ContainedSprite.FlipVertical = value;
                NotifyPropertyChanged();
            }
        }
        public int Green
        {
            get
            {
                return ContainedSprite.Green;
            }
            set
            {
                ContainedSprite.Green = value;
                NotifyPropertyChanged();
            }
        }
        public int Red
        {
            get
            {
                return ContainedSprite.Red;
            }
            set
            {
                ContainedSprite.Red = value;
                NotifyPropertyChanged();
            }
        }
        public Microsoft.Xna.Framework.Graphics.Texture2D SourceFile
        {
            get
            {
                return ContainedSprite.Texture;
            }
            set
            {
                this.Texture = value;
            }
        }
        public Microsoft.Xna.Framework.Color Color
        {
            get
            {
                return RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedSprite.Color);
            }
            set
            {
                ContainedSprite.Color = RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
                NotifyPropertyChanged();
            }
        }
        public Microsoft.Xna.Framework.Graphics.Texture2D Texture
        {
            get
            {
                return ContainedSprite.Texture;
            }
            set
            {
                var shouldUpdateLayout = false;
                int widthBefore = -1;
                int heightBefore = -1;
                var isUsingPercentageWidthOrHeight = WidthUnits == Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile || HeightUnits == Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
                if (isUsingPercentageWidthOrHeight)
                {
                    if (ContainedSprite.Texture != null)
                    {
                        widthBefore = ContainedSprite.Texture.Width;
                        heightBefore = ContainedSprite.Texture.Height;
                    }
                }
                ContainedSprite.Texture = value;
                if (isUsingPercentageWidthOrHeight)
                {
                    int widthAfter = -1;
                    int heightAfter = -1;
                    if (ContainedSprite.Texture != null)
                    {
                        widthAfter = ContainedSprite.Texture.Width;
                        heightAfter = ContainedSprite.Texture.Height;
                    }
                    shouldUpdateLayout = widthBefore != widthAfter || heightBefore != heightAfter;
                }
                if (shouldUpdateLayout)
                {
                    UpdateLayout();
                }
            }
        }

        public string SourceFileName
        {
            set
            {
                base.SetProperty("SourceFile", value);
                if (ContainedSprite.UpdateToCurrentAnimationFrame())
                {
                    UpdateTextureValuesFrom(ContainedSprite);
                }
            }
        }
        public SpriteRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if (fullInstantiation)
            {
                mContainedSprite = new RenderingLibrary.Graphics.Sprite(null);
                SetContainedObject(mContainedSprite);
                Width = 100;
                Height = 100;
                WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
                HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            }
        }

        public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

    }
}
