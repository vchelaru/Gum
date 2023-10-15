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

        public float ScaleX;
        public float ScaleY;
        public float Spacing;
        public float XOffset;
        public float DistanceFromTopOfLine;

        public int GetPixelLeft(Texture2D texture)
        {
            return MathFunctions.RoundToInt(TULeft * texture.Width);
        }
        public int GetPixelTop(Texture2D texture)
        {
            return MathFunctions.RoundToInt(TVTop * texture.Height);
        }
        public int GetPixelRight(Texture2D texture)
        {
            return MathFunctions.RoundToInt(TURight * texture.Width);
        }
        public int GetPixelBottom(Texture2D texture)
        {
            return MathFunctions.RoundToInt(TVBottom * texture.Height);
        }
        public int GetPixelWidth(Texture2D texture)
        {
            return GetPixelRight(texture) - GetPixelLeft(texture);
        }
        public int GetPixelHeight(Texture2D texture)
        {
            return GetPixelBottom(texture) - GetPixelTop(texture);
        }

        public int GetPixelXOffset(int lineHeightInPixels)
        {
            return MathFunctions.RoundToInt(lineHeightInPixels * XOffset / 2.0f);
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
