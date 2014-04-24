using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RenderingLibrary.Graphics
{
    public class ImageData
    {
        #region Fields

        private int width;
        private int height;
        
        // if SurfaceFormat.Color, use these
        private Color[] mData;
        static Color[] mStaticData = new Color[128 * 128];

        // if SurfaceFormat.DXT3, use these
        private byte[] mByteData;

        SystemManagers mManagers;

        #endregion

        #region Properties

        public int Width
        {
            get
            {
                return width;
            }
        }

        public int Height
        {
            get
            {
                return height;
            }
        }

        public Color[] Data
        {
            get
            {
                return mData;
            }
        }

        #endregion

        #region Methods

        #region Constructor

        public ImageData(int width, int height, SystemManagers managers)
            : this(width, height, new Color[width * height], managers)
        {

        }

        public ImageData(int width, int height, Color[] data, SystemManagers managers)
        {
            this.width = width;
            this.height = height;
            this.mData = data;
            mManagers = managers;
        }


        public ImageData(int width, int height, byte[] data, SystemManagers managers)
        {
            this.width = width;
            this.height = height;
            this.mByteData = data;
            mManagers = managers;
        }

        #endregion

        #region Public Static Methods



        public static ImageData FromTexture2D(Texture2D texture2D, SystemManagers managers, Color[] colorBuffer = null)
        {
            ImageData imageData = null;
        

            switch (texture2D.Format)
            {
                case SurfaceFormat.Color:
                    {
                        if (colorBuffer == null)
                        {
                            colorBuffer = new Color[texture2D.Width * texture2D.Height];
                        }

                        lock (colorBuffer)
                        {


                            texture2D.GetData<Color>(colorBuffer, 0, texture2D.Width * texture2D.Height);

                            imageData = new ImageData(
                                texture2D.Width, texture2D.Height, colorBuffer, managers);
                        }
                    }
                    break;
                case SurfaceFormat.Dxt3:

                    Byte[] byteData = new byte[texture2D.Width * texture2D.Height];
                    texture2D.GetData<byte>(byteData);

                    imageData = new ImageData(texture2D.Width, texture2D.Height, byteData, managers);

                    break;

                default:
                    throw new NotImplementedException("The format " + texture2D.Format + " isn't supported.");

                    //break;
            }
            return imageData;
        }




        #endregion

        #region Public Methods

        public void Blit(Texture2D source, Rectangle sourceRectangle, Point destination)
        {
            ImageData sourceAsImageData = ImageData.FromTexture2D(source, mManagers);

            Blit(sourceAsImageData, sourceRectangle, destination);
        }


        public void Blit(ImageData source, Rectangle sourceRectangle, Point destination)
        {
            for (int y = 0; y < sourceRectangle.Height; y++)
            {
                for (int x = 0; x < sourceRectangle.Width; x++)
                {
                    int sourceX = x + sourceRectangle.X;
                    int sourceY = y + sourceRectangle.Y;

                    int destinationX = x + destination.X;
                    int destinationY = y + destination.Y;

                    if (destinationX < this.Width && destinationY < this.Height)
                    {
                        this.SetPixel(destinationX, destinationY, source.GetPixelColor(sourceX, sourceY));
                    }
                }
            }
        }


        public void CopyFrom(Texture2D texture2D)
        {
            texture2D.GetData<Color>(mData, 0, texture2D.Width * texture2D.Height);
        }

        public void CopyTo(ImageData destination, int xOffset, int yOffset)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    destination.mData[(y + yOffset) * destination.width + (x + xOffset)] = this.mData[y * width + x];
                }
            }
        }

        public void ExpandIfNecessary(int desiredWidth, int desiredHeight)
        {
            if (desiredWidth * desiredHeight > mData.Length)
            {
                SetDataDimensions(desiredWidth, desiredHeight);
            }
        }

        public void Fill(Color fillColor)
        {
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = fillColor;
            }
        }

        public void Fill(Color fillColor, Rectangle rectangle)
        {
            for (int y = 0; y < rectangle.Height; y++)
            {
                for (int x = 0; x < rectangle.Width; x++)
                {
                    SetPixel(rectangle.X + x, rectangle.Y + y, fillColor);
                }
            }
        }

        public void FlipHorizontal()
        {
            int halfWidth = width / 2;
            int widthMinusOne = width - 1;

            // This currently assumes Color.  Update to use DXT.
            for (int x = 0; x < halfWidth; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color temp = mData[x + y * Width];

                    mData[x + y * width] = mData[(widthMinusOne - x) + y * width];
                    mData[widthMinusOne - x + y * width] = temp;

                }
            }


        }

        public void FlipVertical()
        {
            int halfHeight = height / 2;
            int heightMinusOne = height - 1;

            // This currently assumes Color.  Update to use DXT.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < halfHeight; y++)
                {
                    Color temp = mData[x + y * Width];

                    mData[x + y * width] = mData[x + (heightMinusOne - y) * width];
                    mData[x + (heightMinusOne - y) * width] = temp;

                }
            }
        }

        public Color GetPixelColor(int x, int y)
        {
            return this.Data[x + y * Width];
        }

        public void GetXAndY(int absoluteIndex, out int x, out int y)
        {
            y = absoluteIndex / this.Width;
            x = absoluteIndex % width;
        }

        public void RemoveColumn(int columnToRemove)
        {
            Color[] newData = new Color[width * height];

            int destinationY = 0;
            int destinationX = 0;

            int newWidth = width - 1;

            for (int y = 0; y < height; y++)
            {
                destinationX = 0;
                for (int x = 0; x < width; x++)
                {
                    if (x == columnToRemove)
                    {
                        continue;
                    }
                    newData[destinationY * newWidth + destinationX] = mData[y * width + x];

                    destinationX++;
                }

                destinationY++;
            }
            width = newWidth;

            mData = newData;
        }

        public void RemoveColumns(IList<int> columnsToRemove)
        {
            Color[] newData = new Color[width * height];

            int destinationY = 0;
            int destinationX = 0;

            int newWidth = width - columnsToRemove.Count;

            for (int y = 0; y < height; y++)
            {
                destinationX = 0;
                for (int x = 0; x < width; x++)
                {
                    if (columnsToRemove.Contains(x))
                    {
                        continue;
                    }
                    newData[destinationY * newWidth + destinationX] = mData[y * width + x];

                    destinationX++;
                }

                destinationY++;
            }
            width = newWidth;

            mData = newData;
        }

        #region XML Docs
        /// <summary>
        /// Removes the index row from the contained data.  Row 0 is the top of the texture.
        /// </summary>
        /// <param name="rowToRemove">The index of the row to remove.  Index 0 is the top row.</param>
        #endregion
        public void RemoveRow(int rowToRemove)
        {
            Color[] newData = new Color[width * height];

            int destinationY = 0;
            int destinationX = 0;

            for (int y = 0; y < height; y++)
            {
                if (y == rowToRemove)
                {
                    continue;
                }

                destinationX = 0;
                for (int x = 0; x < width; x++)
                {
                    newData[destinationY * width + destinationX] = mData[y * width + x];

                    destinationX++;
                }

                destinationY++;
            }
            height--;

            mData = newData;
        }

        public void RemoveRows(IList<int> rowsToRemove)
        {
            Color[] newData = new Color[width * height];

            int destinationY = 0;
            int destinationX = 0;

            for (int y = 0; y < height; y++)
            {
                if (rowsToRemove.Contains(y))
                {
                    continue;
                }

                destinationX = 0;
                for (int x = 0; x < width; x++)
                {
                    newData[destinationY * width + destinationX] = mData[y * width + x];

                    destinationX++;
                }

                destinationY++;
            }
            height -= rowsToRemove.Count;

            mData = newData;
        }

        public void Replace(Color oldColor, Color newColor)
        {
            for (int i = 0; i < mData.Length; i++)
            {
                if (mData[i] == oldColor)
                {
                    mData[i] = newColor;
                }
            }
        }

        public void RotateClockwise90()
        {
            Color[] newData = new Color[width * height];

            int newWidth = height;
            int newHeight = width;

            int xToPullFrom;
            int yToPullFrom;

            for (int x = 0; x < newWidth; x++)
            {
                for (int y = 0; y < newHeight; y++)
                {
                    xToPullFrom = newHeight - 1 - y;
                    yToPullFrom = x;

                    newData[y * newWidth + x] =
                        mData[yToPullFrom * width + xToPullFrom];
                }
            }

            width = newWidth;
            height = newHeight;
            mData = newData;
        }

        public void SetDataDimensions(int desiredWidth, int desiredHeight)
        {
            mData = new Color[desiredHeight * desiredWidth];
            width = desiredWidth;
            height = desiredHeight;
        }

        public void SetPixel(int x, int y, Color color)
        {
            Data[y * width + x] = color;
        }

        public Texture2D ToTexture2D()
        {
            return ToTexture2D(true);
        }

        public Texture2D ToTexture2D(bool generateMipmaps)
        {
            int textureWidth = Width;
            int textureHeight = Height;
            
            if (textureWidth != Width || textureHeight != Height)
            {

                float ratioX = width / (float)textureWidth;
                float ratioY = height / (float)textureHeight;

                if (textureHeight * textureWidth > mStaticData.Length)
                {
                    mStaticData = new Color[textureHeight * textureWidth];
                }
                for (int y = 0; y < textureHeight; y++)
                {
                    for (int x = 0; x < textureWidth; x++)
                    {
                        //try
                        {
                            int sourcePixelX = (int)(x * ratioX);
                            int sourcePixelY = (int)(y * ratioY);

                            // temporary for testing
                            mStaticData[y * textureWidth + x] =
                                mData[((int)(sourcePixelY * Width) + (int)(sourcePixelX))];
                        }
                        //catch
                        //{
                        //    int m = 3;
                        //}
                    }
                }

                return ToTexture2D(mStaticData, textureWidth, textureHeight, generateMipmaps, mManagers);
            }
            else
            {
                return ToTexture2D(mData, textureWidth, textureHeight, generateMipmaps, mManagers);

            }
        }

        public void ToTexture2D(Texture2D textureToFill)
        {
            lock (Renderer.Self.GraphicsDevice)
            {
                textureToFill.SetData<Color>(this.mData, 0, textureToFill.Width * textureToFill.Height);
            }
        }

        #endregion

        #region Internal Methods


        internal void MakePremultiplied()
        {
            MakePremultiplied(mData.Length);
        }

        internal void MakePremultiplied(int count)
        {
            for (int i = count - 1; i > -1; i--)
            {
                Color color = mData[i];

                float multiplier = color.A / 255.0f;

                color.R = (byte)(color.R * multiplier);
                color.B = (byte)(color.B * multiplier);
                color.G = (byte)(color.G * multiplier);

                mData[i] = color;
            }
        }

        internal static Texture2D ToTexture2D(Color[] pixelData, int textureWidth, int textureHeight, SystemManagers managers)
        {

            return ToTexture2D(pixelData, textureWidth, textureHeight, true, managers);
        }

        internal static Texture2D ToTexture2D(Color[] pixelData, int textureWidth, int textureHeight, bool generateMipmaps, SystemManagers managers)
        {
            
            Texture2D texture = null;

            Renderer renderer;
            if (managers == null)
            {
                renderer = Renderer.Self;
            }
            else
            {
                renderer = managers.Renderer;
            }

            lock (renderer.GraphicsDevice)
            {
                const int startIndex = 0;


                texture = new Texture2D(renderer.GraphicsDevice,
                    textureWidth, textureHeight, generateMipmaps, SurfaceFormat.Color);



                texture.SetData<Color>(pixelData, startIndex, textureWidth * textureHeight - startIndex);


            }
            return texture;
        }

        #endregion

        #endregion
        

    }
}
