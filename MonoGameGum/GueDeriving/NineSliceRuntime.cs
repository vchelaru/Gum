using Gum.Graphics.Animation;
using Gum.Managers;
using Gum.RenderingLibrary;
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
    public class NineSliceRuntime : InteractiveGue
    {
        #region Static Defaults
        public static string DefaultSourceFile { get; set; }
        public static int DefaultTextureLeft;
        public static int DefaultTextureTop;
        public static int DefaultTextureWidth;
        public static int DefaultTextureHeight;
        public static TextureAddress DefaultTextureAddress;
        #endregion

        #region Contained Nineslice

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

        #endregion

        #region Color/Blend

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
                BlendState = value.ToBlendState().ToXNA();

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

        #endregion

        #region Animation

        public bool Animate
        {
            get => ContainedNineSlice.Animate;
            set
            {
                ContainedNineSlice.Animate = value;
            }
        }

        public string CurrentChainName
        {
            get => ContainedNineSlice.CurrentChainName;
            set => ContainedNineSlice.CurrentChainName = value;
        }

        public AnimationChainList AnimationChains
        {
            get => ContainedNineSlice.AnimationChains;
            set
            {
                ContainedNineSlice.AnimationChains = value;
                if (ContainedNineSlice.UpdateToCurrentAnimationFrame())
                {
                    UpdateTextureValuesFrom(ContainedNineSlice);
                }
            }
        }

        public float? CustomFrameTextureCoordinateWidth
        {
            get => ContainedNineSlice.CustomFrameTextureCoordinateWidth;
            set => ContainedNineSlice.CustomFrameTextureCoordinateWidth = value;
        }

        #endregion

        #region Source File / Texture

        [Obsolete("Use Texture")]
        public Microsoft.Xna.Framework.Graphics.Texture2D SourceFile
        {
            get => Texture;
            set => Texture = value;
        }

        public Microsoft.Xna.Framework.Graphics.Texture2D Texture
        {
            get
            {
                return ContainedNineSlice.TopLeftTexture;
            }
            set => ContainedNineSlice.SetSingleTexture(value);
        }

        public string SourceFileName
        {
            set
            {
                base.SetProperty("SourceFile", value);
                if (ContainedNineSlice.UpdateToCurrentAnimationFrame())
                {
                    UpdateTextureValuesFrom(ContainedNineSlice);
                }
            }
        }

        #endregion

        public float BorderScale
        {
            get => ContainedNineSlice.BorderScale;
            set => ContainedNineSlice.BorderScale = value;
        }

        public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);


        public NineSliceRuntime(bool fullInstantiation = true)
        {
            if(fullInstantiation)
            {
                var mContainedNineSlice = new NineSlice();
                SetContainedObject(mContainedNineSlice);

                // todo - need to make this work with different relative directories...
                //this.SourceFileName = DefaultSourceFile;
                this.TextureLeft = DefaultTextureLeft;
                this.TextureTop = DefaultTextureTop;
                this.TextureWidth = DefaultTextureWidth;
                this.TextureHeight = DefaultTextureHeight;

                this.TextureAddress = DefaultTextureAddress;
            }
        }
    }
}
