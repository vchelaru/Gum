using System;
using Gum.DataTypes;
using System.ComponentModel;

namespace Gum.PropertyGridHelpers
{
    public class GuideRectanglePropertyGridDisplayer : PropertyGridDisplayer
    {
        ReflectingPropertyDescriptorHelper mHelper = new ReflectingPropertyDescriptorHelper();

        public GuideRectangle GuideRectangle
        {
            get;
            set;
        }

        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorCollection pdc = mHelper.GetEmpty();

            mHelper.CurrentInstance = GuideRectangle;

            mHelper.Include(GuideRectangle, "Name", ref pdc);

            mHelper.Include(GuideRectangle, "X", ref pdc);
            mHelper.Include(GuideRectangle, "XUnitType", ref pdc);

            mHelper.Include(GuideRectangle, "Y", ref pdc);
            mHelper.Include(GuideRectangle, "YUnitType", ref pdc);

            mHelper.Include(GuideRectangle, "Width", ref pdc);
            mHelper.Include(GuideRectangle, "WidthUnitType", ref pdc);

            mHelper.Include(GuideRectangle, "Height", ref pdc);
            mHelper.Include(GuideRectangle, "HeightUnitType", ref pdc);



            return pdc;
        }


    }
}
