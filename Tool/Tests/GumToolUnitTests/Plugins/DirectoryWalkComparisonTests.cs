using Gum.Plugins;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace GumToolUnitTests.Plugins;

public class DirectoryWalkComparisonTests
{
    [Fact]
    public void Compare_SaysNothing_WhenBothWalksAgree()
    {
        string folder = CreateFolderWithNestedDll(out string _);

        try
        {
            new DirectoryWalkComparison().Compare(folder, "dll").ShouldBeNull();
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // The comparison only earns its keep if it names where the walks diverge, so pin that the
    // detail a bug report needs is actually in the text.
    [Fact]
    public void Compare_DescribesTheDivergence_WhenTheFolderIsGone()
    {
        string missingFolder = Path.Combine(Path.GetTempPath(), $"gum-test-missing-{Guid.NewGuid():N}");

        string? description = new DirectoryWalkComparison().Compare(missingFolder, "dll");

        description.ShouldNotBeNull();
        description.ShouldContain(missingFolder);
    }

    private static string CreateFolderWithNestedDll(out string dllPath)
    {
        string folder = Path.Combine(Path.GetTempPath(), $"gum-test-walk-{Guid.NewGuid():N}");
        string subfolder = Path.Combine(folder, "NestedPlugin");
        Directory.CreateDirectory(subfolder);
        File.WriteAllText(Path.Combine(folder, "readme.txt"), "not a dll");
        dllPath = Path.Combine(subfolder, "NestedPlugin.dll");
        File.WriteAllText(dllPath, "pretend assembly");
        return folder;
    }
}
