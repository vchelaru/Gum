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
    public class NineSliceRuntime : GraphicalUiElement
    {
        RenderingLibrary.Graphics.NineSlice mContainedNineSlice;
        RenderingLibrary.Graphics.NineSlice ContainedNineSlice
        {
            get
            {
                if (mContainedNineSlice == null)
                {
                    mContainedNineSlice = this.RenderableComponent as RenderingLibrary.Graphics.NineSlice;
                }
                return mContainedNineSlice;
            }
        }

        public int Alpha
        {
            get
            {
                return ContainedNineSlice.Alpha;
            }
            set
            {
                ContainedNineSlice.Alpha = value;
                NotifyPropertyChanged();
            }
        }

        public Microsoft.Xna.Framework.Graphics.BlendState BlendState
        {
            get => ContainedNineSlice.BlendState.ToXNA();
            set
            {
                ContainedNineSlice.BlendState = value.ToGum();
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(Blend));
            }
        }

        public Gum.RenderingLibrary.Blend Blend
        {
            get
            {
                return Gum.RenderingLibrary.BlendExtensions.ToBlend(ContainedNineSlice.BlendState);
            }
            set
            {
                BlendState = ContainedNineSlice.BlendState.ToXNA();
                // NotifyPropertyChanged handled by BlendState:
            }
        }
        public int Blue
        {
            get
            {
                return ContainedNineSlice.Blue;
            }
            set
            {
                ContainedNineSlice.Blue = value;
                NotifyPropertyChanged();
            }
        }
        public int Green
        {
            get
            {
                return ContainedNineSlice.Green;
            }
            set
            {
                ContainedNineSlice.Green = value;
                NotifyPropertyChanged();
            }
        }
        public int Red
        {
            get
            {
                return ContainedNineSlice.Red;
            }
            set
            {
                ContainedNineSlice.Red = value;
                NotifyPropertyChanged();
            }
        }
        public Microsoft.Xna.Framework.Graphics.Texture2D SourceFile
        {
            get
            {
                return ContainedNineSlice.TopLeftTexture;
            }
            set => ContainedNineSlice.SetSingleTexture(value);
        }
        public Microsoft.Xna.Framework.Color Color
        {
            get
            {
                return RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedNineSlice.Color);
            }
            set
            {
                ContainedNineSlice.Color = RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
                NotifyPropertyChanged();
            }
        }

        public float? CustomFrameTextureCoordinateWidth
        {
            get => ContainedNineSlice.CustomFrameTextureCoordinateWidth;
            set => ContainedNineSlice.CustomFrameTextureCoordinateWidth = value;
        }

        public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

        public string SourceFileName
        {
            set
            {
                base.SetProperty("SourceFile", value);
                //if (ContainedSprite.UpdateToCurrentAnimationFrame())
                //{
                //    UpdateTextureValuesFrom(ContainedSprite);
                //}
            }
        }

        public NineSliceRuntime(bool fullInstantiation = true)
        {
            if(fullInstantiation)
            {
                var mContainedNineSlice = new NineSlice();
                SetContainedObject(mContainedNineSlice);
                
            }
        }
    }
}
