using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;

namespace RenderingLibrary.Graphics;

public class StyledSubstring
{
    public List<InlineVariable> Variables = new List<InlineVariable>();
    public string Substring;
    public int StartIndex;

    public override string ToString()
    {
        var toReturn = Substring ?? "<null>";

        foreach (var variable in Variables)
        {
            toReturn += $" {variable.VariableName} = {variable.Value}";
        }
        return toReturn;
    }
}

/// <summary>
/// Splits a single line of already-wrapped, BBCode-stripped text into runs ("styled substrings") according
/// to which <see cref="InlineVariable"/>s (color, font scale, custom callbacks, etc.) are active over each
/// character. This has no dependency on any particular rendering backend - it only reasons about the
/// stripped text and the character index ranges the inline variables cover, so any Text renderable
/// (MonoGame's BitmapFont-based renderer, raylib's native-Font renderer, etc.) can share it instead of
/// reimplementing the run-splitting logic per backend.
/// </summary>
public class StyledSubstringSplitter
{
    public List<StyledSubstring> GetStyledSubstrings(int startOfLineIndex, string lineOfText, Color color, List<InlineVariable> inlineVariables)
    {
        List<StyledSubstring> substrings = new();
        int currentSubstringStart = 0;

        List<InlineVariable> currentlyActiveInlines = new();
        List<InlineVariable> inlinesForThisCharacter = new();

        int relativeLetterIndex = 0;
        for (; relativeLetterIndex < lineOfText.Length; relativeLetterIndex++)
        {
            inlinesForThisCharacter.Clear();
            var absoluteIndex = startOfLineIndex + relativeLetterIndex;

            var startNewRun = relativeLetterIndex == 0;
            var endLastRun = false;
            foreach (var variable in inlineVariables)
            {

                if (absoluteIndex >= variable.StartIndex && absoluteIndex < variable.StartIndex + variable.CharacterCount)
                {
                    if (currentlyActiveInlines.Contains(variable) == false)
                    {
                        startNewRun = true;
                        endLastRun = true;
                    }
                    inlinesForThisCharacter.Add(variable);
                }
            }

            foreach (var variable in currentlyActiveInlines)
            {
                if (absoluteIndex >= variable.StartIndex + variable.CharacterCount)
                {
                    startNewRun = true;
                    endLastRun = true;
                }
            }

            if (endLastRun && substrings.Count > 0)
            {
                var lastSubstring = substrings.Last();
                lastSubstring.Substring = lineOfText.Substring(currentSubstringStart, relativeLetterIndex - currentSubstringStart);
            }

            if (startNewRun)
            {
                currentSubstringStart = relativeLetterIndex;

                var styledSubstring = new StyledSubstring();
                foreach (var item in inlinesForThisCharacter)
                {
                    // Custom variables can stack (e.g. [Custom=A][Custom=B]),
                    // so allow multiple with the same VariableName. Other variables
                    // like Color should replace the previous value.
                    if (item.VariableName != "Custom")
                    {
                        var existing = styledSubstring.Variables.FirstOrDefault(x => x.VariableName == item.VariableName);
                        if (existing != null)
                        {
                            styledSubstring.Variables.Remove(existing);
                        }
                    }
                    styledSubstring.Variables.Add(item);
                }
                styledSubstring.StartIndex = relativeLetterIndex;

                if (relativeLetterIndex == lineOfText.Length - 1)
                {
                    styledSubstring.Substring = lineOfText.Substring(currentSubstringStart);
                }

                substrings.Add(styledSubstring);

                currentlyActiveInlines.Clear();
                currentlyActiveInlines.AddRange(inlinesForThisCharacter);
            }
        }

        var endSubstring = substrings.LastOrDefault();
        if (endSubstring != null)
        {
            endSubstring.Substring = lineOfText.Substring(currentSubstringStart, relativeLetterIndex - currentSubstringStart);
        }

        return substrings;
    }
}
