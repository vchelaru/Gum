using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GumToolUnitTests.Coupling;

/// <summary>
/// Number of <c>.Self</c> static-singleton references found in a chunk of source,
/// split so that the sanctioned <c>ObjectFinder.Self</c> singleton can be excluded
/// from the ratcheted burn-down number.
/// </summary>
public readonly record struct SelfReferenceCount(int Total, int ObjectFinder)
{
    /// <summary>
    /// The ratcheted metric: every <c>.Self</c> reference except the sanctioned
    /// <c>ObjectFinder.Self</c> singleton (see repo CLAUDE.md), which will never be removed.
    /// </summary>
    public int ExcludingObjectFinder => Total - ObjectFinder;
}

/// <summary>
/// Aggregated coupling metrics for a whole tool source tree. This is the canonical,
/// deterministic measurement that the Phase 0 ratchet test guards.
/// </summary>
public sealed class CouplingMetrics
{
    /// <summary>Total <c>.Self</c> references across all scanned files (includes ObjectFinder).</summary>
    public int SelfTotal { get; }

    /// <summary>The sanctioned <c>ObjectFinder.Self</c> sub-count, excluded from the ratchet.</summary>
    public int ObjectFinderSelf { get; }

    /// <summary>Ratcheted <c>.Self</c> metric (#1): total minus <c>ObjectFinder.Self</c>.</summary>
    public int SelfExcludingObjectFinder => SelfTotal - ObjectFinderSelf;

    /// <summary>Ratcheted metric (#2): WPF (<c>System.Windows.*</c>) coupling tokens inside ViewModels.</summary>
    public int WindowsCouplingInViewModels { get; }

    /// <summary>Ratcheted metric (#3): inline dialog sites bypassing IDialogService.</summary>
    public int InlineDialogSites { get; }

    /// <summary>Ratcheted metric (#4): WinForms/WPF type references inside interface bodies.</summary>
    public int InterfaceTypeLeaks { get; }

    /// <summary>Count of <c>.cs</c> files scanned (excludes bin/obj). For the dashboard only.</summary>
    public int FilesScanned { get; }

    /// <summary>Count of ViewModel <c>.cs</c> files scanned. For the dashboard only.</summary>
    public int ViewModelFilesScanned { get; }

    public CouplingMetrics(
        int selfTotal,
        int objectFinderSelf,
        int windowsCouplingInViewModels,
        int inlineDialogSites,
        int interfaceTypeLeaks,
        int filesScanned,
        int viewModelFilesScanned)
    {
        SelfTotal = selfTotal;
        ObjectFinderSelf = objectFinderSelf;
        WindowsCouplingInViewModels = windowsCouplingInViewModels;
        InlineDialogSites = inlineDialogSites;
        InterfaceTypeLeaks = interfaceTypeLeaks;
        FilesScanned = filesScanned;
        ViewModelFilesScanned = viewModelFilesScanned;
    }
}

/// <summary>
/// Source-scanning coupling-metrics scanner for the Gum tool. The methods on this class
/// are the single canonical, deterministic definition of each Phase 0 coupling metric;
/// there is no separate grep-based method that could diverge from it.
/// </summary>
/// <remarks>
/// All metrics are raw textual scans (comments and string literals are NOT stripped) for
/// maximum determinism and simplicity, with one exception that genuinely needs structure:
/// metric #4 isolates <c>interface</c> bodies with a comment/string-aware brace matcher.
/// For a ratchet (assert count &lt;= baseline) any small over-count from a commented-out
/// occurrence is conservative: it can never let a real regression slip through, it can only
/// make the guard marginally looser by a constant.
/// </remarks>
public sealed class CouplingScanner
{
    // Metric #1 — .Self static-singleton references.
    // `\.Self\b` matches `.Self` only when not followed by another identifier char, so
    // `.SelfManager` does not count. The ObjectFinder sub-count is a strict subset.
    private readonly Regex _selfTotalRegex;
    private readonly Regex _objectFinderSelfRegex;

    // Metric #2 — System.Windows.* coupling in ViewModels.
    // (A) every `using System.Windows*;` import line, plus
    // (B) every whole-word occurrence of an unambiguous WPF type the VMs use unqualified.
    // (A) and (B) never cover the same characters, so there is no double-counting.
    // Ambiguous simple names (Color/Point/Size/Key/Control) are deliberately excluded
    // because they collide with XNA / System.Drawing / Gum / WinForms types.
    private readonly Regex _windowsUsingRegex;
    private readonly Regex _wpfTypeInViewModelRegex;

    // Metric #3 — inline dialog sites bypassing IDialogService.
    private readonly Regex _messageBoxRegex;
    private readonly Regex _windowConstructionRegex;

    // Metric #4 — WinForms/WPF type references inside interface bodies.
    // Group 1 catches any fully-qualified `System.Windows.*` reference. Group 2 catches a
    // documented allow-list of distinctive WPF/WinForms types used unqualified; the
    // negative lookbehind `(?<![\w.])` keeps it from also matching the qualified form
    // (which group 1 already counts), so there is no double-counting.
    private readonly Regex _interfaceQualifiedLeakRegex;
    private readonly Regex _interfaceUnqualifiedLeakRegex;

    // ViewModel filename rule: `*ViewModel.cs` or partial `*ViewModel.*.cs`.
    private readonly Regex _viewModelFileRegex;

    public CouplingScanner()
    {
        _selfTotalRegex = new Regex(@"\.Self\b", RegexOptions.Compiled);
        _objectFinderSelfRegex = new Regex(@"\bObjectFinder\.Self\b", RegexOptions.Compiled);

        _windowsUsingRegex = new Regex(
            @"^[ \t]*using[ \t]+System\.Windows",
            RegexOptions.Compiled | RegexOptions.Multiline);
        _wpfTypeInViewModelRegex = new Regex(
            @"\b(Visibility|Brushes|SolidColorBrush|Brush|Application|Dispatcher|RoutedEventArgs|BitmapImage|Thickness|FrameworkElement)\b",
            RegexOptions.Compiled);

        _messageBoxRegex = new Regex(@"MessageBox\.Show", RegexOptions.Compiled);
        _windowConstructionRegex = new Regex(@"new\s+[\w.]*Window\s*\(", RegexOptions.Compiled);

        _interfaceQualifiedLeakRegex = new Regex(@"System\.Windows\.", RegexOptions.Compiled);
        _interfaceUnqualifiedLeakRegex = new Regex(
            @"(?<![\w.])(FrameworkElement|FrameworkContentElement|UIElement|Spinner|DragEventArgs|KeyPressEventArgs|MouseEventArgs)\b",
            RegexOptions.Compiled);

        _viewModelFileRegex = new Regex(@"ViewModel(\.[^\\/]+)?\.cs$", RegexOptions.Compiled);
    }

    /// <summary>
    /// Metric #1. Counts <c>.Self</c> static-singleton references and the
    /// <c>ObjectFinder.Self</c> sub-count in a single file's source text.
    /// </summary>
    public SelfReferenceCount CountSelfReferences(string source)
    {
        int total = _selfTotalRegex.Matches(source).Count;
        int objectFinder = _objectFinderSelfRegex.Matches(source).Count;
        return new SelfReferenceCount(total, objectFinder);
    }

    /// <summary>
    /// Metric #2. Counts WPF coupling tokens in a single ViewModel's source text:
    /// every <c>using System.Windows*;</c> line plus every whole-word allow-list WPF type.
    /// Only call this on files for which <see cref="IsViewModelFile"/> is true.
    /// </summary>
    public int CountWindowsCouplingInViewModel(string source)
    {
        int usingLines = _windowsUsingRegex.Matches(source).Count;
        int wpfTypes = _wpfTypeInViewModelRegex.Matches(source).Count;
        return usingLines + wpfTypes;
    }

    /// <summary>
    /// Metric #3. Counts inline dialog sites (<c>MessageBox.Show</c> and
    /// <c>new *Window(</c>) in a single file's source text.
    /// </summary>
    public int CountInlineDialogSites(string source)
    {
        int messageBoxes = _messageBoxRegex.Matches(source).Count;
        int windowConstructions = _windowConstructionRegex.Matches(source).Count;
        return messageBoxes + windowConstructions;
    }

    /// <summary>
    /// Metric #4. Counts WinForms/WPF type references that appear inside
    /// <c>interface</c> bodies in a single file's source text.
    /// </summary>
    public int CountInterfaceTypeLeaks(string source)
    {
        int count = 0;
        foreach (string body in ExtractInterfaceBodies(source))
        {
            count += _interfaceQualifiedLeakRegex.Matches(body).Count;
            count += _interfaceUnqualifiedLeakRegex.Matches(body).Count;
        }
        return count;
    }

    /// <summary>
    /// True when <paramref name="fileName"/> is a ViewModel file by the documented
    /// filename rule (<c>*ViewModel.cs</c> or partial <c>*ViewModel.*.cs</c>).
    /// </summary>
    public bool IsViewModelFile(string fileName)
    {
        return _viewModelFileRegex.IsMatch(fileName);
    }

    /// <summary>
    /// True when <paramref name="relativePath"/> is part of the dialog infrastructure
    /// (<c>Services/Dialogs/</c>), which is excluded from the inline-dialog metric.
    /// </summary>
    public bool IsDialogInfrastructure(string relativePath)
    {
        string normalized = relativePath.Replace('\\', '/');
        return normalized.Contains("Services/Dialogs/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Walks every <c>.cs</c> file under <paramref name="toolSourceRoot"/> (excluding
    /// <c>bin</c>/<c>obj</c>) and aggregates the four coupling metrics.
    /// </summary>
    public CouplingMetrics ScanToolSource(string toolSourceRoot)
    {
        if (!Directory.Exists(toolSourceRoot))
        {
            throw new DirectoryNotFoundException(
                $"Tool source root not found: '{toolSourceRoot}'. " +
                "The coupling ratchet could not locate the Gum tool source tree.");
        }

        int selfTotal = 0;
        int objectFinderSelf = 0;
        int windowsCoupling = 0;
        int inlineDialogSites = 0;
        int interfaceLeaks = 0;
        int filesScanned = 0;
        int viewModelFilesScanned = 0;

        foreach (string path in Directory.EnumerateFiles(toolSourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(toolSourceRoot, path);
            if (IsInBinOrObj(relativePath))
            {
                continue;
            }

            string source = File.ReadAllText(path);
            filesScanned++;

            SelfReferenceCount self = CountSelfReferences(source);
            selfTotal += self.Total;
            objectFinderSelf += self.ObjectFinder;

            interfaceLeaks += CountInterfaceTypeLeaks(source);

            if (!IsDialogInfrastructure(relativePath))
            {
                inlineDialogSites += CountInlineDialogSites(source);
            }

            if (IsViewModelFile(Path.GetFileName(path)))
            {
                viewModelFilesScanned++;
                windowsCoupling += CountWindowsCouplingInViewModel(source);
            }
        }

        return new CouplingMetrics(
            selfTotal,
            objectFinderSelf,
            windowsCoupling,
            inlineDialogSites,
            interfaceLeaks,
            filesScanned,
            viewModelFilesScanned);
    }

    private bool IsInBinOrObj(string relativePath)
    {
        string[] segments = relativePath.Split('/', '\\');
        foreach (string segment in segments)
        {
            if (segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("obj", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns the text of every <c>interface</c> body in <paramref name="source"/>, brace-matched
    /// while skipping comments and string/char literals so braces inside them do not throw off
    /// depth tracking. Interpolated strings (<c>$"{x}"</c>) are treated as a sequence of opaque
    /// <c>"..."</c> segments rather than being parsed structurally.
    /// <para>
    /// KNOWN LIMITATION (under-counts -- the dangerous direction): a nested string literal inside
    /// an interpolation hole that itself contains a <c>}</c> (e.g. <c>$"{(x ? "a}" : "b")}"</c>)
    /// makes the matcher mistake a brace for code, prematurely closing the interface body. Any leak
    /// declared AFTER such a default-interface-method body is then outside the captured body and is
    /// MISSED. This is immaterial today because Gum's interfaces are signature-only (no default
    /// method bodies, so no interpolated strings), and hardening the parser is overkill for Phase 0;
    /// the limitation is pinned by a characterization test in CouplingScannerTests.
    /// </para>
    /// </summary>
    private List<string> ExtractInterfaceBodies(string source)
    {
        List<string> bodies = new List<string>();

        int i = 0;
        int length = source.Length;
        bool seekingBrace = false;
        bool inBody = false;
        int depth = 0;
        int bodyStart = -1;

        while (i < length)
        {
            char c = source[i];

            // Line comment.
            if (c == '/' && i + 1 < length && source[i + 1] == '/')
            {
                i += 2;
                while (i < length && source[i] != '\n')
                {
                    i++;
                }
                continue;
            }

            // Block comment.
            if (c == '/' && i + 1 < length && source[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < length && !(source[i] == '*' && source[i + 1] == '/'))
                {
                    i++;
                }
                i += 2;
                continue;
            }

            // Verbatim string @"..." (doubled "" is an escaped quote).
            if (c == '@' && i + 1 < length && source[i + 1] == '"')
            {
                i += 2;
                while (i < length)
                {
                    if (source[i] == '"')
                    {
                        if (i + 1 < length && source[i + 1] == '"')
                        {
                            i += 2;
                            continue;
                        }
                        i++;
                        break;
                    }
                    i++;
                }
                continue;
            }

            // Regular (and interpolated) string "...".
            if (c == '"')
            {
                i++;
                while (i < length)
                {
                    if (source[i] == '\\')
                    {
                        i += 2;
                        continue;
                    }
                    if (source[i] == '"')
                    {
                        i++;
                        break;
                    }
                    i++;
                }
                continue;
            }

            // Char literal '.'.
            if (c == '\'')
            {
                i++;
                while (i < length)
                {
                    if (source[i] == '\\')
                    {
                        i += 2;
                        continue;
                    }
                    if (source[i] == '\'')
                    {
                        i++;
                        break;
                    }
                    i++;
                }
                continue;
            }

            // `interface` keyword in code (not while already capturing a body).
            if (!inBody && c == 'i' && MatchesKeywordAt(source, i, "interface"))
            {
                seekingBrace = true;
                i += "interface".Length;
                continue;
            }

            if (c == '{')
            {
                if (inBody)
                {
                    depth++;
                }
                else if (seekingBrace)
                {
                    inBody = true;
                    seekingBrace = false;
                    depth = 1;
                    bodyStart = i + 1;
                }
                i++;
                continue;
            }

            if (c == '}')
            {
                if (inBody)
                {
                    depth--;
                    if (depth == 0)
                    {
                        bodies.Add(source.Substring(bodyStart, i - bodyStart));
                        inBody = false;
                        bodyStart = -1;
                    }
                }
                i++;
                continue;
            }

            i++;
        }

        return bodies;
    }

    private bool MatchesKeywordAt(string source, int index, string keyword)
    {
        if (index + keyword.Length > source.Length)
        {
            return false;
        }
        if (string.CompareOrdinal(source, index, keyword, 0, keyword.Length) != 0)
        {
            return false;
        }
        if (index > 0 && IsWordChar(source[index - 1]))
        {
            return false;
        }
        int afterIndex = index + keyword.Length;
        if (afterIndex < source.Length && IsWordChar(source[afterIndex]))
        {
            return false;
        }
        return true;
    }

    private bool IsWordChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }
}
