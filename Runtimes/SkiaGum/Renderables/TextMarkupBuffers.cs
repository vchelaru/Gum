using System.Collections.Generic;
using System.Text;
using Gum.Wireframe;
using RenderingLibrary.Graphics;

namespace SkiaGum;

/// <summary>
/// The lists, arrays and run objects a <see cref="Text"/> reuses each time its text or reveal changes, so building the
/// styled runs of changing text allocates only what the layout itself needs (#7846). Each buffer is created when first
/// used and gives its array back once it has grown past a bound, so a Text keeps only what its usual text needs.
/// </summary>
internal sealed class TextMarkupBuffers
{
    // What a buffer keeps once a parse or a build has finished, however large the text it held was.
    internal const int MaxRetainedRuns = 64;
    internal const int MaxRetainedTags = 32;
    internal const int MaxRetainedBuilderCapacity = 1024;
    internal const int MaxPooledInlineVariables = 64;
    internal const int MaxRetainedSubstrings = 64;

    private List<Text.StyledTextRun>? _runs;
    private List<Text.StyledTextRun>? _fittedRuns;
    private List<Text.StyledTextRun>? _lineRuns;
    private List<Text.StyledTextRun>? _visibleSourceRuns;
    private List<Text.StyledTextRun>? _visibleFittedRuns;
    private List<Text.StyledTextRun>? _visibleRuns;
    private List<int>? _visibleLineLengths;
    private List<FoundTag>? _tags;
    private List<BbCodeParser.Tag>? _activeTags;
    private StringBuilder? _builder;
    private List<StyledSubstring>? _substrings;
    private StyledSubstringSplitter? _splitter;
    private List<InlineVariable>? _inlineVariablePool;

    /// <summary>The runs of the full text, laid out into the block a Text keeps.</summary>
    public List<Text.StyledTextRun> Runs => _runs ??= new List<Text.StyledTextRun>();

    /// <summary><see cref="Runs"/> split at the line breaks of a first layout.</summary>
    public List<Text.StyledTextRun> FittedRuns => _fittedRuns ??= new List<Text.StyledTextRun>();

    /// <summary>The runs of the line being fit.</summary>
    public List<Text.StyledTextRun> LineRuns => _lineRuns ??= new List<Text.StyledTextRun>();

    /// <summary>The runs of the full text a reveal slices.</summary>
    public List<Text.StyledTextRun> VisibleSourceRuns => _visibleSourceRuns ??= new List<Text.StyledTextRun>();

    /// <summary><see cref="VisibleSourceRuns"/> split at the line breaks of the full layout.</summary>
    public List<Text.StyledTextRun> VisibleFittedRuns => _visibleFittedRuns ??= new List<Text.StyledTextRun>();

    /// <summary>The runs a reveal lays out.</summary>
    public List<Text.StyledTextRun> VisibleRuns => _visibleRuns ??= new List<Text.StyledTextRun>();

    /// <summary>How many chars of each wrapped line a reveal shows.</summary>
    public List<int> VisibleLineLengths => _visibleLineLengths ??= new List<int>();

    /// <summary>The tags found in the last parse; what <see cref="Text.ParsedBbCodeTags"/> returns.</summary>
    public List<FoundTag> Tags => _tags ??= new List<FoundTag>();

    /// <summary>The stack of open tags the parser fills.</summary>
    public List<BbCodeParser.Tag> ActiveTags => _activeTags ??= new List<BbCodeParser.Tag>();

    /// <summary>The array the parser sorts the tags in while removing them.</summary>
    public TagInfo[]? SortScratch;

    /// <summary>The builder the parser removes the tags from.</summary>
    public StringBuilder Builder => _builder ??= new StringBuilder();

    /// <summary>The runs the splitter produced for the text; valid until the next split.</summary>
    public List<StyledSubstring> Substrings => _substrings ??= new List<StyledSubstring>();

    /// <summary>How many tags the last parse left in <see cref="Tags"/>; reading it creates nothing.</summary>
    internal int RetainedTagCount => _tags?.Count ?? 0;

    /// <summary>The array size <see cref="Tags"/> holds on to.</summary>
    internal int RetainedTagCapacity => _tags?.Capacity ?? 0;

    /// <summary>How many runs <see cref="Substrings"/> still points at.</summary>
    internal int RetainedSubstringCount => _substrings?.Count ?? 0;

    /// <summary>The array size <see cref="Substrings"/> holds on to.</summary>
    internal int RetainedSubstringCapacity => _substrings?.Capacity ?? 0;

    public StyledSubstringSplitter Splitter => _splitter ??= new StyledSubstringSplitter();

    /// <summary>
    /// The inline variable object for position <paramref name="index"/> of the Text's variables, kept from the last parse
    /// so a new parse sets its fields instead of building one. Past <see cref="MaxPooledInlineVariables"/> a new one is returned.
    /// </summary>
    public InlineVariable GetInlineVariable(int index)
    {
        if (index >= MaxPooledInlineVariables)
        {
            return new InlineVariable();
        }

        _inlineVariablePool ??= new List<InlineVariable>();
        while (_inlineVariablePool.Count <= index)
        {
            _inlineVariablePool.Add(new InlineVariable());
        }

        return _inlineVariablePool[index];
    }

    /// <summary>Drops what the last block build left, and the arrays it grew past the bound.</summary>
    public void ReleaseBlockRuns()
    {
        Release(_runs);
        Release(_fittedRuns);
        Release(_lineRuns);
    }

    /// <summary>Drops what the last reveal build left, and the arrays it grew past the bound.</summary>
    public void ReleaseVisibleRuns()
    {
        Release(_visibleSourceRuns);
        Release(_visibleFittedRuns);
        Release(_visibleRuns);
        if (_visibleLineLengths != null && _visibleLineLengths.Capacity > MaxRetainedRuns)
        {
            _visibleLineLengths.Clear();
            _visibleLineLengths.Capacity = 0;
        }
    }

    /// <summary>
    /// Brings everything the parse and the split of the text keep down to its bound: the arrays they grew past it, and every
    /// reference they hold into the text. The tags of the text now set stay, since <see cref="Text.ParsedBbCodeTags"/> is
    /// those. Called wherever a parse or a split ends, so the worst case a Text keeps is the same whatever text it held before.
    /// The run lists are not here because a build is still using them when a split ends; they are released by the build.
    /// </summary>
    public void TrimToBounds()
    {
        ReleaseParse();
        ReleaseSplit();
    }

    /// <summary><see cref="TrimToBounds"/> for a text that has no tags (plain, empty, or none it recognizes): the tags of the last parse go too.</summary>
    public void DiscardParse()
    {
        _tags?.Clear();
        TrimToBounds();
    }

    /// <summary>Drops the runs of the last split and the lists and strings they and the splitter hold, past the bound.</summary>
    public void ReleaseSplit()
    {
        if (_substrings != null)
        {
            _substrings.Clear();
            if (_substrings.Capacity > MaxRetainedSubstrings)
            {
                _substrings.Capacity = 0;
            }
        }

        _splitter?.ReleaseSplit();
    }

    /// <summary>Drops the arrays the last parse grew past the bound.</summary>
    public void ReleaseParse()
    {
        if (_activeTags != null && _activeTags.Capacity > MaxRetainedTags)
        {
            _activeTags.Clear();
            _activeTags.Capacity = 0;
        }

        if (_tags != null && _tags.Capacity > MaxRetainedTags)
        {
            _tags.Capacity = _tags.Count;
        }

        if (SortScratch != null && SortScratch.Length > MaxRetainedTags * 2)
        {
            SortScratch = null;
        }

        if (_builder != null && _builder.Capacity > MaxRetainedBuilderCapacity)
        {
            _builder.Clear();
            _builder.Capacity = 16;
        }
    }

    private static void Release(List<Text.StyledTextRun>? runs)
    {
        if (runs == null)
        {
            return;
        }

        runs.Clear();
        if (runs.Capacity > MaxRetainedRuns)
        {
            runs.Capacity = 0;
        }
    }
}
