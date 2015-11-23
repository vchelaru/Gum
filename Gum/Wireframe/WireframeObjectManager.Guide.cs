using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Math.Geometry;
using Gum.Managers;
using Gum.DataTypes;
using RenderingLibrary;
using Gum.Converters;

namespace Gum.Wireframe
{
    public partial class WireframeObjectManager
    {
        List<LineRectangle> mGuideRectangles = new List<LineRectangle>();


        public void UpdateGuides()
        {
            ClearGuideRectangles();

            if(ObjectFinder.Self.GumProjectSave != null)
            {
                for (int i = 0; i < ObjectFinder.Self.GumProjectSave.Guides.Count; i++)
                {
                    GuideRectangle guideRectangle = ObjectFinder.Self.GumProjectSave.Guides[i];

                    LineRectangle rectangle = new LineRectangle();

                    float absoluteX;
                    float absoluteY;

                    var xUnitType = UnitConverter.ConvertToGeneralUnit(guideRectangle.XUnitType);
                    var yUnitType = UnitConverter.ConvertToGeneralUnit(guideRectangle.YUnitType);

                    UnitConverter.Self.ConvertToPixelCoordinates(
                        guideRectangle.X,
                        guideRectangle.Y,
                        xUnitType,
                        yUnitType,
                        ObjectFinder.Self.GumProjectSave.DefaultCanvasWidth,
                        ObjectFinder.Self.GumProjectSave.DefaultCanvasHeight,
                        0, 0,
                        out absoluteX,
                        out absoluteY);

                    rectangle.X = absoluteX;
                    rectangle.Y = absoluteY;

                    xUnitType = UnitConverter.ConvertToGeneralUnit(guideRectangle.WidthUnitType);
                    yUnitType = UnitConverter.ConvertToGeneralUnit(guideRectangle.HeightUnitType);

                    float absoluteWidth;
                    float absoluteHeight;
                    UnitConverter.Self.ConvertToPixelCoordinates(
                        guideRectangle.Width,
                        guideRectangle.Height,
                        xUnitType,
                        yUnitType,
                        ObjectFinder.Self.GumProjectSave.DefaultCanvasWidth,
                        ObjectFinder.Self.GumProjectSave.DefaultCanvasHeight,
                        0, 0,
                        out absoluteWidth,
                        out absoluteHeight);


                    rectangle.Width = absoluteWidth;
                    rectangle.Height = absoluteHeight;

                    rectangle.Name = guideRectangle.Name;
                    rectangle.Color = new Microsoft.Xna.Framework.Color(1.0f, 1.0f, 1.0f, .5f);

                    mGuideRectangles.Add(rectangle);
                    ShapeManager.Self.Add(rectangle);
                }

            }
        }

        private void ClearGuideRectangles()
        {

            for (int i = 0; i < mGuideRectangles.Count; i++)
            {
                ShapeManager.Self.Remove(mGuideRectangles[i]);
            }

            mGuideRectangles.Clear();
        }

        public IPositionedSizedObject GetGuide(string guideName)
        {
            foreach (LineRectangle lineRectangle in mGuideRectangles)
            {
                if (lineRectangle.Name == guideName)
                {
                    return lineRectangle;
                }
            }
            return null;
        }

    }
}
