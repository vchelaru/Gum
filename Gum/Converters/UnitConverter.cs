using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.Managers;
using Gum.DataTypes;
using Gum.Wireframe;

namespace Gum.Converters
{
    public enum XOrY
    {
        X,
        Y
    }

    public class UnitConverter
    {
        static UnitConverter mSelf = new UnitConverter();


        public static UnitConverter Self
        {
            get { return mSelf; }
        }


        public void ConvertToPixelCoordinates(float relativeX, float relativeY, object xUnitType, object yUnitType, float parentWidth, float parentHeight, 
            float fileWidth, float fileHeight, out float absoluteX, out float absoluteY)
        {
            try
            {
                GeneralUnitType generalX = GeneralUnitType.PixelsFromSmall;
                if (xUnitType != null)
                {
                    generalX = ConvertToGeneralUnit(xUnitType);
                }

                GeneralUnitType generalY = GeneralUnitType.PixelsFromSmall;
                if (yUnitType != null)
                {
                    generalY = ConvertToGeneralUnit(yUnitType);
                }

                absoluteX = relativeX;
                absoluteY = relativeY;

                if (generalX == GeneralUnitType.Percentage)
                {
                    absoluteX = parentWidth * relativeX / 100.0f;
                }
                else if (generalX == GeneralUnitType.PixelsFromMiddle)
                {
                    absoluteX = parentWidth / 2.0f + relativeX;
                }
                else if (generalX == GeneralUnitType.PixelsFromLarge)
                {
                    absoluteX = parentWidth + relativeX;
                }
                else if (generalX == GeneralUnitType.PercentageOfFile)
                {
                    absoluteX = fileWidth * relativeX / 100.0f;
                }


                if (generalY == GeneralUnitType.Percentage)
                {
                    absoluteY = parentHeight * relativeY / 100.0f;
                }
                else if (generalY == GeneralUnitType.PixelsFromMiddle)
                {
                    absoluteY = parentHeight / 2.0f + relativeY;
                }
                else if (generalY == GeneralUnitType.PixelsFromLarge)
                {
                    absoluteY = parentHeight + relativeY;
                }
                else if (generalY == GeneralUnitType.PercentageOfFile)
                {
                    absoluteY = fileHeight * relativeY / 100.0f;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        public void ConvertToUnitTypeCoordinates(float absoluteX, float absoluteY, object xUnitType, object yUnitType, float parentWidth, float parentHeight, float fileWidth, float fileHeight, out float relativeX, out float relativeY)
        {
            GeneralUnitType generalX = GeneralUnitType.PixelsFromSmall;
            if (xUnitType != null)
            {
                generalX = ConvertToGeneralUnit(xUnitType);
            }

            GeneralUnitType generalY = GeneralUnitType.PixelsFromSmall;
            if (yUnitType != null)
            {
                generalY = ConvertToGeneralUnit(yUnitType);
            }

            relativeX = absoluteX;
            relativeY = absoluteY;

            if (generalX == GeneralUnitType.Percentage)
            {
                relativeX = 100 * absoluteX / parentWidth;
            }
            else if (generalX == GeneralUnitType.PixelsFromMiddle)
            {
                relativeX = absoluteX - parentWidth / 2.0f;
            }
            else if (generalX == GeneralUnitType.PixelsFromLarge)
            {
                relativeX = absoluteX - parentWidth;
            }
            else if (generalX == GeneralUnitType.PercentageOfFile)
            {
                relativeX = 100 * absoluteX / fileWidth;
            }

            if (generalY == GeneralUnitType.Percentage)
            {
                relativeY = 100 * absoluteY / parentHeight;
            }
            else if (generalY == GeneralUnitType.PixelsFromMiddle)
            {
                relativeY = absoluteY - parentHeight / 2.0f;
            }
            else if (generalY == GeneralUnitType.PixelsFromLarge)
            {
                relativeY = absoluteY - parentHeight;
            }
            else if (generalY == GeneralUnitType.PercentageOfFile)
            {
                relativeY = 100 * absoluteY / fileHeight;
            }

        }


        public GeneralUnitType ConvertToGeneralUnit(object specificUnit)
        {
            if (specificUnit is PositionUnitType)
            {
                PositionUnitType asPut = (PositionUnitType)specificUnit;

                switch (asPut)
                {
                    case PositionUnitType.PercentageHeight:
                    case PositionUnitType.PercentageWidth:
                        return GeneralUnitType.Percentage;
                    case PositionUnitType.PixelsFromLeft:
                    case PositionUnitType.PixelsFromTop:
                        return GeneralUnitType.PixelsFromSmall;
                    case PositionUnitType.PixelsFromRight:
                    case PositionUnitType.PixelsFromBottom:
                        return GeneralUnitType.PixelsFromLarge;
                    case PositionUnitType.PixelsFromCenterX:
                        return GeneralUnitType.PixelsFromMiddle;
                    case PositionUnitType.PixelsFromCenterY:
                        return GeneralUnitType.PixelsFromMiddle;
                    default:
                        throw new NotImplementedException();
                }
            }

            else if (specificUnit is DimensionUnitType)
            {
                DimensionUnitType asDut = (DimensionUnitType)specificUnit;

                switch(asDut)
                {
                    case DimensionUnitType.Absolute:
                        return GeneralUnitType.PixelsFromSmall;
                    case DimensionUnitType.Percentage:
                        return GeneralUnitType.Percentage;
                    case DimensionUnitType.RelativeToContainer:
                        return GeneralUnitType.PixelsFromLarge;
                    case DimensionUnitType.PercentageOfSourceFile:
                        return GeneralUnitType.PercentageOfFile;
                    default:
                        throw new NotImplementedException();
                }
            }

            throw new NotImplementedException();
        }


    }
}
