using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shouldly;

namespace Gum.Presentation.Tests.Architecture;

/// <summary>
/// A test that builds <c>NewProjectDialogViewModel</c> without naming <c>runChecksInline</c> gets the app's pool-thread
/// checks, whose continuation mutates the view model while the test reads it (#7710, #7903, #8200). Every construction
/// in a test must say which it wants: <c>runChecksInline: true</c>, or <c>false</c> where the test is about a check
/// that outlives a delay.
/// </summary>
public class NewProjectViewModelTestConstructionGuardTests
{
    private const string Construction = "new NewProjectDialogViewModel(";

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

                string text = File.ReadAllText(file);
                int index = text.IndexOf(Construction, StringComparison.Ordinal);
                while (index >= 0)
                {
                    int open = index + Construction.Length;
                    string arguments = ArgumentsFrom(text, open);
                    if (!arguments.Contains("runChecksInline"))
                    {
                        int line = text.Take(index).Count(c => c == '\n') + 1;
                        offenders.Add(Path.GetRelativePath(repoRoot, file).Replace('\\', '/') + ":" + line);
                    }
                    index = text.IndexOf(Construction, open, StringComparison.Ordinal);
                }
            }
        }

        offenders.ShouldBeEmpty(
            "These constructions do not name runChecksInline, so their checks run on the pool and can race the test:" +
            Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    // The text between the open paren (already consumed) and its matching close paren.
    private static string ArgumentsFrom(string text, int start)
    {
        int depth = 1;
        int i = start;
        for (; i < text.Length && depth > 0; i++)
        {
            if (text[i] == '(') depth++;
            else if (text[i] == ')') depth--;
        }
        return text.Substring(start, i - start);
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
