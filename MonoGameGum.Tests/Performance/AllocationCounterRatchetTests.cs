using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// Every test that measures allocations reads the counter through <c>AllocationMeasurer</c>, which drains the
/// finalizers earlier tests left queued before the warm-up (#8190). A test that calls
/// <c>GC.GetAllocatedBytesForCurrentThread</c> itself skips that drain and flakes on whichever test ran before it.
/// </summary>
public class AllocationCounterRatchetTests
{
    private static readonly string[] CounterApis =
    {
        "GetAllocatedBytesForCurrentThread",
        "GetTotalAllocatedBytes",
    };

    [Fact]
    public void TestFiles_ReadTheAllocationCounterOnlyThroughAllocationMeasurer()
    {
        List<string> offenders = new();
        foreach (string file in TestSourceFiles())
        {
            string text = File.ReadAllText(file);
            if (CounterApis.Any(text.Contains))
            {
                offenders.Add(RelativePath(file));
            }
        }

        offenders.ShouldBeEmpty(
            "Use AllocationMeasurer.Measure, MeasureReplayed or ThreadBytes (with Drain before the warm-up) " +
            "instead of GC.GetAllocatedBytesForCurrentThread:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void TestFiles_ReadingThreadBytes_AlsoDrainOrMeasure()
    {
        List<string> offenders = new();
        foreach (string file in TestSourceFiles())
        {
            string text = File.ReadAllText(file);
            if (text.Contains("AllocationMeasurer.ThreadBytes") &&
                !text.Contains("AllocationMeasurer.Drain(") &&
                !text.Contains("AllocationMeasurer.Measure"))
            {
                offenders.Add(RelativePath(file));
            }
        }

        offenders.ShouldBeEmpty(
            "A file that reads AllocationMeasurer.ThreadBytes outside AllocationMeasurer.Measure must call " +
            "AllocationMeasurer.Drain() before its warm-up:\n" + string.Join("\n", offenders));
    }

    private static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Gum.slnx")))
        {
            directory = directory.Parent;
        }
        directory.ShouldNotBeNull("Gum.slnx not found above " + AppContext.BaseDirectory);
        return directory!.FullName;
    }

    private static string RelativePath(string file) => Path.GetRelativePath(RepoRoot(), file).Replace('\\', '/');

    // The shared helper wraps the counter; the Gum.Mvvm benchmark harnesses report allocations as numbers (they
    // assert nothing and run outside the test host); this file names the API in its messages.
    private static IEnumerable<string> TestSourceFiles()
    {
        string root = RepoRoot();
        IEnumerable<string> directories = new[] { Path.Combine(root, "Tests"), Path.Combine(root, "MonoGameGum.Tests") }
            .Where(Directory.Exists);
        foreach (string directory in directories)
        {
            foreach (string file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                string relative = RelativePath(file);
                if (relative.Contains("/obj/") || relative.Contains("/bin/"))
                {
                    continue;
                }
                if (relative.StartsWith("Tests/Gum.Mvvm.Benchmarks", StringComparison.Ordinal) ||
                    relative == "Tests/MonoGameGum.TestsCommon/AllocationMeasurer.cs" ||
                    relative == "MonoGameGum.Tests/Performance/AllocationCounterRatchetTests.cs")
                {
                    continue;
                }
                yield return file;
            }
        }
    }
}
