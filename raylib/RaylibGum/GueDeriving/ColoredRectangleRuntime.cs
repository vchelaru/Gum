using Gum.Wireframe;
using RaylibGum.Renderables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum.GueDeriving;
public class ColoredRectangleRuntime : BindableGue
{
    public static float DefaultWidth = 50;
    public static float DefaultHeight = 50;

    SolidRectangle mContainedColoredRectangle;
    SolidRectangle ContainedColoredRectangle
    {
        get
        {
            if (mContainedColoredRectangle == null)
            {
                mContainedColoredRectangle = this.RenderableComponent as SolidRectangle;
            }
            return mContainedColoredRectangle;
        }
    }


    //public int Alpha
    //{
    //    get
    //    {
    //        return ContainedColoredRectangle.Alpha;
    //    }
    //    set
    //    {
    //        ContainedColoredRectangle.Alpha = value;
    //        NotifyPropertyChanged();
    //    }
    //}

    public int Blue
    {
        get
        {
            return ContainedColoredRectangle.Blue;
        }
        set
        {
            ContainedColoredRectangle.Blue = value;
            NotifyPropertyChanged();
        }
    }
    public int Green
    {
        get
        {
            return ContainedColoredRectangle.Green;
        }
        set
        {
            ContainedColoredRectangle.Green = value;
            NotifyPropertyChanged();
        }
    }
    public int Red
    {
        get
        {
            return ContainedColoredRectangle.Red;
        }
        set
        {
            ContainedColoredRectangle.Red = value;
            NotifyPropertyChanged();
        }
    }

    public Raylib_cs.Color Color
    {
        get => ContainedColoredRectangle.Color;
        set
        {
            ContainedColoredRectangle.Color = value;
            NotifyPropertyChanged();
        }
    }

    public ColoredRectangleRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            var solidRectangle = new SolidRectangle();
            SetContainedObject(solidRectangle);
            mContainedColoredRectangle = solidRectangle;

            solidRectangle.Color = Raylib_cs.Color.White;
            Width = DefaultWidth;
            Height = DefaultHeight;
        }
    }
}
