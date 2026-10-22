using RenderingLibrary.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary.Graphics;

public interface IWrappedText : IText
{
    int? MaxNumberOfLines { get; }


    float Height { get; }

    int LineHeightInPixels { get; }
    bool IsTruncatingWithEllipsisOnLastLine { get; }

    /// <summary>
    /// Whether this text's Height is derived from its own lines (content), as opposed to being an
    /// independent constraint. When true, <see cref="TextOverflowVerticalMode.TruncateLine"/> must NOT
    /// limit the number of lines by Height — doing so would be a circular dependency (the height comes
    /// from the lines, so capping the lines by that height progressively collapses the text). This is
    /// pushed by the layout (GraphicalUiElement) and is true when the element's HeightUnits is
    /// RelativeToChildren. The renderable itself has no concept of HeightUnits, which is why the layout
    /// sets this, mirroring how a RelativeToChildren width pushes a null wrap Width onto the renderable.
    /// </summary>
    bool IsHeightDependentOnLines { get; set; }

    float MeasureString(string text);

    /// <summary>
    /// Measures <paramref name="text"/>, which begins at <paramref name="absoluteStartIndexInStrippedText"/>
    /// within the full stripped (tags-removed) text, honoring any inline BBCode runs (a [FontSize] font swap
    /// or a [FontScale] multiplier) active over that character range so line wrapping breaks where the text
    /// actually renders rather than where the base font would. The returned width is in the same base units
    /// as <see cref="MeasureString(string)"/> (font scale factored out), so it compares directly against the
    /// wrap width. The default implementation ignores inline runs (measures at the base font); a backend
    /// opts in to font-aware wrapping by overriding it.
    /// </summary>
    float MeasureString(string text, int absoluteStartIndexInStrippedText) => MeasureString(text);

    /// <summary>
    /// Full-advance width of <paramref name="text"/> (every glyph at full advance, no trailing-glyph trim),
    /// or negative if the backend can't guarantee that equals measuring the concatenated candidate line —
    /// e.g. when an inline BBCode run is active and the whole line must be sized together. Lets the wrapping
    /// loop size a candidate incrementally instead of concatenating currentLine + currentWord just to measure.
    /// Default: -1 (opt out).
    /// </summary>
    float MeasureStringFullAdvance(string text) => -1;

    /// <summary>
    /// Span form of <see cref="MeasureString(string)"/>, so the wrapping loop can measure a word or a line
    /// that lives inside the raw text without cutting a string out for it. The default makes the string;
    /// a backend that measures glyph by glyph overrides it to measure the span in place.
    /// </summary>
    float MeasureString(ReadOnlySpan<char> text) => MeasureString(text.ToString());

    /// <summary>Span form of <see cref="MeasureString(string, int)"/>; the default makes the string.</summary>
    float MeasureString(ReadOnlySpan<char> text, int absoluteStartIndexInStrippedText) =>
        MeasureString(text.ToString(), absoluteStartIndexInStrippedText);

    /// <summary>Span form of <see cref="MeasureStringFullAdvance(string)"/>. Default: -1 (opt out).</summary>
    float MeasureStringFullAdvance(ReadOnlySpan<char> text) => -1;

    bool IsMidWordLineBreakEnabled { get; }

    /// <summary>
    /// The scale a Width constraint is divided by to compute the word-wrap width, in the same base
    /// units as <see cref="MeasureString(string)"/>. Distinct from <see cref="IText.FontScale"/> when a
    /// backend composes a display-only compensation into FontScale (e.g. MonoGame's camera-zoom font
    /// oversampling, issue #4309/#4317) -- that compensation must not affect where text wraps, since
    /// wrap decisions measure words against a pinned, stable font in native units (issue #4370: an
    /// Absolute-width box re-wrapped every time oversampling recomputed its compensation ratio, even at
    /// a fixed zoom, because the wrap width was still being computed with the compensated scale).
    /// Defaults to <see cref="IText.FontScale"/> for backends with no such compensation.
    /// </summary>
    float WordWrapFontScale => FontScale;
}

public static class IWrappedTextExtensions
{
    const string ellipsis = "...";

    // A word of the raw text as a range into it, so tokenizing makes no strings.
    private struct WordRange
    {
        public int Start;
        public int Length;
    }

    // Reused word buffer for the wrapping path, avoiding a fresh List each layout frame.
    // Thread-static so parallel layout doesn't share it; null means "rented", so a reentrant call on the
    // same thread falls back to a fresh list rather than corrupting the shared one.
    [ThreadStatic]
    static List<WordRange>? _pooledWordList;

    /// <summary>
    /// Rebuilds <paramref name="lines"/> for the current text. The previous contents are kept in
    /// <paramref name="previousLinesBuffer"/> (a list the caller owns and reuses) while wrapping, so a
    /// line whose text did not change keeps its string and a text that changes every frame only makes
    /// strings for the lines that did change (#6420).
    /// </summary>
    public static void RebuildLines(this IWrappedText textInstance, List<string> lines, List<string> previousLinesBuffer)
    {
        previousLinesBuffer.Clear();
        previousLinesBuffer.AddRange(lines);
        lines.Clear();
        textInstance.UpdateLines(lines, previousLinesBuffer);
        previousLinesBuffer.Clear();
    }

    /// <param name="textInstance">The text to wrap.</param>
    /// <param name="lines">Receives the wrapped lines.</param>
    /// <param name="previousLines">Optional lines from the last wrap. A new line equal to the line at the
    /// same index is replaced by that string, so it is not allocated again.</param>
    public static void UpdateLines(this IWrappedText textInstance, List<string> lines, List<string>? previousLines = null)
    {
        var effectiveMaxNumberOfLines = textInstance.MaxNumberOfLines;

        // The Height-derived line cap only makes sense when Height is an independent constraint. When
        // Height is derived from the lines (RelativeToChildren), capping the lines by that height is a
        // circular dependency that collapses the text (issue #3372), so skip it — an explicit
        // MaxNumberOfLines still applies below.
        if (textInstance.TextOverflowVerticalMode == TextOverflowVerticalMode.TruncateLine
            && !textInstance.IsHeightDependentOnLines)
        {

            var maxLinesFromHeight = (int)(textInstance.Height / textInstance.LineHeightInPixels);
            if (maxLinesFromHeight < effectiveMaxNumberOfLines || effectiveMaxNumberOfLines == null)
            {
                effectiveMaxNumberOfLines = maxLinesFromHeight;
            }
        }

        if (string.IsNullOrEmpty(textInstance.RawText) || effectiveMaxNumberOfLines == 0)
        {
            return;
        }
        /////////END EARLY OUT///////////

        float ellipsisWidth = (effectiveMaxNumberOfLines > 0 && textInstance.IsTruncatingWithEllipsisOnLastLine)
            ? ellipsisWidth = textInstance.MeasureString(ellipsis)
            : 0;

        // Past the early-out above, RawText is non-empty. Multiline text editing in Gum can add \r's,
        // so normalize CRLF once here; both the fast path and the wrapping loop consume this form.
        string stringToUse = textInstance.RawText!.Replace("\r\n", "\n");


        int wrappingWidth = int.MaxValue;
        if (textInstance.Width != null && !float.IsPositiveInfinity(textInstance.Width.Value) && textInstance.WordWrapFontScale > 0)
        {
            wrappingWidth = MathFunctions.RoundToInt(System.Math.Ceiling(textInstance.Width.Value / textInstance.WordWrapFontScale));
        }

        wrappingWidth = System.Math.Max(0, wrappingWidth);

        // FAST PATH (issue #1934): a string with no explicit newline that already fits within the wrap
        // width wraps to exactly one line equal to itself, so short-circuit the word-by-word algorithm
        // below and add it directly with zero managed allocation. The general algorithm reproduces
        // stringToUse verbatim in this case — interior/leading/trailing spaces included (see #2617) — so
        // this is behavior-preserving. This is the common case, and specifically the natural-size
        // (unconstrained-width) measurement pass a RelativeToChildren Text runs every layout frame, which
        // was the dominant full-relayout allocation source (it re-tokenized/Split/concatenated an
        // unchanged, unwrapped string ~60x/second). MeasureString is allocation-free when the text has no
        // inline runs (it sums glyph advances); inline-BBCode text still measures correctly, just not for
        // free. Ellipsis/truncation only engages on a word that overflows, so it never applies when the
        // whole string fits — safe to skip.
        if (!ToolsUtilities.StringFunctions.ContainsNoAlloc(stringToUse, '\n')
            && textInstance.MeasureString(stringToUse, 0) <= wrappingWidth)
        {
            lines.Add(stringToUse);
            return;
        }

        // Every line is a run of characters in stringToUse (the words and the single spaces between them
        // stay in order), so the loop tracks the current line and the current word as ranges into it and
        // measures them as spans. Only a committed line becomes a string (AddLine), and a line equal to the
        // previous wrap's line at the same index reuses that string (#6420).
        //
        // The current line is [currentLineStart, currentLineStart + currentLineLength). When it is not
        // empty it ends right where the current word starts, because every word that joins a line brings
        // the space after it along.
        int currentLineStart = 0;
        int currentLineLength = 0;

        // The absolute character index (in the stripped text) where the current line begins. It equals the
        // total length of the lines already committed, so it stays in sync with the inline-variable indexing
        // the styled measurement uses (the same accounting DrawWithInlineVariables/size passes do with
        // `startOfLineIndex += line.Length`). We advance it whenever a line is committed below, and pass it
        // to the styled MeasureString overload so a line containing an inline [FontSize]/[FontScale] run is
        // measured at the run's real size while wrapping.
        int absoluteLineStartIndex = 0;

        // The words left to process are words[firstWord..]. Pooled, returned at the single exit point.
        List<WordRange> words = RentWordList();
        SplitOnSpaces(words, stringToUse);
        int firstWord = 0;

        bool isLastLine = false;
        while (firstWord < words.Count)
        {
            isLastLine = effectiveMaxNumberOfLines != null && lines.Count == effectiveMaxNumberOfLines - 1;

            // The current word, cut after a newline if one exists. words[firstWord] continues to hold the
            // unmodified word.
            WordRange wholeWord = words[firstWord];
            int wordStart = wholeWord.Start;
            int wordLength = wholeWord.Length;
            var isLastWord = firstWord == words.Count - 1;

            bool containsNewline = false;

            int newlineIndex = stringToUse.IndexOf('\n', wordStart, wholeWord.Length);
            if (newlineIndex >= 0)
            {
                // Newline is an explicit character that the user might
                // enter in a textbox. We don't want to lose this character
                // becuse it can be deleted.
                wordLength = newlineIndex - wordStart + 1;
                containsNewline = true;
            }

            if (currentLineLength == 0)
            {
                currentLineStart = wordStart;
            }

            // If it's not the last word, we show ellipsis, and the last word plus ellipsis won't fit, then we need
            // to include part of the word:

            // Measure the candidate line (the current line plus the word, which is one range of stringToUse)
            // to decide whether the word fits: an empty line => the word is the candidate; else if the
            // backend reports a full-advance width (no inline run), measure incrementally; otherwise measure
            // the whole candidate.
            int candidateLength = currentLineLength + wordLength;
            float linePlusWordWidth;
            if (currentLineLength == 0)
            {
                linePlusWordWidth = textInstance.MeasureString(stringToUse.AsSpan(wordStart, wordLength), absoluteLineStartIndex);
            }
            else
            {
                float lineFullAdvance = textInstance.MeasureStringFullAdvance(stringToUse.AsSpan(currentLineStart, currentLineLength));
                if (lineFullAdvance >= 0)
                {
                    // No inline run active: the candidate width is the committed line's full advance (its
                    // last glyph isn't trimmed because the word follows it) plus the word's own width.
                    linePlusWordWidth = lineFullAdvance + textInstance.MeasureString(stringToUse.AsSpan(wordStart, wordLength));
                }
                else
                {
                    linePlusWordWidth = textInstance.MeasureString(stringToUse.AsSpan(currentLineStart, candidateLength), absoluteLineStartIndex);
                }
            }

            var shouldAddEllipsis =
                textInstance.IsTruncatingWithEllipsisOnLastLine &&
                isLastLine &&
                // If it's the last word, then we don't care if the ellipsis fit, we only want to see if the last word fits...
                ((isLastWord && linePlusWordWidth > wrappingWidth) ||
                 // it's not the last word so we need to see if ellipsis fit
                 (!isLastWord && linePlusWordWidth + ellipsisWidth >= wrappingWidth));
            if (shouldAddEllipsis)
            {
                var addedEllipsis = false;
                for (int i = 1; i < wordLength; i++)
                {
                    int lengthWithoutEnd = currentLineLength + wordLength - i;

                    float linePlusWordSub = textInstance.MeasureString(stringToUse.AsSpan(currentLineStart, lengthWithoutEnd), absoluteLineStartIndex);

                    if (linePlusWordSub + ellipsisWidth <= wrappingWidth)
                    {
                        lines.Add(stringToUse.Substring(currentLineStart, lengthWithoutEnd) + ellipsis);
                        addedEllipsis = true;
                        break;
                    }
                }

                if (!addedEllipsis && currentLineLength > 0 && stringToUse[currentLineStart + currentLineLength - 1] == ' ')
                {
                    lines.Add(stringToUse.Substring(currentLineStart, currentLineLength - 1) + ellipsis);

                }
                break;
            }

            bool handledByLineTooLong = false;

            if (linePlusWordWidth > wrappingWidth)
            {
                if (currentLineLength > 0)
                {
                    handledByLineTooLong = true;
                    // We already have a line started, so let's add it
                    // and start the next line
                    AddLine(lines, previousLines, stringToUse, currentLineStart, currentLineLength);
                    absoluteLineStartIndex += currentLineLength;
                    if (lines.Count == effectiveMaxNumberOfLines)
                    {
                        break;
                    }

                    currentLineLength = 0;
                }
                // we don't have a line started, but we have a word that is too
                // long. if we're not using ellipses, then we should measure the
                // string, add what we can, and go to the next line:
                else if (textInstance.IsMidWordLineBreakEnabled)
                {
                    handledByLineTooLong = true;
                    if (wordLength == 1)
                    {
                        AddLine(lines, previousLines, stringToUse, wordStart, 1);
                        absoluteLineStartIndex += 1;
                        firstWord++;
                    }
                    else
                    {
                        // Track zero-width space positions as preferred break points for mid-word wrapping.
                        // When a line needs to wrap mid-word, we prefer breaking at zero-width space (​)
                        // positions if they exist, as they indicate logical break points in the text.
                        int? preferredBreakIndex = null;

                        for (int i = 1; i < wordLength; i++)
                        {
                            // currentLine is empty in this mid-word-break branch, so the substring begins at
                            // the current line's absolute start.
                            float substringLength = textInstance.MeasureString(stringToUse.AsSpan(wordStart, i + 1), absoluteLineStartIndex);

                            // Check if there's a zero-width space at this position that could be a preferred break point
                            if (stringToUse[wordStart + i] == '​' && substringLength < wrappingWidth)
                            {
                                preferredBreakIndex = i;
                            }

                            if (substringLength >= wrappingWidth)
                            {
                                // The first part of the word goes on its own line; the rest stays in
                                // words[firstWord] (which still holds everything before it was broken up
                                // by newlines) for the next line.
                                int lengthToAdd;
                                int lengthToSkip;

                                // If we have a preferred break point (zero-width space) before this position, use it
                                if (preferredBreakIndex.HasValue)
                                {
                                    // Break at the zero-width space position; the zero-width space itself
                                    // is skipped (the next line starts one past it), so advance past it too.
                                    lengthToAdd = preferredBreakIndex.Value;
                                    lengthToSkip = lengthToAdd + 1;
                                }
                                // word fits perfectly in the line, so add the substring
                                else if (substringLength == wrappingWidth)
                                {
                                    lengthToAdd = i + 1;
                                    lengthToSkip = lengthToAdd;
                                }
                                else
                                {
                                    lengthToAdd = i;
                                    lengthToSkip = lengthToAdd;
                                }

                                AddLine(lines, previousLines, stringToUse, wordStart, lengthToAdd);
                                absoluteLineStartIndex += lengthToSkip;
                                words[firstWord] = new WordRange
                                {
                                    Start = wordStart + lengthToSkip,
                                    Length = wholeWord.Length - lengthToSkip
                                };
                                break;
                            }

                        }
                    }

                    currentLineLength = 0;
                }
            }
            if (!handledByLineTooLong)
            {
                // If it's the first word and it's empty, don't add anything
                // update - but this prevents the word from starting with a space, which it should be able to
                //if ((!string.IsNullOrEmpty(word) || !string.IsNullOrEmpty(line)))
                {

                    if (words.Count - firstWord > 1 &&
                        // Update Feb 19, 2023
                        // don't insert space after if it's a newline. That messes up indexes.
                        !containsNewline)
                    {
                        // A trailing empty word (from RawText ending in a space) used to also
                        // hit this branch via `currentWord == ""`, appending a phantom extra
                        // space. That made WrappedText longer than RawText and pushed TextBox
                        // caretIndex past Text.Length on click — see issue #2617.
                        // The extra character is the space that separated this word from the next.
                        currentLineLength = candidateLength + 1;
                    }
                    else
                    {
                        currentLineLength = candidateLength;
                    }
                }

                firstWord++;

                if (containsNewline)
                {
                    AddLine(lines, previousLines, stringToUse, currentLineStart, currentLineLength);
                    absoluteLineStartIndex += currentLineLength;


                    if (lines.Count == effectiveMaxNumberOfLines)
                    {
                        break;
                    }
                    currentLineLength = 0;

                    // What follows the newline in the word is processed next, as its own word.
                    int afterNewline = newlineIndex + 1;
                    firstWord--;
                    words[firstWord] = new WordRange
                    {
                        Start = afterNewline,
                        Length = wholeWord.Start + wholeWord.Length - afterNewline
                    };
                }

            }

        }

        if (effectiveMaxNumberOfLines == null || lines.Count < effectiveMaxNumberOfLines)
        {
            AddLine(lines, previousLines, stringToUse, currentLineStart, currentLineLength);
        }

        // Single exit point (no early return threads past this), so returning the buffer once is enough.
        ReturnWordList(words);

        // This is up to the implementer to set:
        //mNeedsBitmapFontRefresh = true;
    }

    /// <summary>
    /// Adds <c>source[start..start + length]</c> as a line, using the string already at this line's index in
    /// <paramref name="previousLines"/> when it holds the same text.
    /// </summary>
    static void AddLine(List<string> lines, List<string>? previousLines, string source, int start, int length)
    {
        if (length == 0)
        {
            lines.Add(string.Empty);
            return;
        }

        int index = lines.Count;
        if (previousLines != null && index < previousLines.Count)
        {
            string previous = previousLines[index];
            if (previous.Length == length && string.CompareOrdinal(previous, 0, source, start, length) == 0)
            {
                lines.Add(previous);
                return;
            }
        }

        lines.Add(source.Substring(start, length));
    }

    /// <summary>
    /// Splits on spaces into <paramref name="destination"/>, preserving empty entries exactly as
    /// string.Split(' ') does — the wrapping algorithm depends on them (a trailing space matters for
    /// TextBox caret indexing, #2617).
    /// </summary>
    static void SplitOnSpaces(List<WordRange> destination, string value)
    {
        int start = 0;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == ' ')
            {
                destination.Add(new WordRange { Start = start, Length = i - start });
                start = i + 1;
            }
        }
        destination.Add(new WordRange { Start = start, Length = value.Length - start });
    }

    /// <summary>Rents the per-thread word buffer (cleared for reuse); a reentrant call gets a fresh list.</summary>
    static List<WordRange> RentWordList()
    {
        List<WordRange>? rented = _pooledWordList;
        if (rented == null)
        {
            return new List<WordRange>();
        }

        _pooledWordList = null;
        rented.Clear();
        return rented;
    }

    /// <summary>Returns a rented list to the per-thread pool; a reentrant-fallback list is simply dropped.</summary>
    static void ReturnWordList(List<WordRange> list)
    {
        list.Clear();
        _pooledWordList ??= list;
    }

}
