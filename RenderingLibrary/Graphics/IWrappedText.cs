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
    /// Returns the full-advance width of <paramref name="text"/> — every glyph counted at its full advance,
    /// with NO trailing-glyph trim — or a negative value if the backend can't provide it. This lets the
    /// wrapping loop measure a candidate line incrementally (full advance of the already-committed line plus
    /// the next word's own width) instead of concatenating <c>currentLine + currentWord</c> just to measure
    /// it, so that throwaway string is only built when the word is actually appended, never on the overflow
    /// branch (issue #1934). Because the committed line's last glyph is NOT the line's last glyph once a word
    /// follows it, its full advance is what the concatenation would measure. A backend must return negative
    /// whenever it can't guarantee that equivalence — most importantly when an inline BBCode run is active,
    /// since the whole line must then be measured together for per-run sizing — so the caller falls back to
    /// exact concatenation. The default implementation opts out (returns -1).
    /// </summary>
    float MeasureStringFullAdvance(string text) => -1;

    bool IsMidWordLineBreakEnabled { get; }
}

public static class IWrappedTextExtensions
{
    const string ellipsis = "...";

    // Reusable word buffer for the word-by-word wrapping path (issue #1934). UpdateLines runs every
    // layout frame for constrained Text; renting and clearing a single per-thread list avoids allocating
    // a fresh List plus the throwaway array String.Split returns on each call. Thread-static so concurrent
    // layout on separate threads doesn't share the buffer; a null field marks it "rented" so a reentrant
    // call on the same thread transparently falls back to a fresh list instead of corrupting the shared one.
    [ThreadStatic]
    static List<string>? _pooledWordList;

    public static void UpdateLines(this IWrappedText textInstance, List<string> lines)
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
        if (textInstance.Width != null && !float.IsPositiveInfinity(textInstance.Width.Value) && textInstance.FontScale > 0)
        {
            wrappingWidth = MathFunctions.RoundToInt(System.Math.Ceiling(textInstance.Width.Value / textInstance.FontScale));
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

        // This allocates like crazy but we're
        // on the PC and prob won't be calling this
        // very frequently so let's
        String currentLine = String.Empty;

        // The absolute character index (in the stripped text) where the current line begins. It equals the
        // total length of the lines already committed, so it stays in sync with the inline-variable indexing
        // the styled measurement uses (the same accounting DrawWithInlineVariables/size passes do with
        // `startOfLineIndex += line.Length`). We advance it whenever a line is committed below, and pass it
        // to the styled MeasureString overload so a line containing an inline [FontSize]/[FontScale] run is
        // measured at the run's real size while wrapping.
        int absoluteLineStartIndex = 0;

        // The words to process, including the current word. Tokenize directly into a pooled list rather
        // than String.Split (which allocates a throwaway array) + a fresh List every call — this is the
        // genuinely-wrapping path that runs every layout frame for constrained Text (issue #1934). The
        // list is returned to the pool at the single exit point after the loop below.
        List<string> remainingWordsToProcess = RentWordList();
        SplitOnSpaces(remainingWordsToProcess, stringToUse);


        bool isLastLine = false;
        while (remainingWordsToProcess.Count != 0)
        {
            isLastLine = effectiveMaxNumberOfLines != null && lines.Count == effectiveMaxNumberOfLines - 1;

            // The current word, separated by newlines if one exists. The remainingWordsToProcess
            // continues to hold the unmodified word
            string currentWord = remainingWordsToProcess[0];
            var wordBeforeNewlineRemoval = currentWord;
            var isLastWord = remainingWordsToProcess.Count == 1;

            bool containsNewline = false;
            bool startsWithNewline = false;

            if (ToolsUtilities.StringFunctions.ContainsNoAlloc(currentWord, '\n'))
            {
                startsWithNewline = currentWord.StartsWith("\n");

                // Newline is an explicit character that the user might
                // enter in a textbox. We don't want to lose this character
                // becuse it can be deleted.
                //word = word.Substring(0, word.IndexOf('\n'));
                currentWord = currentWord.Substring(0, currentWord.IndexOf('\n') + 1);
                containsNewline = true;
            }

            // If it's not the last word, we show ellipsis, and the last word plus ellipsis won't fit, then we need
            // to include part of the word:

            // Measure the candidate line (currentLine + currentWord) to decide whether the word fits.
            // linePlusWord is the concatenated string; it is built lazily so the overflow branch — which
            // measures but then discards the candidate — never allocates it (issue #1934). When the line is
            // empty the candidate IS the word (no concat). When the backend can report a line's full-advance
            // width (the XNA/BitmapFont path with no inline run), measure incrementally so the concatenation
            // is only built if the word is actually appended below; otherwise fall back to exact concatenation.
            string? linePlusWord;
            float linePlusWordWidth;
            if (currentLine.Length == 0)
            {
                linePlusWord = currentWord;
                linePlusWordWidth = textInstance.MeasureString(currentWord, absoluteLineStartIndex);
            }
            else
            {
                float lineFullAdvance = textInstance.MeasureStringFullAdvance(currentLine);
                if (lineFullAdvance >= 0)
                {
                    // No inline run active: the candidate width is the committed line's full advance (its
                    // last glyph isn't trimmed because the word follows it) plus the word's own width.
                    linePlusWord = null;
                    linePlusWordWidth = lineFullAdvance + textInstance.MeasureString(currentWord);
                }
                else
                {
                    linePlusWord = currentLine + currentWord;
                    linePlusWordWidth = textInstance.MeasureString(linePlusWord, absoluteLineStartIndex);
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
                for (int i = 1; i < currentWord.Length; i++)
                {
                    var substringEnd = currentWord.SubstringEnd(i);

                    float linePlusWordSub = textInstance.MeasureString(currentLine + substringEnd, absoluteLineStartIndex);

                    if (linePlusWordSub + ellipsisWidth <= wrappingWidth)
                    {
                        lines.Add(currentLine + substringEnd + ellipsis);
                        addedEllipsis = true;
                        break;
                    }
                }

                if (!addedEllipsis && currentLine.EndsWith(" "))
                {
                    lines.Add(currentLine.SubstringEnd(1) + ellipsis);

                }
                break;
            }

            bool handledByLineTooLong = false;

            if (linePlusWordWidth > wrappingWidth)
            {
                if (!string.IsNullOrEmpty(currentLine))
                {
                    handledByLineTooLong = true;
                    // We already have a line started, so let's add it
                    // and start the next line
                    lines.Add(currentLine);
                    absoluteLineStartIndex += currentLine.Length;
                    if (lines.Count == effectiveMaxNumberOfLines)
                    {
                        break;
                    }

                    currentLine = String.Empty;
                }
                // we don't have a line started, but we have a word that is too
                // long. if we're not using ellipses, then we should measure the
                // string, add what we can, and go to the next line:
                else if (textInstance.IsMidWordLineBreakEnabled)
                {
                    handledByLineTooLong = true;
                    if (currentWord.Length == 1)
                    {
                        lines.Add(currentWord);
                        absoluteLineStartIndex += currentWord.Length;
                        remainingWordsToProcess.RemoveAt(0);
                    }
                    else
                    {
                        // Track zero-width space positions as preferred break points for mid-word wrapping.
                        // When a line needs to wrap mid-word, we prefer breaking at zero-width space (\u200B)
                        // positions if they exist, as they indicate logical break points in the text.
                        int? preferredBreakIndex = null;
                        float? preferredBreakWidth = null;

                        for (int i = 1; i < currentWord.Length; i++)
                        {
                            var substring = currentWord.Substring(0, i + 1);
                            // currentLine is empty in this mid-word-break branch, so the substring begins at
                            // the current line's absolute start.
                            float substringLength = textInstance.MeasureString(substring, absoluteLineStartIndex);

                            // Check if there's a zero-width space at this position that could be a preferred break point
                            if (i < currentWord.Length && currentWord[i] == '\u200B' && substringLength < wrappingWidth)
                            {
                                preferredBreakIndex = i;
                                preferredBreakWidth = substringLength;
                            }

                            if (substringLength >= wrappingWidth)
                            {
                                string stringToAdd = string.Empty;

                                // If we have a preferred break point (zero-width space) before this position, use it
                                if (preferredBreakIndex.HasValue)
                                {
                                    // Break at the zero-width space position
                                    stringToAdd = currentWord.Substring(0, preferredBreakIndex.Value);
                                    lines.Add(stringToAdd);
                                    // The zero-width space at preferredBreakIndex is skipped (the next line
                                    // starts one past it), so advance past it too to stay in sync.
                                    absoluteLineStartIndex += stringToAdd.Length + 1;

                                    // Skip the zero-width space character when continuing
                                    currentWord = remainingWordsToProcess[0].Substring(preferredBreakIndex.Value + 1);
                                    remainingWordsToProcess[0] = currentWord;
                                    break;
                                }
                                // word fits perfectly in the line, so add
                                // the substring
                                else if (substringLength == wrappingWidth)
                                {
                                    // add this word to the lines, and subtract what was added
                                    // from the current word:
                                    stringToAdd = currentWord.Substring(0, i + 1);
                                    lines.Add(stringToAdd);
                                    absoluteLineStartIndex += stringToAdd.Length;

                                    // Be sure to use remainingWordsToProcess[0] so we get everything
                                    // before it was broken up by newlines
                                    currentWord = remainingWordsToProcess[0].Substring(i + 1);
                                    remainingWordsToProcess[0] = currentWord;
                                    break;
                                }
                                else if (substringLength > wrappingWidth)
                                {
                                    stringToAdd = currentWord.Substring(0, i);
                                    lines.Add(stringToAdd);
                                    absoluteLineStartIndex += stringToAdd.Length;

                                    // Be sure to use remainingWordsToProcess[0] so we get everything
                                    // before it was broken up by newlines
                                    currentWord = remainingWordsToProcess[0].Substring(i);
                                    remainingWordsToProcess[0] = currentWord;
                                    break;
                                }
                            }

                        }
                    }

                    currentLine = String.Empty;
                }
            }
            if (!handledByLineTooLong)
            {
                // If it's the first word and it's empty, don't add anything
                // update - but this prevents the word from starting with a space, which it should be able to 
                //if ((!string.IsNullOrEmpty(word) || !string.IsNullOrEmpty(line)))
                {

                    if (remainingWordsToProcess.Count > 1 &&
                        // Update Feb 19, 2023
                        // don't insert space after if it's a newline. That messes up indexes.
                        !containsNewline)
                    {
                        // A trailing empty word (from RawText ending in a space) used to also
                        // hit this branch via `currentWord == ""`, appending a phantom extra
                        // space. That made WrappedText longer than RawText and pushed TextBox
                        // caretIndex past Text.Length on click — see issue #2617.
                        // linePlusWord is null only on the incremental-measure path; build the concat now
                        // (it's the committed output, so it is not throwaway here).
                        currentLine = (linePlusWord ?? currentLine + currentWord) + ' ';
                    }
                    else
                    {
                        currentLine = linePlusWord ?? currentLine + currentWord;
                    }
                }

                remainingWordsToProcess.RemoveAt(0);

                if (containsNewline)
                {
                    lines.Add(currentLine);
                    absoluteLineStartIndex += currentLine.Length;


                    if (lines.Count == effectiveMaxNumberOfLines)
                    {
                        break;
                    }
                    currentLine = string.Empty;

                    int indexOfNewline = wordBeforeNewlineRemoval.IndexOf('\n');
                    remainingWordsToProcess.Insert(0, wordBeforeNewlineRemoval.Substring(indexOfNewline + 1, wordBeforeNewlineRemoval.Length - (indexOfNewline + 1)));
                }

            }

        }

        if (effectiveMaxNumberOfLines == null || lines.Count < effectiveMaxNumberOfLines)
        {
            lines.Add(currentLine);
        }

        // Single exit point: the loop above only ever breaks or falls through here, so returning the
        // buffer once is sufficient (no early return threads past this).
        ReturnWordList(remainingWordsToProcess);

        // This is up to the implementer to set:
        //mNeedsBitmapFontRefresh = true;
    }

    /// <summary>
    /// Splits <paramref name="value"/> on the space character into <paramref name="destination"/>,
    /// preserving empty entries exactly as <c>string.Split(' ')</c> does (leading, interior, and trailing
    /// spaces each produce an empty token). The wrapping algorithm relies on those empty tokens — a
    /// trailing space is significant for TextBox caret indexing (issue #2617).
    /// </summary>
    static void SplitOnSpaces(List<string> destination, string value)
    {
        int start = 0;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == ' ')
            {
                destination.Add(value.Substring(start, i - start));
                start = i + 1;
            }
        }
        destination.Add(value.Substring(start, value.Length - start));
    }

    /// <summary>
    /// Rents the per-thread word buffer, clearing it for reuse. A reentrant call (the buffer is already
    /// rented on this thread) gets a fresh list so the two calls don't corrupt each other's state.
    /// </summary>
    static List<string> RentWordList()
    {
        List<string>? rented = _pooledWordList;
        if (rented == null)
        {
            return new List<string>();
        }

        _pooledWordList = null;
        rented.Clear();
        return rented;
    }

    /// <summary>
    /// Returns a list previously obtained from <see cref="RentWordList"/> to the per-thread pool. A list
    /// from the reentrant fallback (pool already occupied) is simply dropped for the GC to reclaim.
    /// </summary>
    static void ReturnWordList(List<string> list)
    {
        list.Clear();
        _pooledWordList ??= list;
    }

    static string SubstringEnd(this string value, int lettersToRemove)
    {
        if (value.Length <= lettersToRemove)
        {
            return string.Empty;
        }
        else
        {
            return value.Substring(0, value.Length - lettersToRemove);
        }
    }

}