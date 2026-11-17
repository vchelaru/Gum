using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Shouldly;

namespace Gum.Presentation.Tests.Architecture;

/// <summary>
/// A test that builds <c>NewProjectDialogViewModel</c> without naming <c>runChecksInline</c> gets the app's pool-thread
/// checks, whose continuation mutates the view model while the test reads it (#7710, #7903, #8200). Every construction
/// in a test must say which it wants: <c>runChecksInline: true</c>, or <c>false</c> where the test is about a check
/// that outlives a delay. Limitation: a target-typed <c>new(...)</c> is not seen.
/// </summary>
public class NewProjectViewModelTestConstructionGuardTests
{
    private static readonly Regex Construction = new Regex(@"\bnew\s+(?:[A-Za-z_]\w*\s*\.\s*)*NewProjectDialogViewModel\s*\(");

    [Fact]
    public void EveryTestConstruction_NamesRunChecksInline()
    {
        string repoRoot = FindRepoRoot();
        List<string> offenders = new List<string>();

        foreach (string testRoot in new[] { "Gum.Presentation.Tests", "Gum.Avalonia.Tests" })
        {
            foreach (string file in Directory.EnumerateFiles(Path.Combine(repoRoot, "Tests", testRoot), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                    || file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                    || Path.GetFileName(file) == nameof(NewProjectViewModelTestConstructionGuardTests) + ".cs")
                {
                    continue;
                }

                string relative = Path.GetRelativePath(repoRoot, file).Replace((char)92, '/');
                offenders.AddRange(FindUnflaggedConstructions(File.ReadAllText(file)).Select(line => relative + ":" + line));
            }
        }

        offenders.ShouldBeEmpty(
            "These constructions do not name runChecksInline, so their checks run on the pool and can race the test:" +
            Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void Scan_FlagsAConstructionWithoutTheFlag_AcrossLines()
    {
        FindUnflaggedConstructions("var a = 1;\nvar v = new NewProjectDialogViewModel(\n  x,\n  y);").ShouldBe(new[] { 2 });
    }

    [Fact]
    public void Scan_AcceptsTheFlagTrueOrFalse()
    {
        FindUnflaggedConstructions("new NewProjectDialogViewModel(a, runChecksInline: true); new NewProjectDialogViewModel(a, runChecksInline: false);")
            .ShouldBeEmpty();
    }

    [Fact]
    public void Scan_FlagsAFullyQualifiedConstruction()
    {
        FindUnflaggedConstructions("new Gum.Dialogs.NewProjectDialogViewModel(a);").ShouldBe(new[] { 1 });
    }

    [Fact]
    public void Scan_IgnoresAConstructionInAComment_OrAString()
    {
        FindUnflaggedConstructions("// new NewProjectDialogViewModel(a)\n/* new NewProjectDialogViewModel(a) */\nvar s = \"new NewProjectDialogViewModel(a)\";")
            .ShouldBeEmpty();
    }

    [Fact]
    public void Scan_DoesNotCountTheFlagNamedOnlyInAComment()
    {
        FindUnflaggedConstructions("new NewProjectDialogViewModel(a /* runChecksInline: true */);").ShouldBe(new[] { 1 });
    }

    private static List<int> FindUnflaggedConstructions(string source)
    {
        string code = StripCommentsAndStrings(source);
        List<int> lines = new List<int>();
        foreach (Match match in Construction.Matches(code))
        {
            int start = match.Index + match.Length;
            int depth = 1;
            int i = start;
            for (; i < code.Length && depth > 0; i++)
            {
                if (code[i] == '(') depth++;
                else if (code[i] == ')') depth--;
            }

            if (!code.Substring(start, i - start).Contains("runChecksInline"))
            {
                lines.Add(code.Take(match.Index).Count(c => c == '\n') + 1);
            }
        }
        return lines;
    }

    // Blanks comments and the contents of string and char literals, keeping newlines so line numbers hold.
    // Does not handle interpolated or raw strings specially; good enough for a ctor argument list.
    private static string StripCommentsAndStrings(string s)
    {
        StringBuilder sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            char next = i + 1 < s.Length ? s[i + 1] : '\0';
            if (c == '/' && next == '/')
            {
                while (i < s.Length && s[i] != '\n') i++;
                i--;
            }
            else if (c == '/' && next == '*')
            {
                i += 2;
                while (i < s.Length && !(s[i] == '*' && i + 1 < s.Length && s[i + 1] == '/'))
                {
                    if (s[i] == '\n') sb.Append('\n');
                    i++;
                }
                i++;
            }
            else if (c == '"' || c == '\'')
            {
                bool verbatim = c == '"' && i > 0 && (s[i - 1] == '@' || (i > 1 && s[i - 1] == '$' && s[i - 2] == '@') || (i > 1 && s[i - 1] == '@' && s[i - 2] == '$'));
                i++;
                while (i < s.Length)
                {
                    if (s[i] == '\n') sb.Append('\n');
                    if (!verbatim && s[i] == (char)92) { i += 2; continue; }
                    if (s[i] == c)
                    {
                        if (verbatim && i + 1 < s.Length && s[i + 1] == c) { i += 2; continue; }
                        break;
                    }
                    i++;
                }
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Tests")) && File.Exists(Path.Combine(current, "Gum.slnx")))
            {
                return current;
            }
            current = Path.GetDirectoryName(current)!;
        }
        throw new InvalidOperationException("Repo root not found.");
    }
}
