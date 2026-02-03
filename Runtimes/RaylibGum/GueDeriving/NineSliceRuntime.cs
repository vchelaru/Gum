#if MONOGAME || KNI || XNA4 || FNA
#define XNALIKE
#endif
using Gum.Wireframe;
using Raylib_cs;
using Gum.Renderables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.GueDeriving;
public class NineSliceRuntime : InteractiveGue
{
    NineSlice mContainedNineSlice;

    NineSlice ContainedNineSlice
    {
        get
        {
            if(mContainedNineSlice == null)
            {
                mContainedNineSlice = (NineSlice)this.RenderableComponent;
            }
            return mContainedNineSlice;
        }
    }

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

    public Color Color
    {
        get
        {
            return ContainedNineSlice.Color;
        }
        set
        {
            ContainedNineSlice.Color = value;
            NotifyPropertyChanged();
        }
    }

    #endregion

    public Texture2D? Texture
    {
        get => ContainedNineSlice.Texture;
        set => ContainedNineSlice.Texture = value;
    }

    public string SourceFileName
    {
        set
        {
            base.SetProperty("SourceFile", value);

            // todo - need to support .achx in raylib NineSlices
#if XNALIKE
            if (ContainedNineSlice.UpdateToCurrentAnimationFrame())
            {
                UpdateTextureValuesFrom(ContainedNineSlice);
            }
#endif
        }
    }

#if XNALIKE
    public float BorderScale
    {
        get => ContainedNineSlice.BorderScale;
        set => ContainedNineSlice.BorderScale = value;
    }
#endif

    public NineSliceRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            var mContainedNineSlice = new NineSlice();
            SetContainedObject(mContainedNineSlice);

            Width = 100;
            Height = 100;

            // todo - need to make this work with different relative directories...
            //this.SourceFileName = DefaultSourceFile;
            //this.TextureLeft = DefaultTextureLeft;
            //this.TextureTop = DefaultTextureTop;
            //this.TextureWidth = DefaultTextureWidth;
            //this.TextureHeight = DefaultTextureHeight;

            //this.TextureAddress = DefaultTextureAddress;
        }
    }

}
