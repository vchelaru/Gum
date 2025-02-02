using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving
{
    public class TextRuntime : BindableGue
    {
        Text mContainedText;
        Text ContainedText
        {
            get
            {
                if (mContainedText == null)
                {
                    mContainedText = this.RenderableComponent as Text;
                }
                return mContainedText;
            }
        }

        public Gum.BlendState BlendState
        {
            get => ContainedText.BlendState;
            set
            {
                ContainedText.BlendState = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(Blend));
            }
        }

        public Gum.RenderingLibrary.Blend Blend
        {
            get
            {
                return Gum.RenderingLibrary.BlendExtensions.ToBlend(ContainedText.BlendState);
            }
            set
            {
                BlendState = Gum.RenderingLibrary.BlendExtensions.ToBlendState(value);
                // NotifyPropertyChanged handled by BlendState:
            }
        }

        public int Red
        {
            get => mContainedText.Red;
            set => mContainedText.Red = value;
        }

        public int Green
        {
            get => mContainedText.Green;
            set => mContainedText.Green = value;
        }

        public int Blue
        {
            get => mContainedText.Blue;
            set => mContainedText.Blue = value;
        }

        public Microsoft.Xna.Framework.Color Color
        {
            get
            {
                return RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedText.Color);
            }
            set
            {
                ContainedText.Color = RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
                NotifyPropertyChanged();
            }
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get => ContainedText.HorizontalAlignment;
            set => ContainedText.HorizontalAlignment = value;
        }

        public VerticalAlignment VerticalAlignment
        {
            get => ContainedText.VerticalAlignment;
            set => ContainedText.VerticalAlignment = value;
        }

        public BitmapFont BitmapFont
        {
            get => ContainedText.BitmapFont;
            set
            {
                if (value != BitmapFont)
                {
                    ContainedText.BitmapFont = value;
                    NotifyPropertyChanged();
                    UpdateLayout();
                }
            }
        }

        public float FontScale
        {
            get => ContainedText.FontScale;
            set
            {
                if (value != FontScale)
                {
                    ContainedText.FontScale = value;
                    NotifyPropertyChanged();
                    UpdateLayout();
                }
            }
        }

        public float LineHeightMultiplier
        {
            get => ContainedText.LineHeightMultiplier;
            set
            {
                if (value != LineHeightMultiplier)
                {
                    ContainedText.LineHeightMultiplier = value;
                    NotifyPropertyChanged();
                    UpdateLayout();
                }
            }
        }

        public TextOverflowHorizontalMode TextOverflowHorizontalMode
        {
            // Currently GraphicalUiElement doesn't expose this property so we have to go through setting it by string:
            get => ContainedText.IsTruncatingWithEllipsisOnLastLine ? TextOverflowHorizontalMode.EllipsisLetter : TextOverflowHorizontalMode.TruncateWord;
            set
            {
                ContainedText.IsTruncatingWithEllipsisOnLastLine = value == TextOverflowHorizontalMode.EllipsisLetter;
                NotifyPropertyChanged();
                UpdateLayout();
            }
        }

        public string Text
        {
            get
            {
                return ContainedText.RawText;
            }
            set
            {
                var widthBefore = ContainedText.WrappedTextWidth;
                var heightBefore = ContainedText.WrappedTextHeight;
                if (this.WidthUnits == Gum.DataTypes.DimensionUnitType.RelativeToChildren)
                {
                    // make it have no line wrap width before assignign the text:
                    ContainedText.Width = null;
                }

                // Use SetProperty so it goes through the BBCode-checking methods
                //ContainedText.RawText = value;
                this.SetProperty("Text", value);

                NotifyPropertyChanged();
                var shouldUpdate = widthBefore != ContainedText.WrappedTextWidth || heightBefore != ContainedText.WrappedTextHeight;
                if (shouldUpdate)
                {
                    UpdateLayout(
                        Gum.Wireframe.GraphicalUiElement.ParentUpdateType.IfParentWidthHeightDependOnChildren | 
                        Gum.Wireframe.GraphicalUiElement.ParentUpdateType.IfParentStacks, int.MaxValue / 2);
                }
            }
        }

        #region Defaults



        // todo - add more here
        public static string DefaultFont = "Arial";
        public static int DefaultFontSize = 18;

        public float DefaultWidth = 100;
        public float DefaultHeight = 50;

        public DimensionUnitType DefaultWidthUnits = DimensionUnitType.Absolute;
        public DimensionUnitType DefaultHeightUnits = DimensionUnitType.Absolute;

        #endregion

        public TextRuntime(bool fullInstantiation = true)
        {
            if(fullInstantiation)
            {
                var textRenderable = new Text(SystemManagers.Default);
                textRenderable.RenderBoundary = false;
                mContainedText = textRenderable;
                
                SetContainedObject(textRenderable);

                Width = DefaultWidth;
                WidthUnits = DefaultWidthUnits;
                Height = DefaultHeight;
                HeightUnits = DefaultHeightUnits;
                this.FontSize = DefaultFontSize;
                this.Font = DefaultFont;

                textRenderable.RawText = "Hello World";
            }
        }

        public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer:null);
    }
}
