using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.SpecializedXnaControls.Zooming
{
    public class ZoomNumbers
    {
        public float GetZoomIncreaseFor(float currentZoom)
        {
            if (currentZoom < 0)
            {
                throw new ArgumentException("Zoom value must be greater than 0");
            }

            float modifiedValue;
            float multiplier;
            GetMultiplierAndModifiedValue(currentZoom, out modifiedValue, out multiplier);

            float change;

            // Now we have a number between 1 and 10
            if (modifiedValue > 5)
            {
                change = .5f;
            }
            else if (modifiedValue >= 2)
            {
                change = .25f;
            }
            else
            {
                change = .1f;
            }

            return change * multiplier;
        }


        //public float GetZoomIncreaseFor(float currentZoom)
        //{



        //}

        private static float GetMultiplierAndModifiedValue(float currentZoom, out float modifiedValue, out float multiplier)
        {
            multiplier = 1;
            modifiedValue = currentZoom;

            while (currentZoom > 10)
            {
                currentZoom /= 10;
                multiplier *= 10;
            }
            while (currentZoom < 1)
            {
                if (currentZoom < 0)
                {
                    throw new Exception();
                }

                currentZoom *= 10;
                multiplier /= 10;
            }
            return currentZoom;
        }
    }


}
