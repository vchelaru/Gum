using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Gum.Converters;
using Gum.Managers;

namespace GumUnitTests.ConverterTests
{
    [TestFixture]
    public class UnitConverterTests
    {
        [TestFixtureSetUp]
        public void Initialize()
        {

        }


        [Test]
        public void Test()
        {
            float outX, outY;
            float inX, inY;


            UnitConverter.Self.ConvertToPixelCoordinates(
                50, 25, PositionUnitType.PercentageWidth, PositionUnitType.PercentageHeight, 512, 512, out outX, out outY);

            if (outX != 256 || outY != 128)
            {
                throw new Exception("Converting to pixel coordinates for percentage is not working properly");
            }


            UnitConverter.Self.ConvertToUnitTypeCoordinates(
                512, 512, PositionUnitType.PixelsFromRight, PositionUnitType.PixelsFromBottom, 512, 512, out outX, out outY);

            if (outX != 0 || outY != 0)
            {
                throw new Exception("Converting to UnitType for PixelsFromRight/Bottom is not working properly");
            }
        }
    }
}
