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

    float MeasureString(string text);

    bool IsMidWordLineBreakEnabled { get; }
}

public static class IWrappedTextExtensions
{
    const string ellipsis = "...";
    static char[] whatToSplitOn = new char[] { ' ' };

    public static void UpdateLines(this IWrappedText textInstance, List<string> lines)
    {
        var effectiveMaxNumberOfLines = textInstance.MaxNumberOfLines;

        if (textInstance.TextOverflowVerticalMode == TextOverflowVerticalMode.TruncateLine)
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

        string? stringToUse = string.IsNullOrEmpty(textInstance.RawText)
            ? null
            : stringToUse = textInstance.RawText.Replace("\r\n", "\n");


        int wrappingWidth = int.MaxValue;
        if (textInstance.Width != null && !float.IsPositiveInfinity(textInstance.Width.Value) && textInstance.FontScale > 0)
        {
            wrappingWidth = MathFunctions.RoundToInt(System.Math.Ceiling(textInstance.Width.Value / textInstance.FontScale));
        }

        wrappingWidth = System.Math.Max(0, wrappingWidth);


        // This allocates like crazy but we're
        // on the PC and prob won't be calling this
        // very frequently so let's 
        String currentLine = String.Empty;
        String returnString = String.Empty;

        // The words to process, including the current word
        List<string> remainingWordsToProcess = new();

        // The user may have entered "\n" in the string, which would 
        // be written as "\\n".  Let's replace that, shall we?
        if (!string.IsNullOrEmpty(textInstance.RawText))
        {
            // multiline text editing in Gum can add \r's, so get rid of those:
            stringToUse = textInstance.RawText.Replace("\r\n", "\n");
            remainingWordsToProcess.AddRange(stringToUse.Split(whatToSplitOn));
        }


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

            float linePlusWordWidth = textInstance.MeasureString(currentLine + currentWord);

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

                    float linePlusWordSub = textInstance.MeasureString(currentLine + substringEnd);

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
                            float substringLength = textInstance.MeasureString(substring);

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

                                    // Be sure to use remainingWordsToProcess[0] so we get everything
                                    // before it was broken up by newlines
                                    currentWord = remainingWordsToProcess[0].Substring(i);
                                    remainingWordsToProcess[0] = currentWord;
                                    break;
                                }
                            }

                        }
                    }

                    //returnString = returnString + line + '\n';
                    currentLine = String.Empty;
                }
            }
            if (!handledByLineTooLong)
            {
                // If it's the first word and it's empty, don't add anything
                // update - but this prevents the word from starting with a space, which it should be able to 
                //if ((!string.IsNullOrEmpty(word) || !string.IsNullOrEmpty(line)))
                {

                    if ((remainingWordsToProcess.Count > 1 || currentWord == "") &&
                        // Update Feb 19, 2023
                        // don't insert space after if it's a newline. That messes up indexes.
                        !containsNewline)
                    {
                        currentLine = currentLine + currentWord + ' ';
                    }
                    else
                    {
                        currentLine = currentLine + currentWord;
                    }
                }

                remainingWordsToProcess.RemoveAt(0);

                if (containsNewline)
                {
                    lines.Add(currentLine);


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

        // This is up to the implementer to set:
        //mNeedsBitmapFontRefresh = true;
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