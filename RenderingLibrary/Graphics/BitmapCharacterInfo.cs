using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Math;

namespace RenderingLibrary.Graphics
{
    public class BitmapCharacterInfo
    {
        #region Fields

        public float TULeft;
        public float TVTop;
        public float TURight;
        public float TVBottom;

        public int PixelLeft;
        public int PixelTop;
        public int PixelRight;
        public int PixelBottom;

        public float ScaleX;
        public float ScaleY;

        public float Spacing;

        /// <summary>
        /// The number of pixels to advance after this character as the starting point for the next character. This value
        /// ignores XOffsetInPixels, and it is not affected by the texture coordinate values.
        /// </summary>
        public int XAdvance;

        [Obsolete("This value is confusingly named and it does not use the expected pixel unit. use XoffsetInPixels instead", error:true)]
        public float XOffset;

        /// <summary>
        /// The offset of the character when drawn from the current X position. This value lets
        /// a character get drawn further to the right or left without changing the X position of the
        /// next character.
        /// </summary>
        public int XOffsetInPixels;
        public float DistanceFromTopOfLine;

        [Obsolete("Use PixelLeft")]
        public int GetPixelLeft(Texture2D texture)
        {
            return MathFunctions.RoundToInt(TULeft * texture.Width);
        }

        [Obsolete("Use PixelTop")]
        public int GetPixelTop(Texture2D texture)
        {
            return MathFunctions.RoundToInt(TVTop * texture.Height);
        }

        [Obsolete("Use PixelRight")]
        public int GetPixelRight(Texture2D texture)
        {
            return MathFunctions.RoundToInt(TURight * texture?.Width ?? 0);
        }

        [Obsolete("Use PixelBottom")]
        public int GetPixelBottom(Texture2D texture)
        {
            return MathFunctions.RoundToInt(TVBottom * texture.Height);
        }

        [Obsolete("Use PixelRight - PixelLeft")]
        public int GetPixelWidth(Texture2D texture)
        {
            return GetPixelRight(texture) - GetPixelLeft(texture);
        }

        [Obsolete("Use PixelBottom - PixelTop")]
        public int GetPixelHeight(Texture2D texture)
        {
            return GetPixelBottom(texture) - GetPixelTop(texture);
        }



        public int GetPixelDistanceFromTop(int lineHeightInPixels)
        {
            return MathFunctions.RoundToInt(lineHeightInPixels * DistanceFromTopOfLine / 2.0f);
        }

        public int GetXAdvanceInPixels(int lineHeightInPixels)
        {
            return MathFunctions.RoundToInt(lineHeightInPixels * this.Spacing / 2.0f);
        }

        public int PageNumber;


        public Dictionary<int, int> SecondLetterKearning = new Dictionary<int, int>();

        #endregion

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("TULeft: ").Append(TULeft).Append("\n");
            builder.Append("TVTop: ").Append(TVTop).Append("\n");
            builder.Append("TURight: ").Append(TURight).Append("\n");
            builder.Append("TVBottom: ").Append(TVBottom).Append("\n");

            builder.Append("ScaleX: ").Append(ScaleX).Append("\n");
            builder.Append("ScaleY: ").Append(ScaleY).Append("\n");
            builder.Append("Spacing: ").Append(Spacing).Append("\n");
            builder.Append("DistanceFromTopOfLine: ").Append(DistanceFromTopOfLine).Append("\n");

            return builder.ToString();
        }
    }
}
