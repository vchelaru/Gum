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
    /// Whether <see cref="GetHorizontalAlignmentOffset"/> reads the line's width. A caller that must measure the
    /// line to get it skips the measurement when this is false (left alignment), where the offset is 0.
    /// </summary>
    public static bool PlacesLineByWidth(HorizontalAlignment horizontalAlignment) =>
        horizontalAlignment == HorizontalAlignment.Center || horizontalAlignment == HorizontalAlignment.Right;

    /// <summary>
    /// The distance from the left of a Text's box to the start of a line drawn <paramref name="lineWidth"/>
    /// wide: centered lines split the leftover width, right-aligned lines take all of it.
    /// </summary>
    public static float GetHorizontalAlignmentOffset(HorizontalAlignment horizontalAlignment, float boxWidth, float lineWidth)
    {
        switch (horizontalAlignment)
        {
            case HorizontalAlignment.Center:
                return (boxWidth - lineWidth) / 2.0f;
            case HorizontalAlignment.Right:
                return boxWidth - lineWidth;
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
    /// <param name="getAdvance">The drawn advance of the characters from a start index for a length.</param>
    public static int GetIndexInLine(string? text, float cursorOffset, Func<string, int, int, float> getAdvance)
    {
        text ??= string.Empty;
        int index = text.Length;
        float distanceMeasuredSoFar = 0;

        for (int i = 0; i < text.Length;)
        {
            int characterLength = IsSurrogatePairAt(text, i) ? 2 : 1;
            float advance = getAdvance(text, i, characterLength);

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

    /// <inheritdoc cref="GetIndexInLine(string?, float, Func{string, int, int, float})"/>
    /// <param name="text">The line.</param>
    /// <param name="cursorOffset">The x offset from the line's start, in drawn units.</param>
    /// <param name="getCharacterAdvance">The drawn advance of one character; a surrogate pair sums its halves.</param>
    public static int GetIndexInLine(string? text, float cursorOffset, Func<char, float> getCharacterAdvance) =>
        GetIndexInLine(text, cursorOffset, (line, start, length) =>
        {
            float advance = 0;
            for (int i = 0; i < length; i++)
            {
                advance += getCharacterAdvance(line[start + i]);
            }
            return advance;
        });

    /// <summary>
    /// Returns the index into the concatenation of <paramref name="lines"/> nearest a point given
    /// relative to the top-left of the first line, before horizontal alignment.
    /// </summary>
    /// <param name="lines">The wrapped lines, as drawn.</param>
    /// <param name="cursorXOffset">The point's x offset from the text's left edge.</param>
    /// <param name="cursorYOffset">The point's y offset from the top of the first line.</param>
    /// <param name="getLineTop">The distance from the top of the first line to the top of a line.</param>
    /// <param name="getLineXOffset">The horizontal alignment offset of a line.</param>
    /// <param name="getIndexInLine">The index in a line nearest an x offset from the line's start.</param>
    public static int GetIndexInWrappedLines(
        List<string> lines,
        float cursorXOffset,
        float cursorYOffset,
        Func<int, float> getLineTop,
        Func<string, float> getLineXOffset,
        Func<float, string, int> getIndexInLine)
    {
        int index = 0;
        int lineOn = 0;
        while (lineOn < lines.Count - 1 && getLineTop(lineOn + 1) <= cursorYOffset)
        {
            lineOn++;
        }

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
