using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.Managers;



namespace Gum.Managers
{
    public enum PositionUnitType
    {
        PixelsFromLeft,
        PixelsFromTop,
        PercentageWidth,
        PercentageHeight,
        PixelsFromRight,
        PixelsFromBottom,
        PixelsFromCenterX,
        PixelsFromCenterY,
        PixelsFromCenterYInverted


    }
}


namespace Gum.Converters
{

    public enum GeneralUnitType
    {
        PixelsFromSmall,
        PixelsFromLarge,
        PixelsFromMiddle,
        Percentage,
        PercentageOfFile,
        PixelsFromMiddleInverted,
        PercentageOfOtherDimension
    }

    public static class GeneralUnitTypeExtensions
    {
        public static bool GetIsPixelBased(this GeneralUnitType unitType)
        {
            return unitType == GeneralUnitType.PixelsFromSmall ||
                unitType == GeneralUnitType.PixelsFromMiddle ||
                unitType == GeneralUnitType.PixelsFromMiddleInverted ||
                unitType == GeneralUnitType.PixelsFromLarge;
        }

    }


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

        public float ConvertXPosition(float xToConvert, GeneralUnitType fromUnit, GeneralUnitType toUnit, float parentWidth)
        {
            float throwaway = 0;
            float pixelX;

            // convert from whatever unit to pixel:
            ConvertToPixelCoordinates(xToConvert, 0, fromUnit, GeneralUnitType.PixelsFromLarge, parentWidth, 0, 0, 0, out pixelX, out throwaway);

            float convertedX;
            // now from pixel back to whatever unit type:
            // owner x and owner wy don't matter here
            ConvertToUnitTypeCoordinates(pixelX, 0, toUnit, GeneralUnitType.PixelsFromLarge, 0,0, parentWidth, 0, 0, 0, out convertedX, out throwaway);

            return convertedX;
        }

        public float ConvertYPosition(float yToConvert, GeneralUnitType fromUnit, GeneralUnitType toUnit, float parentHeight)
        {
            float throwaway = 0;
            float pixelY;

            // convert from whatever unit to pixel:
            ConvertToPixelCoordinates(0, yToConvert, GeneralUnitType.PixelsFromLarge, fromUnit, 0, parentHeight, 0, 0, out throwaway, out pixelY);

            float convertedY;
            // now from pixel back to whatever unit type:
            // owner x and owner wy don't matter here
            ConvertToUnitTypeCoordinates(0, pixelY, GeneralUnitType.PixelsFromLarge, toUnit, 0,0, 0, parentHeight, 0, 0, out throwaway, out convertedY);

            return convertedY;
        }

        public void ConvertToPixelCoordinates(float relativeX, float relativeY, GeneralUnitType generalX, GeneralUnitType generalY, 
            float parentWidth, float parentHeight, 
            float fileWidth, float fileHeight, out float absoluteX, out float absoluteY)
        {
            if(generalX == GeneralUnitType.PixelsFromSmall)
            {
                absoluteX = relativeX;
            }
            else if (generalX == GeneralUnitType.Percentage)
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
            else
            {
                throw new NotImplementedException();
            }

            if(generalY == GeneralUnitType.PixelsFromSmall)
            {
                absoluteY = relativeY;
            }
            else if (generalY == GeneralUnitType.Percentage)
            {
                absoluteY = parentHeight * relativeY / 100.0f;
            }
            else if (generalY == GeneralUnitType.PixelsFromMiddle)
            {
                absoluteY = parentHeight / 2.0f + relativeY;
            }
            else if(generalY == GeneralUnitType.PixelsFromMiddleInverted)
            {
                absoluteY = parentHeight / 2.0f - relativeY;
            }
            else if (generalY == GeneralUnitType.PixelsFromLarge)
            {
                absoluteY = parentHeight + relativeY;
            }
            else if (generalY == GeneralUnitType.PercentageOfFile)
            {
                absoluteY = fileHeight * relativeY / 100.0f;
            }
            else
            {
                throw new NotImplementedException();
            }
        }


        public void ConvertToUnitTypeCoordinates(float pixelXToConvert, float pixelYToConvert, GeneralUnitType generalX, GeneralUnitType generalY,
            // owner X and Y values are needed if the object is using percentage of other unit value
            float ownerWidthInPixels, float ownerHeightInPixels,
            float parentWidth, float parentHeight, float fileWidth, float fileHeight, out float relativeX, out float relativeY)
        {
            relativeY = pixelYToConvert;

            if(generalX == GeneralUnitType.PixelsFromSmall)
            {
                relativeX = pixelXToConvert;

            }
            else if (generalX == GeneralUnitType.Percentage)
            {
                relativeX = 100 * pixelXToConvert / parentWidth;
            }
            else if (generalX == GeneralUnitType.PixelsFromMiddle)
            {
                relativeX = pixelXToConvert - parentWidth / 2.0f;
            }
            else if (generalX == GeneralUnitType.PixelsFromLarge)
            {
                relativeX = pixelXToConvert - parentWidth;
            }
            else if (generalX == GeneralUnitType.PercentageOfFile)
            {
                relativeX = 100 * pixelXToConvert / fileWidth;
            }
            else if(generalX == GeneralUnitType.PercentageOfOtherDimension)
            {
                relativeX = pixelXToConvert / ( ownerHeightInPixels / 100f);
            }
            else
            {
                throw new NotImplementedException();
            }

            if(generalY == GeneralUnitType.PixelsFromSmall)
            {
                relativeY = pixelYToConvert;
            }
            else if (generalY == GeneralUnitType.Percentage)
            {
                relativeY = 100 * pixelYToConvert / parentHeight;
            }
            else if (generalY == GeneralUnitType.PixelsFromMiddle)
            {
                relativeY = pixelYToConvert - parentHeight / 2.0f;
            }
            else if(generalY == GeneralUnitType.PixelsFromMiddleInverted)
            {
                relativeY = -pixelYToConvert - parentHeight / 2.0f;
            }
            else if (generalY == GeneralUnitType.PixelsFromLarge)
            {
                relativeY = pixelYToConvert - parentHeight;
            }
            else if (generalY == GeneralUnitType.PercentageOfFile)
            {
                relativeY = 100 * pixelYToConvert / fileHeight;
            }

            else if (generalY == GeneralUnitType.PercentageOfOtherDimension)
            {
                relativeY = pixelYToConvert / (ownerWidthInPixels/100f);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static bool TryConvertToGeneralUnit(object unitType, out GeneralUnitType result)
        {
            result = GeneralUnitType.PixelsFromSmall;
            try
            {
                result = ConvertToGeneralUnit(unitType);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static GeneralUnitType ConvertToGeneralUnit(object unitType)
        {
            if(unitType == null || (int)unitType == 0) 
            {
                return GeneralUnitType.PixelsFromSmall;
            }
            else if(unitType is PositionUnitType)
            {
                return ConvertToGeneralUnit((PositionUnitType) unitType);
            }
            else if(unitType is DimensionUnitType)
            {
                return ConvertToGeneralUnit((DimensionUnitType)unitType);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static GeneralUnitType ConvertToGeneralUnit(PositionUnitType unitType)
        {
            switch (unitType)
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
                case PositionUnitType.PixelsFromCenterYInverted:
                    return GeneralUnitType.PixelsFromMiddleInverted;
                default:
                    throw new NotImplementedException();
            }
        }

        public static GeneralUnitType ConvertToGeneralUnit(DimensionUnitType unitType)
        { 
            switch (unitType)
            {
                case DimensionUnitType.Absolute:
                case DimensionUnitType.RelativeToChildren:
                    return GeneralUnitType.PixelsFromSmall;
                case DimensionUnitType.Percentage:
                    return GeneralUnitType.Percentage;
                case DimensionUnitType.RelativeToContainer:
                    return GeneralUnitType.PixelsFromLarge;
                case DimensionUnitType.PercentageOfSourceFile:
                    return GeneralUnitType.PercentageOfFile;
                case DimensionUnitType.PercentageOfOtherDimension:
                    return GeneralUnitType.PercentageOfOtherDimension;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
