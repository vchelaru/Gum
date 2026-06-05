using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Shouldly;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Coverage guard for the draw-call counter. Every raylib state change that flushes the render
/// batch (blend / scissor / render-target / shader / 2D-mode) must go through
/// <c>BatchDrawCallCounter</c> so the flushed segment is banked into the draw-call count. A direct
/// <c>Raylib.Begin*Mode</c> / <c>End*Mode</c> call anywhere else silently drops that segment and
/// undercounts. This test scans the RaylibGum source and fails if any direct call exists outside
/// the counter, turning a forgotten flush point into a red build instead of a silent miscount.
/// </summary>
public class FlushPointCoverageGuardTests
{
    private static readonly string[] FlushMethods =
    {
        "BeginMode2D", "EndMode2D",
        "BeginScissorMode", "EndScissorMode",
        "BeginBlendMode", "EndBlendMode",
        "BeginTextureMode", "EndTextureMode",
        "BeginShaderMode", "EndShaderMode",
    };

    // The one file allowed to call the raylib flush functions directly — it is the wrapper.
    private const string WrapperFileName = "BatchDrawCallCounter.cs";

    [Fact]
    public void NoRaylibFlushCallsBypassTheCounter()
    {
        string sourceDirectory = FindRaylibGumSourceDirectory();
        string methods = string.Join("|", FlushMethods);
        // A direct raylib flush is either unqualified (called via `using static Raylib_cs.Raylib`)
        // or `Raylib.`-qualified. A member-access call on the counter (e.g.
        // `...BatchDrawCallCounter.BeginBlendMode(`) is preceded by a '.' and is the sanctioned
        // path, so it is not matched.
        Regex directCall = new(
            $@"\bRaylib\.\s*({methods})\s*\(|(?<![\w.])({methods})\s*\(",
            RegexOptions.Compiled);

        List<string> violations = new();

        foreach (string file in Directory.EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file) == WrapperFileName)
            {
                continue;
            }
            // Skip generated/build output.
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.TrimStart();
                // Ignore comment lines so doc references to the method names don't trip the guard.
                if (trimmed.StartsWith("//") || trimmed.StartsWith("*") || trimmed.StartsWith("/*"))
                {
                    continue;
                }

                if (directCall.IsMatch(line))
                {
                    violations.Add($"{Path.GetFileName(file)}:{i + 1}: {trimmed}");
                }
            }
        }

        violations.ShouldBeEmpty(
            "These raylib flush calls bypass BatchDrawCallCounter and will undercount draw calls. "
            + "Route them through Renderer.Self.BatchDrawCallCounter:" + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    private static string FindRaylibGumSourceDirectory()
    {
        DirectoryInfo directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, "Runtimes", "RaylibGum");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the Runtimes/RaylibGum source directory from " + AppContext.BaseDirectory);
    }
}
