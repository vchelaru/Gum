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
    /// The distance from the top of a Text's box down to the top of its wrapped lines, as every
    /// renderer draws them: centered text splits the leftover height, bottom-aligned text takes all of it.
    /// </summary>
    public static float GetVerticalAlignmentOffset(VerticalAlignment verticalAlignment, float boxHeight, float wrappedTextHeight)
    {
        switch (verticalAlignment)
        {
            case VerticalAlignment.Center:
                return (boxHeight - wrappedTextHeight) / 2.0f;
            case VerticalAlignment.Bottom:
                return boxHeight - wrappedTextHeight;
            default:
                return 0;
        }
    }

    /// <summary>
    /// True when <paramref name="index"/> is the high half of a surrogate pair (an emoji), which is
    /// one character stored as two chars.
    /// </summary>
    public static bool IsSurrogatePairAt(string text, int index) =>
        index >= 0 && index + 1 < text.Length && char.IsHighSurrogate(text[index]) && char.IsLowSurrogate(text[index + 1]);

    /// <summary>
    /// Returns the index in <paramref name="text"/> nearest <paramref name="cursorOffset"/>, an x offset in
    /// drawn units from the start of the line. A surrogate pair is one character, so the result is never
    /// between its halves.
    /// </summary>
    /// <param name="text">The line.</param>
    /// <param name="cursorOffset">The x offset from the line's start, in drawn units.</param>
    /// <param name="getAdvance">The unscaled advance of the characters from a start index for a length.</param>
    /// <param name="fontScale">The text's effective font scale (<c>IText.FontScale</c>), which converts
    /// unscaled advances to the drawn units <paramref name="cursorOffset"/> is in.</param>
    public static int GetIndexInLine(string? text, float cursorOffset, Func<string, int, int, float> getAdvance, float fontScale)
    {
        text ??= string.Empty;
        int index = text.Length;
        float distanceMeasuredSoFar = 0;

        for (int i = 0; i < text.Length;)
        {
            int characterLength = IsSurrogatePairAt(text, i) ? 2 : 1;
            float advance = getAdvance(text, i, characterLength) * fontScale;

            distanceMeasuredSoFar += advance;

            // This should find which side of the character you're closest to, but for now it's good enough...
            if (distanceMeasuredSoFar > cursorOffset)
            {
                float halfwayPoint = distanceMeasuredSoFar - (advance / 2.0f);
                index = halfwayPoint > cursorOffset ? i : i + characterLength;
                break;
            }

            i += characterLength;
        }

        return index;
    }

    /// <inheritdoc cref="GetIndexInLine(string?, float, Func{string, int, int, float}, float)"/>
    /// <param name="getCharacterAdvance">The unscaled advance of one character; a surrogate pair sums its halves.</param>
    public static int GetIndexInLine(string? text, float cursorOffset, Func<char, float> getCharacterAdvance, float fontScale) =>
        GetIndexInLine(text, cursorOffset, (line, start, length) =>
        {
            float advance = 0;
            for (int i = 0; i < length; i++)
            {
                advance += getCharacterAdvance(line[start + i]);
            }
            return advance;
        }, fontScale);

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
