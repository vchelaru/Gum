using Gum.Graphics;
using RenderingLibrary.Content;
using RenderingLibrary.Math;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using ToolsUtilitiesStandard.Helpers;
using BlendState = Gum.BlendState;
using Color = System.Drawing.Color;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Matrix = System.Numerics.Matrix4x4;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

#if RAYLIB
using Gum.Renderables;

#else
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Math.Geometry;

#endif



namespace RenderingLibrary.Graphics;

public static class TextExtensions
{

    public static int GetCharacterIndexAtPosition(this Text textInstance, float screenX, float screenY)
    {
        TextHitTesting.UnrotatePoint(textInstance, ref screenX, ref screenY);

        var lineHeight = textInstance.LineHeightInPixels * textInstance.LineHeightMultiplier;
        var topOfText = textInstance.GetAbsoluteTop() + TextHitTesting.GetVerticalAlignmentOffset(
            textInstance.VerticalAlignment, textInstance.EffectiveHeight, textInstance.WrappedTextHeight);

        return TextHitTesting.GetIndexInWrappedLines(
            textInstance.WrappedText,
            screenX - textInstance.GetAbsoluteLeft(),
            screenY - topOfText,
            lineHeight,
            lineText => GetLineXOffsetForHorizontalAlignment(textInstance, lineText),
            (cursorOffset, lineText) => GetIndex(textInstance, cursorOffset, lineText));
    }

    static float GetLineXOffsetForHorizontalAlignment(Text textInstance, string stringToMeasure)
    {
        if (textInstance.HorizontalAlignment == global::RenderingLibrary.Graphics.HorizontalAlignment.Left)
            return 0;

        float measuredLineWidth = textInstance.MeasureString(stringToMeasure);
        float textComponentWidth = textInstance.EffectiveWidth;
        float gapBetweenTextAndEdge = textComponentWidth - measuredLineWidth;
        if (textInstance.HorizontalAlignment == global::RenderingLibrary.Graphics.HorizontalAlignment.Center)
            gapBetweenTextAndEdge /= 2.0f;
        return gapBetweenTextAndEdge;
    }

    private static int GetIndex(Text textInstance, float cursorOffset, string? textToUse)
    {
        var index = textToUse?.Length ?? 0;
        float distanceMeasuredSoFar = 0;

#if RAYLIB
        if (textToUse == null)
        {
            return index;
        }

        for (int i = 0; i < textToUse.Length; i++)
        {
            // Is there a faster way to do this?
            distanceMeasuredSoFar = textInstance.MeasureString(textToUse.Substring(0, i + 1));

            // This should find which side of the character you're closest to, but for now it's good enough...
            if (distanceMeasuredSoFar > cursorOffset)
            {
                var distanceBefore = textInstance.MeasureString(textToUse.Substring(0, i));
                var advance = distanceMeasuredSoFar - distanceBefore;
                var halfwayPoint = distanceMeasuredSoFar - (advance / 2.0f);
                if (halfwayPoint > cursorOffset)
                {
                    index = i;
                }
                else
                {
                    index = i + 1;
                }
                break;
            }
        }
#else

        var bitmapFont = textInstance.BitmapFont;

        for (int i = 0; i < (textToUse?.Length ?? 0); i++)
        {
            // The loop only runs when textToUse has characters.
            char character = textToUse![i];
            // No font yet measures as 0, like Text.MeasureString.
            global::RenderingLibrary.Graphics.BitmapCharacterInfo? characterInfo = bitmapFont?.GetCharacterInfo(character);

            int advance = 0;

            if (characterInfo != null)
            {
                //advance = characterInfo.GetXAdvanceInPixels(coreTextObject.BitmapFont.LineHeightInPixels);
                advance = characterInfo.XAdvance;
            }

            distanceMeasuredSoFar += advance;

            // This should find which side of the character you're closest to, but for now it's good enough...
            if (distanceMeasuredSoFar > cursorOffset)
            {
                var halfwayPoint = distanceMeasuredSoFar - (advance / 2.0f);
                if (halfwayPoint > cursorOffset)
                {
                    index = i;
                }
                else
                {
                    index = i + 1;
                }
                break;
            }
        }
#endif
        return index;
    }

    public static void GetLineNumber(this Text textInstance, int absoluteCharacterIndex, out int lineNumber, out int absoluteStartOfLine, out int relativeIndexOnLine)
    {
        lineNumber = 0;
        relativeIndexOnLine = absoluteCharacterIndex;
        absoluteStartOfLine = 0;

        for (int i = 0; i < textInstance.WrappedText.Count; i++)
        {
            var currentLine = textInstance.WrappedText[i];
            var lineLength = currentLine.Length;
            if (relativeIndexOnLine <= lineLength)
            {
                var shouldShowFirstOfNextLine =
                    // If we're at the very end of the line,
                    relativeIndexOnLine == lineLength &&
                    // the last character is whitespace,
                    currentLine.Length > 0 &&
                    // we have another line
                    lineNumber < textInstance.WrappedText.Count - 1 &&
                    // and the first letter on the next line is not whitespace
                    textInstance.WrappedText[lineNumber + 1].Length > 0 && !char.IsWhiteSpace(textInstance.WrappedText[lineNumber + 1][0]);

                if (!shouldShowFirstOfNextLine && lineLength > 0 && relativeIndexOnLine == lineLength && currentLine[lineLength - 1] == '\n')
                {
                    shouldShowFirstOfNextLine = true;
                }

                if (shouldShowFirstOfNextLine)
                {
                    relativeIndexOnLine -= lineLength;
                    absoluteStartOfLine += lineLength;
                    lineNumber++;
                }
                break;
            }
            else
            {
                absoluteStartOfLine += lineLength;
                relativeIndexOnLine -= lineLength;
                lineNumber++;
            }
        }

        lineNumber = System.Math.Min(lineNumber, textInstance.WrappedText.Count - 1);
    }
}