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
                mContainedNineSlice = this.RenderableComponent as NineSlice;
            }
            return mContainedNineSlice;
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

    public Texture2D? Texture
    {
        get => ContainedNineSlice.Texture;
        set => ContainedNineSlice.Texture = value;
    }

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
