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
    /// Returns the index into <see cref="Text.WrappedText"/> (its lines concatenated) of the
    /// character nearest the screen point, measured in drawn units so it follows FontScale.
    /// </summary>
    /// <param name="textInstance">The text to hit test.</param>
    /// <param name="screenX">The screen x position, usually obtained by Cursor.XRespectingGumZoomAndBounds().</param>
    /// <param name="screenY">The screen y position, usually obtained by Cursor.YRespectingGumZoomAndBounds().</param>
    public static int GetCharacterIndexAtPosition(this Text textInstance, float screenX, float screenY)
    {
        TextHitTesting.UnrotatePoint(textInstance, ref screenX, ref screenY);

        IFormsText formsText = textInstance;
        var topOfText = textInstance.GetAbsoluteTop() + TextHitTesting.GetVerticalAlignmentOffset(
            textInstance.VerticalAlignment, textInstance.EffectiveHeight, textInstance.WrappedTextHeight);

        // Widths come from the styled overload, so a line with [FontScale]/[FontSize]/[Font] runs is placed
        // and hit tested at the sizes it is drawn at (#8321).
        return TextHitTesting.GetIndexInWrappedLines(
            textInstance.WrappedText,
            screenX - textInstance.GetAbsoluteLeft(),
            screenY - topOfText,
            formsText.GetDrawnLineTop,
            (startOfLine, lineText) => TextHitTesting.GetHorizontalAlignmentOffset(textInstance.HorizontalAlignment, textInstance.EffectiveWidth,
                textInstance.MeasureDrawnString(lineText, startOfLine, HorizontalMeasurementStyle.TrimRight)),
            (cursorOffset, startOfLine, lineText) => GetIndex(textInstance, cursorOffset, startOfLine, lineText));
    }

    private static int GetIndex(Text textInstance, float cursorOffset, int startOfLine, string? textToUse)
    {
#if !RAYLIB
        if (textInstance.InlineVariables.Count == 0)
        {
            return TextHitTesting.GetIndexInLine(textToUse, cursorOffset, textInstance.GetDrawnCharacterAdvance, textInstance.GetDrawnKerning);
        }
#endif
        // A character's advance is the growth of the drawn prefix: raylib has no per-character advance, and
        // a styled line's advance depends on the run each character is in.
        return TextHitTesting.GetIndexInLine(
            textToUse,
            cursorOffset,
            (line, start, length) =>
                textInstance.MeasureDrawnString(line.Substring(0, start + length), startOfLine, HorizontalMeasurementStyle.Full)
                - textInstance.MeasureDrawnString(line.Substring(0, start), startOfLine, HorizontalMeasurementStyle.Full));
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