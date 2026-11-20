using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gum.Cli.Commands;

/// <summary>
/// Collects the bitmap font files the runtime reports as unresolvable ("No usable font could be resolved
/// for '...'") during a render, so <c>gumcli screenshot</c> can end with one hint to run
/// <c>gumcli fonts</c>. Fed from the runtime's <c>PropertyAssignmentError</c> event, which covers exactly
/// the fonts this render needed, including ones a <c>--set</c> override asked for.
/// </summary>
public sealed class MissingFontHint
{
    private const int MaxFilesNamed = 5;

    private static readonly Regex MissingFontMessage =
        new Regex(@"No usable font could be resolved for '([^']+)'", RegexOptions.Compiled);

    private readonly object _lock = new object();
    private readonly List<string> _files = new List<string>();

    /// <summary>The distinct missing font files reported so far, in the order first reported.</summary>
    public IReadOnlyList<string> Files
    {
        get
        {
            lock (_lock)
            {
                return _files.ToList();
            }
        }
    }

    /// <summary>Records <paramref name="message"/> when it reports a missing font; ignores any other message.</summary>
    public void Add(string message)
    {
        Match match = MissingFontMessage.Match(message);
        if (!match.Success)
        {
            return;
        }

        lock (_lock)
        {
            if (!_files.Contains(match.Groups[1].Value))
            {
                _files.Add(match.Groups[1].Value);
            }
        }
    }

    /// <summary>
    /// Builds the one-line hint telling the user to run <c>gumcli fonts</c> on <paramref name="projectPath"/>.
    /// </summary>
    public static string BuildHint(IReadOnlyList<string> missingFonts, string projectPath, bool hasSetOverrides = false)
    {
        string named = string.Join(", ", missingFonts.Take(MaxFilesNamed));
        if (missingFonts.Count > MaxFilesNamed)
        {
            named += $" and {missingFonts.Count - MaxFilesNamed} more";
        }

        string noun = missingFonts.Count == 1 ? "font file is" : "font files are";
        string hint = $"hint: {missingFonts.Count} {noun} missing from FontCache ({named}), so text drew in the default font. "
            + $"Run `gumcli fonts \"{projectPath}\"` to generate them, then screenshot again.";

        // `gumcli fonts` bakes what the saved project uses; a --set override is not saved, so its size is not covered.
        if (hasSetOverrides)
        {
            hint += " A size that only --set uses also needs `--font <name> --size <n>` on that command.";
        }

        return hint;
    }
}
