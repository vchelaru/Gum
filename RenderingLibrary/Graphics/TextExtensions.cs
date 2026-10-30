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
    /// <summary>
    /// Returns the index into <see cref="IWrappedText.WrappedText"/> (its lines concatenated) of the
    /// character nearest the screen point, measured in drawn units so it follows FontScale.
    /// </summary>
    /// <param name="textInstance">The text to hit test.</param>
    /// <param name="screenX">The screen x position, usually obtained by Cursor.XRespectingGumZoomAndBounds().</param>
    /// <param name="screenY">The screen y position, usually obtained by Cursor.YRespectingGumZoomAndBounds().</param>
    public static int GetCharacterIndexAtPosition(this Text textInstance, float screenX, float screenY)
    {
        TextHitTesting.UnrotatePoint(textInstance, ref screenX, ref screenY);

        float fontScale = GetMeasurementFontScale(textInstance);
        var lineHeight = textInstance.LineHeightInPixels * fontScale * textInstance.LineHeightMultiplier;
        var topOfText = textInstance.GetAbsoluteTop() + TextHitTesting.GetVerticalAlignmentOffset(
            textInstance.VerticalAlignment, textInstance.EffectiveHeight, textInstance.WrappedTextHeight);

        return TextHitTesting.GetIndexInWrappedLines(
            textInstance.WrappedText,
            screenX - textInstance.GetAbsoluteLeft(),
            screenY - topOfText,
            lineHeight,
            lineText => GetLineXOffsetForHorizontalAlignment(textInstance, lineText, fontScale),
            (cursorOffset, lineText) => GetIndex(textInstance, cursorOffset, lineText, fontScale));
    }

    // The scale from MeasureString/GetCharacterAdvance units (the font's own pixels) to drawn units.
    // IWrappedText.WordWrapFontScale is that scale: the effective FontScale, except while oversampling pins
    // a separate measurement font, where layout (and so the drawn box) ignores the compensation scale.
    private static float GetMeasurementFontScale(Text textInstance) =>
        ((IWrappedText)textInstance).WordWrapFontScale;

    static float GetLineXOffsetForHorizontalAlignment(Text textInstance, string stringToMeasure, float fontScale)
    {
        if (textInstance.HorizontalAlignment == global::RenderingLibrary.Graphics.HorizontalAlignment.Left)
            return 0;

        float measuredLineWidth = textInstance.MeasureString(stringToMeasure) * fontScale;
        float textComponentWidth = textInstance.EffectiveWidth;
        float gapBetweenTextAndEdge = textComponentWidth - measuredLineWidth;
        if (textInstance.HorizontalAlignment == global::RenderingLibrary.Graphics.HorizontalAlignment.Center)
            gapBetweenTextAndEdge /= 2.0f;
        return gapBetweenTextAndEdge;
    }

    private static int GetIndex(Text textInstance, float cursorOffset, string? textToUse, float fontScale)
    {
#if RAYLIB
        // Raylib has no per-character advance, so a character's advance is the growth of the measured prefix.
        return TextHitTesting.GetIndexInLine(
            textToUse,
            cursorOffset,
            (line, start, length) =>
                textInstance.MeasureString(line.Substring(0, start + length)) - textInstance.MeasureString(line.Substring(0, start)),
            fontScale);
#else
        return TextHitTesting.GetIndexInLine(textToUse, cursorOffset, ((IFormsText)textInstance).GetCharacterAdvance, fontScale);
#endif
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