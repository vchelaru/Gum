using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.GueDeriving
{
    public class SolidRectangleRuntime : BindableGraphicalUiElement
    {
        SolidRectangle mContainedRectangle;
        SolidRectangle ContainedRectangle
        {
            get
            {
                if(mContainedRectangle == null)
                {
                    mContainedRectangle = this.RenderableComponent as SolidRectangle;
                }
                return mContainedRectangle;
            }
        }

        public SKColor Color
        {
            get => ContainedRectangle.Color;
            set => ContainedRectangle.Color = value;
        }

        public SolidRectangleRuntime(bool fullInstantiation = true)
        {
            if(fullInstantiation)
            {
                SetContainedObject(new SolidRectangle());
            }
        }
    }
}
