using System;
using System.Collections.Generic;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Hit testing shared by every backend's Text: <c>TextExtensions.GetCharacterIndexAtPosition</c>
/// (MonoGame, KNI, FNA, raylib), <c>TextBoxBase.GetCaretIndexAtPosition</c>, and Skia's
/// <c>Text.GetCharacterIndexAtPosition</c>.
/// </summary>
internal static class TextHitTesting
{
    /// <summary>
    /// Turns a screen point back by the text's absolute rotation about the text's top-left, the
    /// point every renderer rotates text around, so the unrotated hit-test math finds the character
    /// drawn under the point. Unrotated text leaves the point unchanged.
    /// </summary>
    public static void UnrotatePoint(IRenderableIpso text, ref float screenX, ref float screenY)
    {
        float rotation = text.GetAbsoluteRotation();
        if (rotation == 0)
        {
            return;
        }

        float pivotX = text.GetAbsoluteX();
        float pivotY = text.GetAbsoluteY();
        double radians = rotation * System.Math.PI / 180;
        float cos = (float)System.Math.Cos(radians);
        float sin = (float)System.Math.Sin(radians);
        float offsetX = screenX - pivotX;
        float offsetY = screenY - pivotY;

        screenX = pivotX + offsetX * cos - offsetY * sin;
        screenY = pivotY + offsetX * sin + offsetY * cos;
    }

    /// <summary>
    /// Returns the index into the concatenation of <paramref name="lines"/> nearest a point given
    /// relative to the top-left of the first line, before horizontal alignment.
    /// </summary>
    /// <param name="lines">The wrapped lines, as drawn.</param>
    /// <param name="cursorXOffset">The point's x offset from the text's left edge.</param>
    /// <param name="cursorYOffset">The point's y offset from the top of the first line.</param>
    /// <param name="lineHeight">The distance between the tops of consecutive lines.</param>
    /// <param name="getLineXOffset">The horizontal alignment offset of a line.</param>
    /// <param name="getIndexInLine">The index in a line nearest an x offset from the line's start.</param>
    public static int GetIndexInWrappedLines(
        List<string> lines,
        float cursorXOffset,
        float cursorYOffset,
        float lineHeight,
        Func<string, float> getLineXOffset,
        Func<float, string, int> getIndexInLine)
    {
        int index = 0;
        int lineOn = System.Math.Max(0, System.Math.Min((int)(cursorYOffset / lineHeight), lines.Count - 1));

        if (lineOn < lines.Count)
        {
            string lineText = lines[lineOn];
            int indexInThisLine = getIndexInLine(cursorXOffset - getLineXOffset(lineText), lineText);

            // A click past the end of a wrapped line's trailing space puts the caret before the
            // space, so it stays on this line rather than jumping to the start of the next.
            bool isOnLastLine = lineOn == lines.Count - 1;
            if (!isOnLastLine &&
                indexInThisLine == lineText.Length &&
                indexInThisLine > 0 &&
                char.IsWhiteSpace(lineText[indexInThisLine - 1]))
            {
                index = indexInThisLine - 1;
            }
            else
            {
                index = indexInThisLine;
            }
        }

        for (int line = 0; line < lineOn; line++)
        {
            index += lines[line].Length;
        }

        return index;
    }
}
