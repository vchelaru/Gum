using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Gum.Cli.Commands;

/// <summary>
/// Wraps <see cref="System.Console.Error"/> during a render and records the bitmap font files the
/// runtime reports as unresolvable ("No usable font could be resolved for '...'"), so the command can
/// end with one hint to run <c>gumcli fonts</c>. Every line still passes through unchanged. Watching the
/// report, instead of re-checking the project's fonts, covers every backend that logs it and exactly the
/// fonts this render needed, including ones a <c>--set</c> override asked for.
/// </summary>
public sealed class MissingFontHintWriter : TextWriter
{
    private const int MaxFilesNamed = 5;

    private static readonly Regex MissingFontLine =
        new Regex(@"No usable font could be resolved for '([^']+)'", RegexOptions.Compiled);

    private readonly TextWriter _inner;
    private readonly StringBuilder _line = new StringBuilder();
    private readonly List<string> _missingFonts = new List<string>();

    /// <summary>Wraps <paramref name="inner"/>, which receives everything written.</summary>
    public MissingFontHintWriter(TextWriter inner)
    {
        _inner = inner;
    }

    /// <inheritdoc/>
    public override Encoding Encoding => _inner.Encoding;

    /// <summary>The distinct missing font files reported so far, in the order first reported.</summary>
    public IReadOnlyList<string> MissingFonts => _missingFonts;

    /// <inheritdoc/>
    public override void Write(char value)
    {
        _inner.Write(value);

        if (value != '\n')
        {
            _line.Append(value);
            return;
        }

        Match match = MissingFontLine.Match(_line.ToString());
        _line.Clear();
        if (match.Success && !_missingFonts.Contains(match.Groups[1].Value))
        {
            _missingFonts.Add(match.Groups[1].Value);
        }
    }

    /// <inheritdoc/>
    public override void Flush() => _inner.Flush();

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
        string hint = $"hint: {missingFonts.Count} {noun} missing from the project's FontCache, so text drew in the default font "
            + $"({named}). Run `gumcli fonts \"{projectPath}\"` to generate them, then screenshot again.";

        // `gumcli fonts` bakes what the saved project uses; a --set override is not saved, so its font is not covered.
        if (hasSetOverrides)
        {
            hint += " A font or size that only a --set override uses is not in the project: "
                + $"generate it with `gumcli fonts \"{projectPath}\" --font <name> --size <n>`.";
        }

        return hint;
    }
}
