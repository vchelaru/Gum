using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Math;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ToolsUtilities;
using BlendState = Gum.BlendState;
using Color = System.Drawing.Color;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Vector2 = System.Numerics.Vector2;

namespace RenderingLibrary.Graphics;

public enum HorizontalMeasurementStyle
{
    Full = 1,
    TrimRight = 2,
    // eventually trim left and trim both too
}


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
    private ParsedFontFile _ParsedFontFile;
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

    [Obsolete("Use the version that does not take SystemManagers")]
    public BitmapFont(string fontFile, SystemManagers managers) : this(fontFile)
    {
    }

    public BitmapFont(string fontFile)
    {
        Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        string fontContents = ContainsAscii(fontFile) ? FileManager.FromFileText(fontFile, encoding) : FileManager.FromFileText(fontFile);

        mFontFile = FileManager.Standardize(fontFile, preserveCase:true);

        _ParsedFontFile = new ParsedFontFile(fontContents);
        ReloadTextures(fontFile, fontContents);

        SetFontPattern();
    }

    private bool ContainsAscii(string s)
    {
        //TODO : Here is a simple check for ASCII characters.
        return s.All(ch => ch >= 32 && ch <= 126);
    }

    private void ReloadTextures(string fontFile, string fontContents)
    {
        var unqualifiedTextureNames = _ParsedFontFile.GetPagesAsArrayOfStrings;


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

        _ParsedFontFile = new ParsedFontFile(fontPattern);

        SetFontPattern();
    }



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
        if(mCharacterInfo.Length == 0)
        {
            return null;
        }
        else if (asciiNumber < mCharacterInfo.Length)
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

    public float GetCharacterWidth(char character)
    {
        return GetCharacterScaleX(character) * 2;
    }

    public float GetCharacterWidth(int asciiNumber)
    {
        return GetCharacterScaleX(asciiNumber) * 2;
    }

    public void SetFontPattern(int? forcedTextureWidth = null, int? forcedTextureHeight = null)
    {

        var parsedData = _ParsedFontFile;

        this.mOutlineThickness = parsedData.Info?.Outline ?? 0;

        var charArraySize = (parsedData.Chars.LastOrDefault()?.Id + 1) ?? 0;
        mCharacterInfo = new BitmapCharacterInfo[charArraySize];
        mLineHeightInPixels = parsedData.Common.LineHeight;
        BaselineY = parsedData.Common.Base;

        int textureWidth = 255;
        int textureHeight = 255;

        if(forcedTextureWidth != null && forcedTextureHeight != null)
        {
            textureWidth = forcedTextureWidth.Value;
            textureHeight = forcedTextureHeight.Value;
        }
        else if(mTextures?.Length > 0)
        {

            textureWidth = mTextures[0]?.Width ?? 255;
            textureHeight = mTextures[0]?.Height ?? 255;
        }



        //ToDo: Atlas support  **************************************************************
        var spaceCharInfo = parsedData.Chars.FirstOrDefault(x => x.Id == ' ');

        // Hiero "Extended" does not include the space character.
        // This used to cause a rendering crash. That was fixed but
        // even with it fixed we want to make sure we have a valid space
        // character since it's so common.
        bool wasSpaceCreatedDynamically = false;
        if(spaceCharInfo == null)
        {
            wasSpaceCreatedDynamically = true;

            var fontSize = 18;

            var absFontSize = System.Math.Abs(parsedData.Info.Size);
            if (absFontSize > 0)
            {
                // bmfc uses negative values for fonts
                // that "match char height":
                fontSize = absFontSize;

            }

            // Arial 32 has 9 spacing for 32, so let's try 3
            int spaceSize = fontSize / 3;

            spaceCharInfo = new FontFileCharLine
            {
                Id = (char)' ',
                XAdvance = spaceSize,
                Width = spaceSize
            };
        }

        // Added null check for space since some special fonts might not have a space inside them.
        if (spaceCharInfo != null)
        {
            var space = FillBitmapCharacterInfo(spaceCharInfo, textureWidth, textureHeight,
                mLineHeightInPixels);

            for (int i = 0; i < charArraySize; i++)
            {
                mCharacterInfo[i] = space;
            }

            if (mCharacterInfo.Length > (int)'\t')
            {
                // Make the tab character be equivalent to 4 spaces:
                mCharacterInfo['\t'].ScaleX = space.ScaleX * 4;
                mCharacterInfo['\t'].Spacing = space.Spacing * 4;
                mCharacterInfo['\t'].XAdvance = space.XAdvance * 4;
                mCharacterInfo['\t'].XOffsetInPixels = space.XOffsetInPixels * 4;
            }
            if(mCharacterInfo.Length > (int)'\n')
            {
                mCharacterInfo['\n'].ScaleX = 0;
                mCharacterInfo['\n'].Spacing = 0;
                mCharacterInfo['\n'].TURight = 0;
                mCharacterInfo['\n'].TULeft = 0;
                //mCharacterInfo['\n'].XOffset = 0;
                mCharacterInfo['\n'].XOffsetInPixels = 0;
            }
        }

            
        foreach (var charInfo in parsedData.Chars)
        {
            // TODO: Ask VIC why BMFont generator will create a char id="-1" entry, which crashes this code.
            // This is a temporary fix until he can tell me
            if (charInfo.Id == -1)
            {
                continue;
            }
            mCharacterInfo[charInfo.Id] = FillBitmapCharacterInfo(charInfo, textureWidth,
                textureHeight, mLineHeightInPixels);
        }

        if(wasSpaceCreatedDynamically)
        {
            if(mCharacterInfo.Length > (int)' ')
            {
                mCharacterInfo[' '] = FillBitmapCharacterInfo(spaceCharInfo, textureWidth,
                    textureHeight, mLineHeightInPixels);
            }
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

    public void SetFontPatternFromFile(string fntFileName)
    {
        // standardize before doing anything else
        fntFileName = FileManager.Standardize(fntFileName, preserveCase:true);

        mFontFile = fntFileName;
        //System.IO.StreamReader sr = new System.IO.StreamReader(mFontFile);
        string fontPattern = FileManager.FromFileText(mFontFile);
        //sr.Close();

        _ParsedFontFile = new ParsedFontFile(fontPattern);

        SetFontPattern();
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
        float xOffset = 0, float yOffset = 0, 
        float rotation = 0, float scaleX = 1, float scaleY = 1,
        int? numberOfLettersToRender = null, 
        TextRenderingPositionMode? overrideTextRenderingPositionMode = null, 
        float lineHeightMultiplier = 1,
        bool shiftForOutline = true)
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

        // int is used if pixel perfect
        int xOffsetAsInt = MathFunctions.RoundToInt(xOffset);
        int yOffsetAsInt = MathFunctions.RoundToInt(yOffset);

        // otherwise, we use rounded to the zoom value, to try to get close:
        float xOffsetRoundedToZoom = MathFunctions.RoundFloat(xOffset, scaleX);
        float yOffsetRoundedToZoom = MathFunctions.RoundFloat(yOffset, scaleY);

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

            if(shiftForOutline)
            {
                // scoot over to leave room for the outline
                currentLetterOrigin.X = mOutlineThickness * scaleX;
            }

            float offsetFromAlignment = 0;

#if DEBUG
            if(lineNumber >= widths.Count)
            {
                var message = $"Error trying to draw text with {lines.Count} lines, but only {widths.Count} widths. Lines:\n{GetCombinedLines()}";
                throw new Exception(message);
            }

            string GetCombinedLines()
            {
                return string.Join("\n", lines);
            }
#endif

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



#if DEBUG
                    if(mTextures.Length <= pageIndex)
                    {
                        throw new Exception($"Attempting to render a character {c} in line {line} with font which accesses a character on page {pageIndex}, but only {mTextures} texture page(s) exist");
                    }
#endif

                    var isFreeFloating = effectiveTextRenderingMode == TextRenderingPositionMode.FreeFloating ||
                        // If rotated, need free floating positions since sprite positions will likely not line up with pixels
                        rotation != 0;

                    if(!isFreeFloating)
                    {
                        // If scaled up/down, don't use free floating
                        isFreeFloating = scaleX != 1;
                    }

                    if (isFreeFloating)
                    {

                        var finalPosition = destRect.X * xAxis + destRect.Y * yAxis;

                        finalPosition.X += xOffsetRoundedToZoom;
                        finalPosition.Y += yOffsetRoundedToZoom;


                        var scale = new Vector2(scaleX, scaleY);
                        spriteRenderer.Draw(mTextures[pageIndex], finalPosition, sourceRect, 
                            color, -rotationRadians, Vector2.Zero, scale, SpriteEffects.None, 0, this,
                            dimensionSnapping: DimensionSnapping.DimensionSnapping);
                    }
                    else
                    {
                        // position:
                        destRect.X += xOffsetAsInt + lineOffset;
                        destRect.Y += yOffsetAsInt;

                        var position = new Vector2(destRect.X, destRect.Y);

                        spriteRenderer.Draw(mTextures[pageIndex], position, sourceRect, color, 0, Vector2.Zero, 
                            new Vector2(scaleX, scaleY), SpriteEffects.None, 0, this,
                            dimensionSnapping: DimensionSnapping.DimensionSnapping);
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
        if (Texture == null)
        {
            throw new InvalidOperationException("The bitmap font has a null texture so it cannot return a character rectangle");
        }
        BitmapCharacterInfo characterInfo = GetCharacterInfo(c);

        int sourceLeft = 0;
        int sourceTop = 0;
        int sourceWidth = 0;
        int sourceHeight = 0;

        if(characterInfo != null)
        {
            sourceLeft = characterInfo.PixelLeft;
            sourceTop = characterInfo.PixelTop;
            sourceWidth = characterInfo.PixelRight - sourceLeft;
            sourceHeight = characterInfo.PixelBottom - sourceTop;
        }

        var sourceRectangle = new Rectangle(sourceLeft, sourceTop, sourceWidth, sourceHeight);

        if(characterInfo != null)
        {
            pageIndex = characterInfo.PageNumber;

            int distanceFromTop = characterInfo.GetPixelDistanceFromTop(mLineHeightInPixels);

            // There could be some offset for this character
            int xOffset = 
                characterInfo.XOffsetInPixels;
            //characterInfo.GetPixelXOffset(mLineHeightInPixels);

            // Shift the point by the xOffset, which affects destination (drawing) but does not affect the advance of the position for the next letter
            currentCharacterDrawPosition.X += xOffset * fontScale;
            currentCharacterDrawPosition.Y = GetCharacterTop(lineNumber, distanceFromTop, fontScale, lineHeightMultiplier);
            destinationRectangle = new FloatRectangle(currentCharacterDrawPosition.X, currentCharacterDrawPosition.Y, sourceWidth * fontScale, sourceHeight * fontScale);

            // Shift it back now that we have the destinationRectangle
            currentCharacterDrawPosition.X -= xOffset * fontScale;
            currentCharacterDrawPosition.X += 
                //characterInfo.GetXAdvanceInPixels(mLineHeightInPixels) * fontScale;
                characterInfo.XAdvance * fontScale;
        }
        else
        {
            pageIndex = 0;
            destinationRectangle = new FloatRectangle(currentCharacterDrawPosition.X, currentCharacterDrawPosition.Y, 0, 0);
        }



        return sourceRectangle;
    }

    private float GetCharacterTop(int lineNumber, int distanceFromTop, float fontScale = 1, float lineHeightMultiplier = 1)
    {
        return lineNumber * EffectiveLineHeight(fontScale, lineHeightMultiplier) + distanceFromTop * fontScale;
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

                    int sourceLeft = characterInfo.PixelLeft;
                    int sourceTop = characterInfo.PixelTop;
                    int sourceWidth = characterInfo.PixelRight - sourceLeft;
                    int sourceHeight = characterInfo.PixelBottom - sourceTop;

                    int distanceFromTop = characterInfo.GetPixelDistanceFromTop(LineHeightInPixels);

                    // There could be some offset for this character
                    //int xOffset = characterInfo.GetPixelXOffset(LineHeightInPixels);
                    int xOffset = characterInfo.XOffsetInPixels;
                    point.X += xOffset;

                    point.Y = lineNumber * LineHeightInPixels + distanceFromTop;

                    Rectangle sourceRectangle = new Rectangle(sourceLeft, sourceTop, sourceWidth, sourceHeight);

                    int pageIndex = characterInfo.PageNumber;

                    imageData.Blit(imageDatas[pageIndex], sourceRectangle, point);

                    point.X -= xOffset;
                    //point.X += characterInfo.GetXAdvanceInPixels(LineHeightInPixels);
                    point.X += characterInfo.XAdvance;

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
    public int MeasureString(string line, HorizontalMeasurementStyle horizontalMeasurementStyle = HorizontalMeasurementStyle.TrimRight)
    {
        int toReturn = 0;
        for (int i = 0; i < line.Length; i++)
        {
            char character = line[i];
            BitmapCharacterInfo characterInfo = null;
            try
            {
                characterInfo = GetCharacterInfo(character);
            }
            catch
            {
                // If we can't measure a character due to it missing, we shouldn't throw an exception here.
                // By catching the exception, we allow Gum to work even if it's missing characters
            }

            if (characterInfo != null)
            {
                bool isLast = i == line.Length - 1 ||
                    i == line.Length - 2 && line[i + 1] == '\n';

                // April 14, 2025
                // It looks like the
                // code here is written
                // to respect the texture
                // width of the last character
                // rather than the XAdvance. This
                // might be so that icons sit snug
                // against the end of their line of
                // of text. 
                if ((isLast && horizontalMeasurementStyle == HorizontalMeasurementStyle.TrimRight && 
                    // Texture being null should never happen in real development, but we want to check for this with unit tests:
                    Texture != null) 
                    || (isLast && char.IsWhiteSpace(character)))
                {
                    if (character == '\n')
                    {
                        // do nothing
                    }
                    else
                    {
                        toReturn += (characterInfo.PixelRight - characterInfo.PixelLeft) + characterInfo.XOffsetInPixels;
                    }
                }
                else
                {
                    // ignore the on newlines, otherwise trailing newlines make text wider which is weird:
                    if(character == '\n')
                    {
                        // do nothing
                    }
                    else
                    {
                        toReturn += characterInfo.XAdvance;
                    }
                }
            }
        }
        return toReturn;
    }


    public void MakeNumbersMonospaced()
    {
        var character0 = GetCharacterInfo('0');

        int characterIndex = (int)'1';

        for (int i = characterIndex; i <= (int)'9'; i++)
        {
            var characterInfo = Characters[i];

            var xAdvanceDifference = character0.XAdvance - characterInfo.XAdvance;

            characterInfo.XAdvance = character0.XAdvance;
            characterInfo.XOffsetInPixels += (int)(xAdvanceDifference / 2);
        }


    }


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
            PixelLeft = charInfo.X,
            PixelTop = charInfo.Y,
            PixelRight = charInfo.X + charInfo.Width,
            PixelBottom = charInfo.Y + charInfo.Height,
            DistanceFromTopOfLine = 2 * charInfo.YOffset / (float)lineHeightInPixels,
            ScaleX = charInfo.Width / (float)lineHeightInPixels,
            ScaleY = charInfo.Height / (float)lineHeightInPixels,
            Spacing = 2 * charInfo.XAdvance / (float)lineHeightInPixels,
            XAdvance = charInfo.XAdvance,
            //XOffset = 2 * charInfo.XOffset / (float)lineHeightInPixels,
            XOffsetInPixels = charInfo.XOffset,
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


}
