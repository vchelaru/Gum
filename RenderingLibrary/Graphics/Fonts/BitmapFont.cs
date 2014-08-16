using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using Microsoft.Xna.Framework;
using System.Collections;
using RenderingLibrary.Math;
using ToolsUtilities;

namespace RenderingLibrary.Graphics
{
    public class BitmapFont : IDisposable
    {
        #region Fields

        internal Texture2D[] mTextures;

        BitmapCharacterInfo[] mCharacterInfo;

        int mLineHeightInPixels;

        internal string mFontFile;
        internal string[] mTextureNames = new string[1];

        int mOutlineThickness;

        #endregion

        #region Properties

        public Texture2D Texture
        {
            get { return mTextures[0]; }
            set
            {
                mTextures[0] = value;

                mTextureNames[0] = mTextures[0].Name;
            }
        }

        public Texture2D[] Textures
        {
            get { return mTextures; }
        }

        public string FontFile
        {
            get { return mFontFile; }
        }

        public string TextureName
        {
            get { return mTextureNames[0]; }
        }

        public int LineHeightInPixels
        {
            get { return mLineHeightInPixels; }
        }

        #endregion

        #region Methods

        public BitmapFont(string fontFile, SystemManagers managers)
        {
            string fontContents = FileManager.FromFileText(fontFile);
            mFontFile = FileManager.Standardize(fontFile);

            string[] texturesToLoad = GetSourceTextures(fontContents);

            mTextures = new Texture2D[texturesToLoad.Length];


            string directory = FileManager.GetDirectory(fontFile);

            for (int i = 0; i < mTextures.Length; i++)
            {
                if (FileManager.IsRelative(texturesToLoad[i]))
                {
                    mTextures[i] = LoaderManager.Self.Load(directory + texturesToLoad[i], managers);
                }
                else
                {
                    mTextures[i] = LoaderManager.Self.Load(texturesToLoad[i], managers);
                }
            }

            SetFontPattern(fontContents);
        }

        public BitmapFont(string textureFile, string fontFile, SystemManagers managers)
        {
            mTextures = new Texture2D[1];
            mTextures[0] = LoaderManager.Self.Load(textureFile, managers);

            mTextureNames[0] = mTextures[0].Name;

            //if (FlatRedBall.IO.FileManager.IsRelative(fontFile))
            //    fontFile = FlatRedBall.IO.FileManager.MakeAbsolute(fontFile);

            //FlatRedBall.IO.FileManager.ThrowExceptionIfFileDoesntExist(fontFile);

            SetFontPatternFromFile(fontFile);
        }

        public BitmapFont(Texture2D fontTextureGraphic, string fontPattern)
        {
            // the font could be an extended character set - let's say for Chinese
            // default it to 256, but search for the largest number.
            mTextures = new Texture2D[1];
            mTextures[0] = fontTextureGraphic;

            //mTextureName = mTexture.Name;

            SetFontPattern(fontPattern);
        }


        #region Public Methods

        public void AssignCharacterTextureCoordinates(int asciiNumber, out float tVTop, out float tVBottom,
            out float tULeft, out float tURight)
        {
            BitmapCharacterInfo characterInfo = null;

            if (asciiNumber < mCharacterInfo.Length)
            {
                characterInfo = mCharacterInfo[asciiNumber];
            }
            else
            {
                // Just return the coordinates for the space character
                characterInfo = mCharacterInfo[' '];
            }

            tVTop = characterInfo.TVTop;
            tVBottom = characterInfo.TVBottom;
            tULeft = characterInfo.TULeft;
            tURight = characterInfo.TURight;

        }

        public float DistanceFromTopOfLine(int asciiNumber)
        {
            BitmapCharacterInfo characterInfo = null;

            if (asciiNumber < mCharacterInfo.Length)
            {
                characterInfo = mCharacterInfo[asciiNumber];
            }
            else
            {
                characterInfo = mCharacterInfo[' '];
            }

            return characterInfo.DistanceFromTopOfLine;
        }

        public BitmapCharacterInfo GetCharacterInfo(int asciiNumber)
        {
            if (asciiNumber < mCharacterInfo.Length)
            {
                return mCharacterInfo[asciiNumber];
            }
            else
            {
                return mCharacterInfo[' '];
            }
        }

        public BitmapCharacterInfo GetCharacterInfo(char character)
        {
            int asciiNumber = (int)character;
            return GetCharacterInfo(asciiNumber);
        }

        public float GetCharacterHeight(int asciiNumber)
        {
            if (asciiNumber < mCharacterInfo.Length)
            {
                return mCharacterInfo[asciiNumber].ScaleY * 2;
            }
            else
            {
                return mCharacterInfo[' '].ScaleY * 2;
            }
        }

        public float GetCharacterScaleX(int asciiNumber)
        {
            if (asciiNumber < mCharacterInfo.Length)
            {
                return mCharacterInfo[asciiNumber].ScaleX;
            }
            else
            {
                return mCharacterInfo[' '].ScaleX;

            }
        }

        public float GetCharacterSpacing(int asciiNumber)
        {
            if (asciiNumber < mCharacterInfo.Length)
            {
                return mCharacterInfo[asciiNumber].Spacing;
            }
            else
            {
                return mCharacterInfo[' '].Spacing;
            }
        }

        public float GetCharacterXOffset(int asciiNumber)
        {
            if (asciiNumber < mCharacterInfo.Length)
            {
                return mCharacterInfo[asciiNumber].XOffset;
            }
            else
            {
                return mCharacterInfo[' '].XOffset;
            }
        }

        public float GetCharacterWidth(char character)
        {
            return GetCharacterScaleX(character) * 2;
        }

        public float GetCharacterWidth(int asciiNumber)
        {
            return GetCharacterScaleX(asciiNumber) * 2;
        }

        public static string[] GetSourceTextures(string fontPattern)
        {
            List<string> texturesToLoad = new List<string>();

            int currentIndexIntoFile = fontPattern.IndexOf("page id=");

            while (currentIndexIntoFile != -1)
            {
                // Right now we'll assume that the pages come in order and they're sequential
                // If this isn' the case then the logic may need to be modified to support this
                // instead of just returning a string[].
                int page = StringFunctions.GetIntAfter("page id=", fontPattern, currentIndexIntoFile);

                int openingQuotesIndex = fontPattern.IndexOf('"', currentIndexIntoFile);

                int closingQuotes = fontPattern.IndexOf('"', openingQuotesIndex + 1);

                string textureName = fontPattern.Substring(openingQuotesIndex + 1, closingQuotes - openingQuotesIndex - 1);
                texturesToLoad.Add(textureName);

                currentIndexIntoFile = fontPattern.IndexOf("page id=", closingQuotes);
            }
            return texturesToLoad.ToArray();
        }

        public void SetFontPattern(string fontPattern)
        {
            mOutlineThickness = StringFunctions.GetIntAfter(" outline=", fontPattern);


            #region Identify the size of the character array to create

            int sizeOfArray = 256;
            // now loop through the file and look for numbers after "char id="

            // Vic says:  This used to
            // go through the entire file
            // to find the last character index.
            // I think they're ordered by character
            // index, so we can just find the last one
            // and save some time.
            int index = fontPattern.LastIndexOf("char id=");
            if (index != -1)
            {
                int ID = StringFunctions.GetIntAfter("char id=", fontPattern, index);

                sizeOfArray = System.Math.Max(sizeOfArray, ID + 1);
            }

            #endregion



            mCharacterInfo = new BitmapCharacterInfo[sizeOfArray];
            mLineHeightInPixels =
                StringFunctions.GetIntAfter(
                "lineHeight=", fontPattern);

            if (mTextures.Length > 0)
            {

                BitmapCharacterInfo space = FillBitmapCharacterInfo(' ', fontPattern,
                   mTextures[0].Width, mTextures[0].Height, mLineHeightInPixels, 0);

                for (int i = 0; i < sizeOfArray; i++)
                {
                    mCharacterInfo[i] = space;
                }

                index = fontPattern.IndexOf("char id=");
                while (index != -1)
                {

                    int ID = StringFunctions.GetIntAfter("char id=", fontPattern, index);

                    mCharacterInfo[ID] = FillBitmapCharacterInfo(ID, fontPattern, mTextures[0].Width,
                        mTextures[0].Height, mLineHeightInPixels, index);

                    int indexOfID = fontPattern.IndexOf("char id=", index);
                    if (indexOfID != -1)
                    {
                        index = indexOfID + ID.ToString().Length;
                    }
                    else
                        index = -1;
                }

                #region Get Kearning Info

                index = fontPattern.IndexOf("kerning ");

                if (index != -1)
                {

                    index = fontPattern.IndexOf("first=", index);

                    while (index != -1)
                    {
                        int ID = StringFunctions.GetIntAfter("first=", fontPattern, index);
                        int secondCharacter = StringFunctions.GetIntAfter("second=", fontPattern, index);
                        int kearningAmount = StringFunctions.GetIntAfter("amount=", fontPattern, index);

                        mCharacterInfo[ID].SecondLetterKearning.Add(secondCharacter, kearningAmount);

                        index = fontPattern.IndexOf("first=", index + 1);
                    }
                }

                #endregion

            }
            //mCharacterInfo[32].ScaleX = .23f;
        }

        public void SetFontPatternFromFile(string fntFileName)
        {
            // standardize before doing anything else
            fntFileName = FileManager.Standardize(fntFileName);

            mFontFile = fntFileName;
            //System.IO.StreamReader sr = new System.IO.StreamReader(mFontFile);
            string fontPattern = FileManager.FromFileText(mFontFile);
            //sr.Close();

            SetFontPattern(fontPattern);
        }


        public Texture2D RenderToTexture2D(string whatToRender, SystemManagers managers)
        {
            string[] lines = whatToRender.Split('\n');

            return RenderToTexture2D(lines, HorizontalAlignment.Left, managers);
        }

        public Texture2D RenderToTexture2D(string whatToRender, HorizontalAlignment horizontalAlignment, SystemManagers managers)
        {
            string[] lines = whatToRender.Split('\n');

            return RenderToTexture2D(lines, horizontalAlignment, managers);
        }

        // To help out the GC, we're going to just use a Color that's 2048x2048
        static Color[] mColorBuffer = new Color[2048 * 2048];

        public Texture2D RenderToTexture2D(IEnumerable<string> lines, HorizontalAlignment horizontalAlignment, SystemManagers managers)
        {
            bool useImageData = false;
            if (useImageData)
            {
                return RenderToTexture2DUsingImageData(lines, horizontalAlignment, managers);
            }
            else
            {
                return RenderToTexture2DUsingRenderStates(lines, horizontalAlignment, managers);

            }
        }

        private Texture2D RenderToTexture2DUsingRenderStates(IEnumerable<string> lines, HorizontalAlignment horizontalAlignment, SystemManagers managers)
        {
            if (managers == null)
            {
                managers = SystemManagers.Default;
            }

            ////////////////// Early out /////////////////////////
            if (managers.Renderer.GraphicsDevice.GraphicsDeviceStatus != GraphicsDeviceStatus.Normal)
            {
                return null;
            }
            ///////////////// End early out //////////////////////


            RenderTarget2D renderTarget = null;


            using (SpriteBatch spriteBatch = new SpriteBatch(managers.Renderer.GraphicsDevice))
            {

                Point point = new Point();
                int requiredWidth;
                int requiredHeight;
                List<int> widths;
                GetRequiredWithAndHeight(lines, out requiredWidth, out requiredHeight, out widths);


                if (requiredWidth != 0)
                {
                    renderTarget = new RenderTarget2D(managers.Renderer.GraphicsDevice, requiredWidth, requiredHeight);
                    managers.Renderer.GraphicsDevice.SetRenderTarget(renderTarget);

                    managers.Renderer.GraphicsDevice.Clear(Color.Transparent);
                    spriteBatch.Begin();
                    int lineNumber = 0;

                    foreach (string line in lines)
                    {
                        point.X = 0;

                        if (horizontalAlignment == HorizontalAlignment.Right)
                        {
                            point.X = requiredWidth - widths[lineNumber];
                        }
                        else if (horizontalAlignment == HorizontalAlignment.Center)
                        {
                            point.X = (requiredWidth - widths[lineNumber]) / 2;
                        }

                        foreach (char c in line)
                        {
                            BitmapCharacterInfo characterInfo = GetCharacterInfo(c);

                            int sourceLeft = characterInfo.GetPixelLeft(Texture);
                            int sourceTop = characterInfo.GetPixelTop(Texture);
                            int sourceWidth = characterInfo.GetPixelRight(Texture) - sourceLeft;
                            int sourceHeight = characterInfo.GetPixelBottom(Texture) - sourceTop;


                            int distanceFromTop = characterInfo.GetPixelDistanceFromTop(LineHeightInPixels);

                            // There could be some offset for this character
                            int xOffset = characterInfo.GetPixelXOffset(LineHeightInPixels);
                            point.X += xOffset;

                            point.Y = lineNumber * LineHeightInPixels + distanceFromTop;

                            Microsoft.Xna.Framework.Rectangle sourceRectangle = new Microsoft.Xna.Framework.Rectangle(
                                sourceLeft, sourceTop, sourceWidth, sourceHeight);

                            int pageIndex = characterInfo.PageNumber;

                            Rectangle destinationRectangle = new Rectangle(point.X, point.Y, sourceWidth, sourceHeight);

                            spriteBatch.Draw(mTextures[pageIndex], destinationRectangle, sourceRectangle, Color.White);

                            point.X -= xOffset;
                            point.X += characterInfo.GetXAdvanceInPixels(LineHeightInPixels);




                        }

                        point.X = 0;
                        lineNumber++;
                    }
                    spriteBatch.End();
                    managers.Renderer.GraphicsDevice.SetRenderTarget(null);
                }


            }

            return renderTarget;
        }

        public void GetRequiredWithAndHeight(IEnumerable lines, out int requiredWidth, out int requiredHeight)
        {
            List<int> throwaway;
            GetRequiredWithAndHeight(lines, out requiredWidth, out requiredHeight, out throwaway);
        }

        private void GetRequiredWithAndHeight(IEnumerable lines, out int requiredWidth, out int requiredHeight, out List<int> widths)
        {

            requiredWidth = 0;
            requiredHeight = 0;

            widths = new List<int>();

            foreach (string line in lines)
            {
                requiredHeight += LineHeightInPixels;
                int lineWidth = 0;

                lineWidth = MeasureString(line);
                widths.Add(lineWidth);
                requiredWidth = System.Math.Max(lineWidth, requiredWidth);
            }

            const int MaxWidthAndHeight = 2048; // change this later?
            requiredWidth = System.Math.Min(requiredWidth, MaxWidthAndHeight);
            requiredHeight = System.Math.Min(requiredHeight, MaxWidthAndHeight);
        }

        private Texture2D RenderToTexture2DUsingImageData(IEnumerable lines, HorizontalAlignment horizontalAlignment, SystemManagers managers)
        {
            ImageData[] imageDatas = new ImageData[this.mTextures.Length];

            for (int i = 0; i < imageDatas.Length; i++)
            {
                // Only use the existing buffer on one-page fonts
                var bufferToUse = mColorBuffer;
                if (i > 0)
                {
                    bufferToUse = null;
                }
                imageDatas[i] = ImageData.FromTexture2D(this.mTextures[i], managers, bufferToUse);
            }

            Point point = new Point();

            int maxWidthSoFar = 0;
            int requiredWidth = 0;
            int requiredHeight = 0;

            List<int> widths = new List<int>();

            foreach (string line in lines)
            {
                requiredHeight += LineHeightInPixels;
                requiredWidth = 0;

                requiredWidth = MeasureString(line);
                widths.Add(requiredWidth);
                maxWidthSoFar = System.Math.Max(requiredWidth, maxWidthSoFar);
            }

            const int MaxWidthAndHeight = 2048; // change this later?
            maxWidthSoFar = System.Math.Min(maxWidthSoFar, MaxWidthAndHeight);
            requiredHeight = System.Math.Min(requiredHeight, MaxWidthAndHeight);



            ImageData imageData = null;

            if (maxWidthSoFar != 0)
            {
                imageData = new ImageData(maxWidthSoFar, requiredHeight, managers);

                int lineNumber = 0;

                foreach (string line in lines)
                {
                    point.X = 0;

                    if (horizontalAlignment == HorizontalAlignment.Right)
                    {
                        point.X = maxWidthSoFar - widths[lineNumber];
                    }
                    else if (horizontalAlignment == HorizontalAlignment.Center)
                    {
                        point.X = (maxWidthSoFar - widths[lineNumber]) / 2;
                    }

                    foreach (char c in line)
                    {

                        BitmapCharacterInfo characterInfo = GetCharacterInfo(c);

                        int sourceLeft = characterInfo.GetPixelLeft(Texture);
                        int sourceTop = characterInfo.GetPixelTop(Texture);
                        int sourceWidth = characterInfo.GetPixelRight(Texture) - sourceLeft;
                        int sourceHeight = characterInfo.GetPixelBottom(Texture) - sourceTop;

                        int distanceFromTop = characterInfo.GetPixelDistanceFromTop(LineHeightInPixels);

                        // There could be some offset for this character
                        int xOffset = characterInfo.GetPixelXOffset(LineHeightInPixels);
                        point.X += xOffset;

                        point.Y = lineNumber * LineHeightInPixels + distanceFromTop;

                        Microsoft.Xna.Framework.Rectangle sourceRectangle = new Microsoft.Xna.Framework.Rectangle(
                            sourceLeft, sourceTop, sourceWidth, sourceHeight);

                        int pageIndex = characterInfo.PageNumber;

                        imageData.Blit(imageDatas[pageIndex], sourceRectangle, point);

                        point.X -= xOffset;
                        point.X += characterInfo.GetXAdvanceInPixels(LineHeightInPixels);

                    }
                    point.X = 0;
                    lineNumber++;
                }
            }


            if (imageData != null)
            {
                // We don't want
                // to generate mipmaps
                // because text is usually
                // rendered pixel-perfect.

                const bool generateMipmaps = false;


                return imageData.ToTexture2D(generateMipmaps);
            }
            else
            {
                return null;
            }
        }

        public int MeasureString(string line)
        {
            int toReturn = 0;
            for (int i = 0; i < line.Length; i++)
            {
                char character = line[i];
                BitmapCharacterInfo characterInfo = GetCharacterInfo(character);

                if (characterInfo != null)
                {
                    bool isLast = i == line.Length - 1;

                    if (isLast)
                    {
                        toReturn += characterInfo.GetPixelWidth(Texture) + characterInfo.GetPixelXOffset(LineHeightInPixels);
                    }
                    else
                    {
                        toReturn += characterInfo.GetXAdvanceInPixels(LineHeightInPixels);
                    }
                }
            }
            return toReturn;
        }

        #endregion

        #region Private Methods

        private BitmapCharacterInfo FillBitmapCharacterInfo(int characterID, string fontString, int textureWidth,
            int textureHeight, int lineHeightInPixels, int startingIndex)
        {
            BitmapCharacterInfo characterInfoToReturn = new BitmapCharacterInfo();

            int indexOfID = fontString.IndexOf("char id=" + characterID, startingIndex);

            if (indexOfID != -1)
            {
                characterInfoToReturn.TULeft =
                    StringFunctions.GetIntAfter("x=", fontString, indexOfID) / (float)textureWidth;
                characterInfoToReturn.TVTop =
                    StringFunctions.GetIntAfter("y=", fontString, indexOfID) / (float)textureHeight;
                characterInfoToReturn.TURight = characterInfoToReturn.TULeft +
                    StringFunctions.GetIntAfter("width=", fontString, indexOfID) / (float)textureWidth;
                characterInfoToReturn.TVBottom = characterInfoToReturn.TVTop +
                    StringFunctions.GetIntAfter("height=", fontString, indexOfID) / (float)textureHeight;

                characterInfoToReturn.DistanceFromTopOfLine = // 1 sclY means 2 height
                    2 * StringFunctions.GetIntAfter("yoffset=", fontString, indexOfID) / (float)lineHeightInPixels;

                characterInfoToReturn.ScaleX = StringFunctions.GetIntAfter("width=", fontString, indexOfID) /
                    (float)lineHeightInPixels;

                characterInfoToReturn.ScaleY = StringFunctions.GetIntAfter("height=", fontString, indexOfID) /
                    (float)lineHeightInPixels;

                characterInfoToReturn.Spacing = 2 * StringFunctions.GetIntAfter("xadvance=", fontString, indexOfID) /
                    (float)lineHeightInPixels;

                characterInfoToReturn.XOffset = 2 * StringFunctions.GetIntAfter("xoffset=", fontString, indexOfID) /
                    (float)lineHeightInPixels;

                characterInfoToReturn.PageNumber = StringFunctions.GetIntAfter("page=", fontString, indexOfID);



                //              characterInfoToReturn.Spacing = 25 * StringFunctions.GetIntAfter("xadvance=", fontString, indexOfID) /
                //                (float)(textureWidth);


            }

            return characterInfoToReturn;
        }

        #endregion

        #endregion


        public void Dispose()
        {
            // Do nothing, the loader will handle disposing the texture.
        }
    }
}
