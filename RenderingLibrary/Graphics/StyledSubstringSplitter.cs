using System.Collections.Generic;
using System.Linq;

namespace RenderingLibrary.Graphics;

/// <summary>
/// A single styling directive parsed from a BBCode tag, covering a contiguous range of characters in the
/// "stripped" (tags-removed) text. Produced when markup text is assigned to a Text renderable and consumed
/// by <see cref="StyledSubstringSplitter"/> to break a line into styled runs. Lives here (rather than in a
/// specific Text renderable) so every rendering backend can share both the model and the splitter.
/// </summary>
public class InlineVariable
{
    /// <summary>
    /// Variable name, such as "Font". This translates to the left-side of the assignment in the tag. For example
    /// [Font=Arial] would have a VariableName of "Font".
    /// </summary>
    public string VariableName = null!;

    /// <summary>
    /// The start index of the tag in the "stripped" text (after all tags have been removed).
    /// </summary>
    public int StartIndex;

    /// <summary>
    /// The number of characters covered by this inline variable. This is the character count on the "stripped" text.
    /// </summary>
    public int CharacterCount;
    public object Value = null!;

    public override string ToString()
    {
        return $"{VariableName} = {Value} at [{StartIndex}] for {CharacterCount} characters";
    }
}

/// <summary>
/// A single run of text within a line that shares the same set of active <see cref="InlineVariable"/>s,
/// as produced by <see cref="StyledSubstringSplitter.GetStyledSubstrings"/>.
/// </summary>
public class StyledSubstring
{
    /// <summary>
    /// The inline variables (color, font scale, custom callbacks, etc.) active over this run.
    /// </summary>
    public List<InlineVariable> Variables = new List<InlineVariable>();

    /// <summary>
    /// The text content of this run.
    /// </summary>
    public string Substring = string.Empty;

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
    // What the cached runs were built from: each inline variable and the fields the split reads. A
    // variable's Value is not part of it, because the runs only point at the variable and the draw loop
    // reads Value when it draws.
    private struct VariableSnapshot
    {
        public InlineVariable Variable;
        public string VariableName;
        public int StartIndex;
        public int CharacterCount;
    }

    private class CachedLine
    {
        public int StartOfLineIndex;
        public string Line = string.Empty;
        public List<StyledSubstring> Substrings = null!;
    }

    private VariableSnapshot[] _snapshot = new VariableSnapshot[0];
    private int _snapshotCount;
    private readonly Dictionary<int, CachedLine> _cachedLines = new();

    /// <summary>
    /// Returns the runs of line <paramref name="lineIndex"/> from a cache, so a Text that draws the same
    /// lines every frame splits them once instead of allocating the runs every frame. The cache drops a line
    /// when its text or start index differs from the one it was built for, and drops every line when any of
    /// <paramref name="inlineVariables"/> was added, removed, replaced or changed its name, start or length,
    /// so no explicit invalidation is needed when the text, a colour tag, the font or the wrap width changes.
    /// Call it for the lines of the wrapped text only (not for measuring arbitrary fragments, which is
    /// <see cref="GetStyledSubstrings(int, string, List{InlineVariable})"/>), and treat the returned list and
    /// its runs as read-only: they are shared until the line or the variables change.
    /// </summary>
    /// <param name="lineIndex">The index of the line in the wrapped text.</param>
    /// <param name="startOfLineIndex">See <see cref="GetStyledSubstrings(int, string, List{InlineVariable})"/>.</param>
    /// <param name="lineOfText">The already-wrapped, BBCode-stripped line.</param>
    /// <param name="inlineVariables">The full set of inline variables for the text.</param>
    public List<StyledSubstring> GetStyledSubstrings(int lineIndex, int startOfLineIndex, string lineOfText, List<InlineVariable> inlineVariables)
    {
        if (!SnapshotMatches(inlineVariables))
        {
            TakeSnapshot(inlineVariables);
            _cachedLines.Clear();
        }

        if (_cachedLines.TryGetValue(lineIndex, out CachedLine? cached))
        {
            if (cached.StartOfLineIndex == startOfLineIndex && string.Equals(cached.Line, lineOfText))
            {
                return cached.Substrings;
            }
        }
        else
        {
            cached = new CachedLine();
            _cachedLines[lineIndex] = cached;
        }

        cached.StartOfLineIndex = startOfLineIndex;
        cached.Line = lineOfText;
        cached.Substrings = GetStyledSubstrings(startOfLineIndex, lineOfText, inlineVariables);
        return cached.Substrings;
    }

    private bool SnapshotMatches(List<InlineVariable> inlineVariables)
    {
        if (inlineVariables.Count != _snapshotCount)
        {
            return false;
        }

        for (int i = 0; i < _snapshotCount; i++)
        {
            InlineVariable variable = inlineVariables[i];
            ref VariableSnapshot entry = ref _snapshot[i];
            if (!ReferenceEquals(variable, entry.Variable) ||
                variable.StartIndex != entry.StartIndex ||
                variable.CharacterCount != entry.CharacterCount ||
                !string.Equals(variable.VariableName, entry.VariableName))
            {
                return false;
            }
        }

        return true;
    }

    private void TakeSnapshot(List<InlineVariable> inlineVariables)
    {
        if (_snapshot.Length < inlineVariables.Count)
        {
            _snapshot = new VariableSnapshot[inlineVariables.Count];
        }

        for (int i = 0; i < inlineVariables.Count; i++)
        {
            InlineVariable variable = inlineVariables[i];
            _snapshot[i] = new VariableSnapshot
            {
                Variable = variable,
                VariableName = variable.VariableName,
                StartIndex = variable.StartIndex,
                CharacterCount = variable.CharacterCount,
            };
        }
        _snapshotCount = inlineVariables.Count;
    }

    /// <summary>
    /// Splits <paramref name="lineOfText"/> into runs according to which <paramref name="inlineVariables"/>
    /// are active over each character. Builds a new list every call; per-frame drawing uses the cached
    /// <see cref="GetStyledSubstrings(int, int, string, List{InlineVariable})"/> instead.
    /// </summary>
    /// <param name="startOfLineIndex">The index of the line's first character within the full (stripped) text, used to look up which inline variables are active.</param>
    /// <param name="lineOfText">The already-wrapped, BBCode-stripped line to split.</param>
    /// <param name="inlineVariables">The full set of inline variables for the text, keyed by absolute character index.</param>
    public List<StyledSubstring> GetStyledSubstrings(int startOfLineIndex, string lineOfText, List<InlineVariable> inlineVariables)
    {
        List<StyledSubstring> substrings = new();
        Split(startOfLineIndex, lineOfText, inlineVariables, substrings, new List<InlineVariable>(), new List<InlineVariable>(), pooled: false);
        return substrings;
    }

    // What GetStyledSubstringsReusing keeps between calls, so text split on every change allocates no run objects,
    // lists or temporary lists. Bounded by MaxPooledSubstrings.
    private const int MaxPooledSubstrings = 32;

    // The most variables a pooled run's list, or either temporary list, keeps room for once a split is over.
    internal const int MaxRetainedVariables = 16;
    private readonly List<StyledSubstring> _pooledSubstrings = new();
    private readonly List<InlineVariable> _currentlyActiveInlines = new();
    private readonly List<InlineVariable> _inlinesForThisCharacter = new();

    /// <summary>
    /// <see cref="GetStyledSubstrings(int, string, List{InlineVariable})"/> into <paramref name="substrings"/>, which is
    /// cleared first, with the run objects reused from the previous call. The runs are valid until the next call on
    /// this splitter (a Text that changes every frame splits the line again before drawing it).
    /// </summary>
    internal void GetStyledSubstringsReusing(int startOfLineIndex, string lineOfText, List<InlineVariable> inlineVariables, List<StyledSubstring> substrings)
    {
        substrings.Clear();
        _currentlyActiveInlines.Clear();
        _inlinesForThisCharacter.Clear();
        Split(startOfLineIndex, lineOfText, inlineVariables, substrings, _currentlyActiveInlines, _inlinesForThisCharacter, pooled: true);
        _currentlyActiveInlines.Clear();
        _inlinesForThisCharacter.Clear();
    }

    /// <summary>
    /// Drops what the last <see cref="GetStyledSubstringsReusing"/> left in the pooled runs (their variables and text), and
    /// the room their lists and the temporary lists grew past <see cref="MaxRetainedVariables"/>, so a deeply nested text
    /// does not leave its depth behind. The runs it returned are not valid afterwards.
    /// </summary>
    internal void ReleaseSplit()
    {
        foreach (StyledSubstring substring in _pooledSubstrings)
        {
            substring.Substring = string.Empty;
            ReleaseVariables(substring.Variables);
        }

        ReleaseVariables(_currentlyActiveInlines);
        ReleaseVariables(_inlinesForThisCharacter);
    }

    private static void ReleaseVariables(List<InlineVariable> variables)
    {
        variables.Clear();
        if (variables.Capacity > MaxRetainedVariables)
        {
            variables.Capacity = 0;
        }
    }

    /// <summary>How many variables the pooled runs and the temporary lists still point at.</summary>
    internal int RetainedVariableCount
    {
        get
        {
            int count = _currentlyActiveInlines.Count + _inlinesForThisCharacter.Count;
            foreach (StyledSubstring substring in _pooledSubstrings)
            {
                count += substring.Variables.Count;
            }
            return count;
        }
    }

    /// <summary>The largest array any pooled run's variable list or a temporary list holds on to.</summary>
    internal int RetainedVariableCapacity
    {
        get
        {
            int capacity = System.Math.Max(_currentlyActiveInlines.Capacity, _inlinesForThisCharacter.Capacity);
            foreach (StyledSubstring substring in _pooledSubstrings)
            {
                capacity = System.Math.Max(capacity, substring.Variables.Capacity);
            }
            return capacity;
        }
    }

    private StyledSubstring CreateSubstring(List<StyledSubstring> substrings, bool pooled)
    {
        if (!pooled || substrings.Count >= MaxPooledSubstrings)
        {
            return new StyledSubstring();
        }

        if (substrings.Count == _pooledSubstrings.Count)
        {
            _pooledSubstrings.Add(new StyledSubstring());
        }

        StyledSubstring reused = _pooledSubstrings[substrings.Count];
        reused.Variables.Clear();
        reused.Substring = string.Empty;
        return reused;
    }

    private void Split(int startOfLineIndex, string lineOfText, List<InlineVariable> inlineVariables, List<StyledSubstring> substrings,
        List<InlineVariable> currentlyActiveInlines, List<InlineVariable> inlinesForThisCharacter, bool pooled)
    {
        int currentSubstringStart = 0;

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

                var styledSubstring = CreateSubstring(substrings, pooled);
                foreach (var item in inlinesForThisCharacter)
                {
                    // Custom variables can stack (e.g. [Custom=A][Custom=B]),
                    // so allow multiple with the same VariableName. Other variables
                    // like Color should replace the previous value.
                    if (item.VariableName != "Custom")
                    {
                        for (int i = 0; i < styledSubstring.Variables.Count; i++)
                        {
                            if (styledSubstring.Variables[i].VariableName == item.VariableName)
                            {
                                styledSubstring.Variables.RemoveAt(i);
                                break;
                            }
                        }
                    }
                    styledSubstring.Variables.Add(item);
                }

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
    }
}
