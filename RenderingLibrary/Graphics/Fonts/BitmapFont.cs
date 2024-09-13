﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using System.Collections;
using Microsoft.Xna.Framework;
using RenderingLibrary.Math;
using RenderingLibrary.Math.Geometry;
using ToolsUtilities;
using BlendState = Gum.BlendState;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Point = System.Drawing.Point;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

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

        private AtlasedTexture mAtlasedTexture;
        private LineRectangle mCharRect;
        #endregion

        #region Properties

        public AtlasedTexture AtlasedTexture
        {
            get { return mAtlasedTexture; }
        }

        public Texture2D Texture
        {
            get { return mTextures.Length > 0 ? mTextures[0] : null; }
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

        public int BaselineY
        {
            get; set;
        }

        public int DescenderHeight => LineHeightInPixels - BaselineY;

        public BitmapCharacterInfo[] Characters => mCharacterInfo;

        #endregion

        #region Methods

        public BitmapFont(string fontFile, SystemManagers managers)
        {
            string fontContents = FileManager.FromFileText(fontFile);
            mFontFile = FileManager.Standardize(fontFile, preserveCase:true);


            ReloadTextures(fontFile, fontContents);

            SetFontPattern(fontContents);
        }


        private void ReloadTextures(string fontFile, string fontContents)
        {
            var unqualifiedTextureNames = GetSourceTextures(fontContents);


            mTextures = new Texture2D[unqualifiedTextureNames.Length];
            mTextureNames = new string[unqualifiedTextureNames.Length];

            string directory = FileManager.GetDirectory(fontFile);
            for (int i = 0; i < mTextures.Length; i++)
            {
                // fnt files treat ./ as relative, but FRB Android treats ./ as
                // absolute. Since the value comes directly from .fnt, we want to 
                // consider ./ as relative instead of whatever FRB thinks is relative:
                //if (FileManager.IsRelative(texturesToLoad[i]))
                bool isRelative = unqualifiedTextureNames[i].StartsWith("./") || FileManager.IsRelative(unqualifiedTextureNames[i]);

                if (isRelative)
                {
                    if (FileManager.IsRelative(directory))
                    {
                        mTextureNames[i] = FileManager.RelativeDirectory + directory + unqualifiedTextureNames[i];
                    }
                    else
                    {
                        mTextureNames[i] = directory + unqualifiedTextureNames[i];
                    }
                }
                else
                {
                    mTextureNames[i] = unqualifiedTextureNames[i];
                }
            }

            ReAssignTextures();
        }

        /// <summary>
        /// Loops through all internally-stored texture names and reloads the textures.
        /// Note, this does not clear any internal caches, so if these textures are cached,
        /// the cache will be used.
        /// </summary>
        public void ReAssignTextures()
        {
            for (int i = 0; i < mTextures.Length; i++)
            {
                AtlasedTexture atlasedTexture = CheckForLoadedAtlasTexture(mTextureNames[i]);
                if (atlasedTexture != null)
                {
                    mAtlasedTexture = atlasedTexture;
                    mTextures[i] = mAtlasedTexture.Texture;
                }
                else
                {
                    // Don't rely on FileExists because mTextureNames may be aliased.
                    // If aliased, the internal loader may redirect. Let it do its job:
                    //if (ToolsUtilities.FileManager.FileExists(mTextureNames[i]))
                    mTextures[i] = global::RenderingLibrary.Content.LoaderManager.Self.LoadContent<Texture2D>(mTextureNames[i]);
                }
            }
        }

        public BitmapFont(string textureFile, string fontFile, SystemManagers managers)
        {
            mTextures = new Texture2D[1];

            mTextureNames = new string[] { textureFile };

            var atlasedTexture = CheckForLoadedAtlasTexture(FileManager.GetDirectory(fontFile) + textureFile);
            if (atlasedTexture != null)
            {
                mAtlasedTexture = atlasedTexture;
                mTextures[0] = mAtlasedTexture.Texture;
            }
            else
            {
                mTextures[0] = global::RenderingLibrary.Content.LoaderManager.Self.LoadContent<Texture2D>(textureFile);
            }

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
            mTextureNames = new string[1];
            mTextureNames[0] = mTextures[0]?.Name;

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

            if(fontPattern?.StartsWith("<?xml version=\"1.0\"?>") == true)
            {
                throw new Exception("Cannot load a font file that is in XML format. Please convert it to Text format.");
            }

            while (currentIndexIntoFile != -1)
            {
                // Right now we'll assume that the pages come in order and they're sequential
                // If this isn' the case then the logic may need to be modified to support this
                // instead of just returning a string[].
                //int page = StringFunctions.GetIntAfter("page id=", fontPattern, currentIndexIntoFile);

                int openingQuotesIndex = fontPattern.IndexOf('"', currentIndexIntoFile);

                int closingQuotesIndex = fontPattern.IndexOf('"', openingQuotesIndex + 1);

                string textureName = fontPattern.Substring(openingQuotesIndex + 1, closingQuotesIndex - openingQuotesIndex - 1);
                texturesToLoad.Add(textureName);

                currentIndexIntoFile = fontPattern.IndexOf("page id=", closingQuotesIndex);
            }
            return texturesToLoad.ToArray();
        }
        
        

        public void SetFontPattern(string fontPattern)
        {
            var parsedData = new ParsedFontFile(fontPattern.AsSpan());

            var charArraySize = (parsedData.Chars.LastOrDefault()?.Id + 1) ?? 0;
            mCharacterInfo = new BitmapCharacterInfo[charArraySize];
            mLineHeightInPixels = parsedData.Common.LineHeight;
            BaselineY = parsedData.Common.Base;

            if (mTextures.Length > 0 && mTextures[0] != null)
            {
                //ToDo: Atlas support  **************************************************************
                var spaceCharInfo = parsedData.Chars.First(x => x.Id == ' ');
                var space = FillBitmapCharacterInfo(spaceCharInfo, mTextures[0].Width, mTextures[0].Height,
                    mLineHeightInPixels);
                
                for (int i = 0; i < charArraySize; i++)
                {
                    mCharacterInfo[i] = space;
                }

                // Make the tab character be equivalent to 4 spaces:
                mCharacterInfo['t'].ScaleX = space.ScaleX * 4;
                mCharacterInfo['t'].Spacing = space.Spacing * 4;

                foreach (var charInfo in parsedData.Chars)
                {
                    mCharacterInfo[charInfo.Id] = FillBitmapCharacterInfo(charInfo, mTextures[0].Width,
                        mTextures[0].Height, mLineHeightInPixels);
                }

                foreach (var kerning in parsedData.Kernings)
                {
                    var character = mCharacterInfo[kerning.First];
                    if (!character.SecondLetterKearning.ContainsKey(kerning.Second))
                    {
                        character.SecondLetterKearning.Add(kerning.Second, kerning.Amount);
                    }
                }
            }
        }

        public void SetFontPatternFromFile(string fntFileName)
        {
            // standardize before doing anything else
            fntFileName = FileManager.Standardize(fntFileName, preserveCase:true);

            mFontFile = fntFileName;
            //System.IO.StreamReader sr = new System.IO.StreamReader(mFontFile);
            string fontPattern = FileManager.FromFileText(mFontFile);
            //sr.Close();

            SetFontPattern(fontPattern);
        }


        public Texture2D RenderToTexture2D(string whatToRender, SystemManagers managers, object objectRequestingRender)
        {
            var lines = whatToRender.Split('\n').ToList();

            return RenderToTexture2D(lines, HorizontalAlignment.Left, managers, null, objectRequestingRender, null);
        }

        public Texture2D RenderToTexture2D(string whatToRender, HorizontalAlignment horizontalAlignment, SystemManagers managers, object objectRequestingRender)
        {
            var lines = whatToRender.Split('\n').ToList();

            return RenderToTexture2D(lines, horizontalAlignment, managers, null, objectRequestingRender);
        }

        // To help out the GC, we're going to just use a Color that's 2048x2048
        static Microsoft.Xna.Framework.Color[] mColorBuffer = new Microsoft.Xna.Framework.Color[2048 * 2048];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="horizontalAlignment"></param>
        /// <param name="managers"></param>
        /// <param name="toReplace"></param>
        /// <param name="objectRequestingRender"></param>
        /// <param name="numberOfLettersToRender">The maximum number of characters to render.</param>
        /// <returns></returns>
        public Texture2D RenderToTexture2D(List<string> lines, HorizontalAlignment horizontalAlignment,
            SystemManagers managers, Texture2D toReplace, object objectRequestingRender,
            int? numberOfLettersToRender = null, float lineHeightMultiplier = 1)
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
            if (numberOfLettersToRender == 0)
            {
                return null;
            }
            ///////////////// End early out //////////////////////

            RenderTarget2D renderTarget = null;

            int requiredWidth;
            int requiredHeight;
            List<int> widths = new List<int>();
            GetRequiredWidthAndHeight(lines, out requiredWidth, out requiredHeight, widths);

            if (requiredWidth != 0)
            {
#if DEBUG
                foreach (var texture in this.Textures)
                {
                    if (texture.IsDisposed)
                    {
                        string message =
                            $"The font:\n{this.FontFile}\nis disposed";
                        throw new InvalidOperationException(message);
                    }
                }
#endif
                var oldViewport = managers.Renderer.GraphicsDevice.Viewport;
                if (toReplace != null && requiredWidth == toReplace.Width && requiredHeight == toReplace.Height)
                {
                    renderTarget = toReplace as RenderTarget2D;
                }
                else
                {
                    renderTarget = new RenderTarget2D(managers.Renderer.GraphicsDevice, requiredWidth, requiredHeight);
                }
                // render target has to be set before setting the viewport
                managers.Renderer.GraphicsDevice.SetRenderTarget(renderTarget);

                var viewportToSet = new Viewport(0, 0, requiredWidth, requiredHeight);
                try
                {
                    managers.Renderer.GraphicsDevice.Viewport = viewportToSet;
                }
                catch (Exception exception)
                {
                    throw new Exception("Error setting graphics device when rendering bitmap font. used values:\n" +
                        $"requiredWidth:{requiredWidth}\nrequiredHeight:{requiredHeight}", exception);
                }


                var spriteRenderer = managers.Renderer.SpriteRenderer;
                managers.Renderer.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
                spriteRenderer.Begin();

                DrawTextLines(lines, horizontalAlignment, objectRequestingRender, requiredWidth, widths,
                    spriteRenderer, Color.White, numberOfLettersToRender: numberOfLettersToRender, lineHeightMultiplier: lineHeightMultiplier);

                spriteRenderer.End();

                managers.Renderer.GraphicsDevice.SetRenderTarget(null);
                managers.Renderer.GraphicsDevice.Viewport = oldViewport;

            }

            return renderTarget;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="horizontalAlignment"></param>
        /// <param name="objectRequestingChange"></param>
        /// <param name="requiredWidth"></param>
        /// <param name="widths"></param>
        /// <param name="spriteRenderer"></param>
        /// <param name="color"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="rotation"></param>
        /// <param name="scaleX"></param>
        /// <param name="scaleY"></param>
        /// <param name="numberOfLettersToRender"></param>
        /// <param name="overrideTextRenderingPositionMode"></param>
        /// <param name="lineHeightMultiplier"></param>
        /// <returns>The rectangle of the drawn text. This will return the same value regardless of alignment.</returns>
        public FloatRectangle DrawTextLines(List<string> lines, HorizontalAlignment horizontalAlignment,
            object objectRequestingChange, int requiredWidth, List<int> widths,
            SpriteRenderer spriteRenderer,
            Color color,
            float xOffset = 0, float yOffset = 0, float rotation = 0, float scaleX = 1, float scaleY = 1,
            int? numberOfLettersToRender = null, TextRenderingPositionMode? overrideTextRenderingPositionMode = null, float lineHeightMultiplier = 1)
        {
            ///////////Early Out////////////////
            if (numberOfLettersToRender == 0)
            {
                return default(FloatRectangle);
            }
            /////////End Early Out//////////////

            FloatRectangle toReturn = new FloatRectangle();

            var currentLetterOrigin = new Vector2();

            int lineNumber = 0;

            int xOffsetAsInt = MathFunctions.RoundToInt(xOffset);
            int yOffsetAsInt = MathFunctions.RoundToInt(yOffset);

            // Custom effect already does premultiply alpha on the shader so we skip that in this case
            if (!Renderer.UseCustomEffectRendering && Renderer.NormalBlendState == BlendState.AlphaBlend)
            {
                // this is premultiplied, so premulitply the color value
                float multiple = color.A / 255.0f;

                color = Color.FromArgb(color.A,
                    (byte)(color.R * multiple),
                    (byte)(color.G * multiple),
                    (byte)(color.B * multiple));
            }

            var rotationRadians = MathHelper.ToRadians(rotation);

            Vector2 xAxis = Vector2.UnitX;
            Vector2 yAxis = Vector2.UnitY;

            if (rotation != 0)
            {
                xAxis.X = (float)System.Math.Cos(-rotationRadians);
                xAxis.Y = (float)System.Math.Sin(-rotationRadians);

                yAxis.X = (float)System.Math.Cos(-rotationRadians + MathHelper.PiOver2);
                yAxis.Y = (float)System.Math.Sin(-rotationRadians + MathHelper.PiOver2);
            }

            int numberOfLettersRendered = 0;


            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                // scoot over to leave room for the outline
                currentLetterOrigin.X = mOutlineThickness;

                float offsetFromAlignment = 0;

                if (horizontalAlignment == HorizontalAlignment.Right)
                {
                    offsetFromAlignment = scaleX * (requiredWidth - widths[lineNumber]);
                }
                else if (horizontalAlignment == HorizontalAlignment.Center)
                {
                    offsetFromAlignment = scaleX * (requiredWidth - widths[lineNumber]) / 2;
                }

                currentLetterOrigin.X += offsetFromAlignment;

                var effectiveTextRenderingMode = overrideTextRenderingPositionMode ??
                    Text.TextRenderingPositionMode;

                if (line.Length > 0)
                {
                    FloatRectangle destRect;
                    int pageIndex;

                    float lineOffset = 0;

                    for(int charIndex = 0; charIndex < line.Length; charIndex++)
                    {
                        char c = line[charIndex];
                        var sourceRect =
                            GetCharacterRect(c, lineNumber, ref currentLetterOrigin, out destRect, out pageIndex, scaleX, lineHeightMultiplier: lineHeightMultiplier);

                        if(charIndex == 0)
                        {
                            var firstLetterDestinationX = destRect.X;
                            var firstLetterDestinationXInt = MathFunctions.RoundToInt(firstLetterDestinationX);
                            lineOffset = 0f;

                            if (effectiveTextRenderingMode == TextRenderingPositionMode.SnapToPixel)
                            {
                                lineOffset = firstLetterDestinationX - firstLetterDestinationXInt;
                            }

                        }

                        var unrotatedX = destRect.X + xOffset;
                        var unrotatedY = destRect.Y + yOffset;
                        toReturn.X = System.Math.Min(toReturn.X, unrotatedX - offsetFromAlignment) ;
                        toReturn.Y = System.Math.Min(toReturn.Y, unrotatedY);

                        // why are we max'ing the point.X's and width? This makes center and right-alignment text render incorrectly
                        // when this method is called multiple times due to styling:
                        //toReturn.Width = System.Math.Max(toReturn.Width, point.X);
                        // Update - because point.X is the current point - which marks the location of the next
                        // character.
                        // After calling GetCharacterRect, the currentLetterOrigin.X is updated to be the next letter's origin.
                        toReturn.Width = System.Math.Max(toReturn.Width, currentLetterOrigin.X - offsetFromAlignment);

                        toReturn.Height = System.Math.Max(toReturn.Height, currentLetterOrigin.Y);


                        var finalPosition = destRect.X * xAxis + destRect.Y * yAxis;

                        finalPosition.X += xOffset;
                        finalPosition.Y += yOffset;


                        if (effectiveTextRenderingMode == TextRenderingPositionMode.FreeFloating ||
                            // If rotated, need free floating positions since sprite positions will likely not line up with pixels
                            rotation != 0 ||
                            // If scaled up/down, don't use free floating
                            scaleX != 1)
                        {
                            var scale = new Vector2(scaleX, scaleY);
                            spriteRenderer.Draw(mTextures[pageIndex], finalPosition, sourceRect, color, -rotationRadians, Vector2.Zero, scale, SpriteEffects.None, 0, this);
                        }
                        else
                        {
                            // position:
                            destRect.X += xOffsetAsInt + lineOffset;
                            destRect.Y += yOffsetAsInt;

                            var position = new Vector2(destRect.X, destRect.Y);

                            spriteRenderer.Draw(mTextures[pageIndex], position, sourceRect, color, 0, Vector2.Zero, new Vector2(scaleX, scaleY), SpriteEffects.None, 0, this);
                        }

                        numberOfLettersRendered++;

                        if (numberOfLettersToRender <= numberOfLettersRendered)
                        {
                            break;
                        }

                    }

                }
                currentLetterOrigin.X = 0;
                lineNumber++;

                if (numberOfLettersToRender <= numberOfLettersRendered)
                {
                    break;
                }
            }
            return toReturn;
        }

        /// <summary>
        /// Used for rendering directly to screen with an atlased texture.
        /// </summary>
        public void RenderAtlasedTextureToScreen(List<string> lines, HorizontalAlignment horizontalAlignment,
            float textureToRenderHeight, Color color, float rotation, float fontScale, SystemManagers managers, SpriteRenderer spriteRenderer,
            object objectRequestingChange)
        {
            var textObject = (Text)objectRequestingChange;
            var point = new Vector2();
            int requiredWidth;
            int requiredHeight;
            List<int> widths = new List<int>();
            GetRequiredWidthAndHeight(lines, out requiredWidth, out requiredHeight, widths);

            int lineNumber = 0;

            if (mCharRect == null) mCharRect = new LineRectangle(managers);

            var yoffset = 0f;
            if (textObject.VerticalAlignment == Graphics.VerticalAlignment.Center)
            {
                yoffset = (textObject.EffectiveHeight - textureToRenderHeight) / 2.0f;
            }
            else if (textObject.VerticalAlignment == Graphics.VerticalAlignment.Bottom)
            {
                yoffset = textObject.EffectiveHeight - textureToRenderHeight * fontScale;
            }

            foreach (string line in lines)
            {
                // scoot over to leave room for the outline
                point.X = mOutlineThickness;

                if (horizontalAlignment == HorizontalAlignment.Right)
                {
                    point.X = (int)(textObject.Width - widths[lineNumber] * fontScale);
                }
                else if (horizontalAlignment == HorizontalAlignment.Center)
                {
                    point.X = (int)(textObject.Width - widths[lineNumber] * fontScale) / 2;
                }

                foreach (char c in line)
                {
                    FloatRectangle destRect;
                    int pageIndex;
                    var sourceRect = GetCharacterRect(c, lineNumber, ref point, out destRect, out pageIndex, textObject.FontScale);

                    var origin = new Point((int)textObject.X, (int)(textObject.Y + yoffset));
                    var rotate = (float)-(textObject.Rotation * System.Math.PI / 180f);

                    var rotatingPoint = new Point(origin.X + (int)destRect.X, origin.Y + (int)destRect.Y);
                    MathFunctions.RotatePointAroundPoint(origin, ref rotatingPoint, rotate);

                    mCharRect.X = rotatingPoint.X;
                    mCharRect.Y = rotatingPoint.Y;
                    mCharRect.Width = destRect.Width;
                    mCharRect.Height = destRect.Height;

                    if (textObject.Parent != null)
                    {
                        mCharRect.X += textObject.Parent.GetAbsoluteX();
                        mCharRect.Y += textObject.Parent.GetAbsoluteY();
                    }

                    Sprite.Render(managers, spriteRenderer, mCharRect, mTextures[0], color, sourceRect, false, rotation,
                        treat0AsFullDimensions: false, objectCausingRendering: objectRequestingChange);
                }
                point.X = 0;
                lineNumber++;
            }
        }

        public float EffectiveLineHeight(float fontScale = 1, float lineHeightMultiplier = 1) => mLineHeightInPixels * lineHeightMultiplier * fontScale;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="lineNumber"></param>
        /// <param name="currentCharacterDrawPosition">When passed in, this is the point used to draw the current character. This is used to set the destinationRectangle. This value is modified, increasing the position by XAdvance.</param>
        /// <param name="destinationRectangle"></param>
        /// <param name="pageIndex"></param>
        /// <param name="fontScale"></param>
        /// <param name="lineHeightMultiplier"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Rectangle GetCharacterRect(char c, int lineNumber, ref Vector2 currentCharacterDrawPosition, out FloatRectangle destinationRectangle,
            out int pageIndex, float fontScale = 1, float lineHeightMultiplier = 1)
        {
            if(Texture == null)
            {
                throw new InvalidOperationException("The bitmap font has a null texture so it cannot return a character rectangle");
            }
            BitmapCharacterInfo characterInfo = GetCharacterInfo(c);

            int sourceLeft = characterInfo.GetPixelLeft(Texture);
            int sourceTop = characterInfo.GetPixelTop(Texture);
            int sourceWidth = characterInfo.GetPixelRight(Texture) - sourceLeft;
            int sourceHeight = characterInfo.GetPixelBottom(Texture) - sourceTop;
            var sourceRectangle = new Rectangle(sourceLeft, sourceTop, sourceWidth, sourceHeight);

            pageIndex = characterInfo.PageNumber;

            int distanceFromTop = characterInfo.GetPixelDistanceFromTop(mLineHeightInPixels);

            // There could be some offset for this character
            int xOffset = characterInfo.GetPixelXOffset(mLineHeightInPixels);

            // Shift the point by the xOffset, which affects destination (drawing) but does not affect the advance of the position for the next letter
            currentCharacterDrawPosition.X += xOffset * fontScale;
            currentCharacterDrawPosition.Y = lineNumber * EffectiveLineHeight(fontScale, lineHeightMultiplier) + distanceFromTop * fontScale;
            destinationRectangle = new FloatRectangle(currentCharacterDrawPosition.X, currentCharacterDrawPosition.Y, sourceWidth * fontScale, sourceHeight * fontScale);

            // Shift it back.
            currentCharacterDrawPosition.X -= xOffset * fontScale;
            currentCharacterDrawPosition.X += characterInfo.GetXAdvanceInPixels(mLineHeightInPixels) * fontScale;

            return sourceRectangle;
        }

        public void GetRequiredWidthAndHeight(IEnumerable<string> lines, out int requiredWidth, out int requiredHeight)
        {
            GetRequiredWidthAndHeight(lines, out requiredWidth, out requiredHeight, null);
        }

        // This sucks, but if we pass an IEnumerable, it allocates memory like crazy. Duplicate code to handle List to reduce alloc
        //public void GetRequiredWidthAndHeight(IEnumerable<string> lines, out int requiredWidth, out int requiredHeight, List<int> widths)
        /// <summary>
        /// Returns the width and height required to render the argument line of text.
        /// </summary>
        /// <param name="lines">The lines of text, where each entry is one line of text.</param>
        /// <param name="requiredWidth">The required width returned by this method.</param>
        /// <param name="requiredHeight">The required height returned by this method.</param>
        /// <param name="widths">The widths of the individual lines.</param>
        public void GetRequiredWidthAndHeight(List<string> lines, out int requiredWidth, out int requiredHeight, List<int> widths)
        {

            requiredWidth = 0;
            requiredHeight = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                requiredHeight += LineHeightInPixels;
                int lineWidth = 0;

                lineWidth = MeasureString(line);
                if (widths != null)
                {
                    widths.Add(lineWidth);
                }
                requiredWidth = System.Math.Max(lineWidth, requiredWidth);
            }

            const int MaxWidthAndHeight = 4096; // change this later?
            requiredWidth = System.Math.Min(requiredWidth, MaxWidthAndHeight);
            requiredHeight = System.Math.Min(requiredHeight, MaxWidthAndHeight);
            if (requiredWidth != 0 && mOutlineThickness != 0)
            {
                requiredWidth += mOutlineThickness * 2;
            }
        }

        public void GetRequiredWidthAndHeight(IEnumerable<string> lines, out int requiredWidth, out int requiredHeight, List<int> widths)
        {

            requiredWidth = 0;
            requiredHeight = 0;

            foreach (string line in lines)
            {
                requiredHeight += LineHeightInPixels;
                int lineWidth = 0;

                lineWidth = MeasureString(line);
                if (widths != null)
                {
                    widths.Add(lineWidth);
                }
                requiredWidth = System.Math.Max(lineWidth, requiredWidth);
            }

            const int MaxWidthAndHeight = 4096; // change this later?
            requiredWidth = System.Math.Min(requiredWidth, MaxWidthAndHeight);
            requiredHeight = System.Math.Min(requiredHeight, MaxWidthAndHeight);
            if (requiredWidth != 0 && mOutlineThickness != 0)
            {
                requiredWidth += mOutlineThickness * 2;
            }
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

                        Rectangle sourceRectangle = new Rectangle(sourceLeft, sourceTop, sourceWidth, sourceHeight);

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

        /// <summary>
        /// Returns the number of pixels (horizontally) required to render the argument string.
        /// </summary>
        /// <param name="line">The line of text.</param>
        /// <returns>The number of pixels needed to render this text horizontally.</returns>
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

        private AtlasedTexture CheckForLoadedAtlasTexture(string filename)
        {
            if (ToolsUtilities.FileManager.IsRelative(filename))
            {
                filename = ToolsUtilities.FileManager.RelativeDirectory + filename;

                filename = ToolsUtilities.FileManager.RemoveDotDotSlash(filename);
            }

            // see if an atlas exists:
            var atlasedTexture = global::RenderingLibrary.Content.LoaderManager.Self.TryLoadContent<AtlasedTexture>(filename);

            return atlasedTexture;
        }

        private BitmapCharacterInfo FillBitmapCharacterInfo(
            FontFileCharLine charInfo, 
            int textureWidth, 
            int textureHeight,
            int lineHeightInPixels)
        {
            var tuLeft = charInfo.X / (float)textureWidth;
            var tvTop = charInfo.Y / (float)textureHeight;

            return new BitmapCharacterInfo
            {
                TULeft = tuLeft,
                TVTop = tvTop,
                TURight = tuLeft + charInfo.Width / (float)textureWidth,
                TVBottom = tvTop + charInfo.Height / (float)textureHeight,
                DistanceFromTopOfLine = 2 * charInfo.YOffset / (float)lineHeightInPixels,
                ScaleX = charInfo.Width / (float)lineHeightInPixels,
                ScaleY = charInfo.Height / (float)lineHeightInPixels,
                Spacing = 2 * charInfo.XAdvance / (float)lineHeightInPixels,
                XOffset = 2 * charInfo.XOffset / (float)lineHeightInPixels,
                PageNumber = charInfo.Page,
            };
        }

        #endregion

        #endregion

        public void Dispose()
        {
            // Do nothing, the loader will handle disposing the texture.
        }

        public override string ToString()
        {
            return mFontFile;
        }

        private ref struct FontFileParseWordResult
        {
            public readonly ReadOnlySpan<char> Word;
            public readonly ReadOnlySpan<char> Remaining;
            public bool IsEndOfLine;

            private FontFileParseWordResult(ReadOnlySpan<char> word, ReadOnlySpan<char> remaining, bool isEndOfLine)
            {
                Word = word;
                Remaining = remaining;
                IsEndOfLine = isEndOfLine;
            }

            public static FontFileParseWordResult NextWord(ReadOnlySpan<char> text)
            {
                if (text.IsEmpty) return new FontFileParseWordResult();

                var wordStartIndex = (int?)null;
                var isInQuotes = false;
                var currentIndex = 0;
                foreach (var character in text)
                {
                    if (char.IsWhiteSpace(character) || character == '=')
                    {
                        if (!isInQuotes && wordStartIndex != null)
                        {
                            // Hit the end of a word
                            var length = currentIndex - wordStartIndex.Value;
                            var word = text.Slice(wordStartIndex.Value, length);
                            var remaining = text.Slice(currentIndex);
                            var isEndOfLine = character == '\r' || character == '\n';

                            return new FontFileParseWordResult(word, remaining, isEndOfLine);
                        }
                    }
                    else
                    {
                        if (character == '"')
                        {
                            if (!isInQuotes)
                            {
                                // starting quote
                                if (wordStartIndex != null)
                                {
                                    // Not sure that this is valid
                                    var message = "Found quote in the middle of an existing font file word.";
                                    throw new InvalidOperationException(message);
                                }
                                
                                isInQuotes = true;
                                wordStartIndex = currentIndex;
                            }
                            else
                            {
                                // Ending quote
                                if (wordStartIndex == null)
                                {
                                    var message = "Got an ending quote without a word start";
                                    throw new InvalidOperationException(message);
                                }
                                
                                // Cut off the quotes
                                var length = currentIndex - wordStartIndex.Value - 1;
                                var word = text.Slice(wordStartIndex.Value + 1, length);
                                var remaining = text.Slice(currentIndex + 1);

                                return new FontFileParseWordResult(word, remaining, false);
                            }
                        }

                        wordStartIndex ??= currentIndex;
                    }

                    currentIndex++;
                }
                
                // If we got here we hit the end of the file
                if (wordStartIndex == null)
                {
                    return new FontFileParseWordResult();
                }

                var finalWord = text.Slice(wordStartIndex.Value);
                return new FontFileParseWordResult(
                    finalWord,
                    ReadOnlySpan<char>.Empty,
                    true);
            }
        }

        private class ParsedFontFile
        {
            public FontFileInfoLine Info { get; }
            public FontFileCommonLine Common { get; }
            public List<FontFileCharLine> Chars { get; } = new(300);
            public List<FontFileKerningLine> Kernings { get; } = new(300);

            public ParsedFontFile(ReadOnlySpan<char> contents)
            {
                while (true)
                {
                    var result = FontFileParseWordResult.NextWord(contents);
                    contents = result.Remaining;
                    if (result.Word.IsEmpty)
                    {
                        return;
                    }

                    if (result.Word.Equals("info", StringComparison.OrdinalIgnoreCase))
                    {
                        Info = new FontFileInfoLine(ref contents);
                    }
                    else if (result.Word.Equals("common", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Common = new FontFileCommonLine(ref contents);
                    }
                    else if (result.Word.Equals("char", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Chars.Add(new FontFileCharLine(ref contents));
                    }
                    else if (result.Word.Equals("kerning", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Kernings.Add(new FontFileKerningLine(ref contents));
                    }
                }
            }
        }

        private class FontFileInfoLine
        {
            public int Outline { get; set; }

            public FontFileInfoLine(ref ReadOnlySpan<char> contents)
            {
                ParseLine(ref contents, this, new Dictionary<string, Action<FontFileInfoLine, int>>
                {
                    {"outline", (line, num) => line.Outline = num},
                });
            }
        }

        private class FontFileCommonLine
        {
            public int LineHeight { get; set; }
            public int Base { get; set; }

            public FontFileCommonLine(ref ReadOnlySpan<char> contents)
            {
                ParseLine(ref contents, this, new Dictionary<string, Action<FontFileCommonLine, int>>
                {
                    {"lineHeight", (line, num) => line.LineHeight = num},
                    {"base", (line, num) => line.Base = num},
                });
            }
        }

        private class FontFileCharLine
        {
            public int Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int XOffset { get; set; }
            public int YOffset { get; set; }
            public int XAdvance { get; set; }
            public int Page { get; set; }

            public FontFileCharLine(ref ReadOnlySpan<char> contents)
            {
                ParseLine(ref contents, this, new Dictionary<string, Action<FontFileCharLine, int>>
                {
                    {"id", (line, num) => line.Id = num},
                    {"x", (line, num) => line.X = num},
                    {"y", (line, num) => line.Y = num},
                    {"width", (line, num) => line.Width = num},
                    {"height", (line, num) => line.Height = num},
                    {"xoffset", (line, num) => line.XOffset = num},
                    {"yoffset", (line, num) => line.YOffset = num},
                    {"xadvance", (line, num) => line.XAdvance = num},
                    {"page", (line, num) => line.Page = num},
                });
            }
        }

        private class FontFileKerningLine
        {
            public int First { get; set; }
            public int Second { get; set; }
            public int Amount { get; set; }

            public FontFileKerningLine(ref ReadOnlySpan<char> contents)
            {
                ParseLine(ref contents, this, new Dictionary<string, Action<FontFileKerningLine, int>>
                {
                    {"first", (line, num) => line.First = num},
                    {"second", (line, num) => line.Second = num},
                    {"amount", (line, num) => line.Amount = num},
                });
            }
        }

        private static void ParseLine<T>(
            ref ReadOnlySpan<char> contents, 
            T item, 
            Dictionary<string, Action<T, int>> modifiers)
        {
            ReadOnlySpan<char> currentAttribute = ReadOnlySpan<char>.Empty;
            while (true)
            {
                var result = FontFileParseWordResult.NextWord(contents);
                contents = result.Remaining;

                if (!result.Word.IsEmpty)
                {
                    if (currentAttribute.IsEmpty)
                    {
                        currentAttribute = result.Word;
                    }
                    else
                    {
                        foreach (var modifier in modifiers)
                        {
                            if (currentAttribute.Equals(modifier.Key, StringComparison.OrdinalIgnoreCase))
                            {
                                modifier.Value(item, Convert.ToInt32(result.Word.ToString()));
                                break;
                            }
                        }
                        currentAttribute = ReadOnlySpan<char>.Empty;
                    }
                }

                if (result.Word.IsEmpty || result.IsEndOfLine)
                {
                    return;
                }
            }
        }
    }
}
