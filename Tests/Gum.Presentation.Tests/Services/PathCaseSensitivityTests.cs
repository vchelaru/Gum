using System;
using System.IO;
using Gum.Services;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests.Services;

public class PathCaseSensitivityTests
{
    private readonly PathCaseSensitivity _sut = new PathCaseSensitivity();

    [Fact]
    public void GetComparison_InACaseSensitiveDirectory_IsOrdinal_EvenWhenACaseTwinExists()
    {
        string? folder = CaseSensitiveTempDirectory.TryCreate();
        if (folder == null)
        {
            // A default macOS volume can't hold a case-sensitive directory.
            return;
        }
        try
        {
            Directory.CreateDirectory(Path.Combine(folder, "Foo"));
            Directory.CreateDirectory(Path.Combine(folder, "fOO"));

            _sut.GetComparison(Path.Combine(folder, "Foo")).ShouldBe(StringComparison.Ordinal);
            // A path that doesn't exist yet takes its nearest existing ancestor's answer.
            _sut.GetComparison(Path.Combine(folder, "Foo", "NotCreated")).ShouldBe(StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void GetComparison_InTheDefaultTempDirectory_MatchesWhetherTheFileSystemResolvesAnotherCase()
    {
        string folder = Path.Combine(Path.GetTempPath(), "GumPathCase_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string path = Path.Combine(folder, "Folder");
            Directory.CreateDirectory(path);
            StringComparison expected = Directory.Exists(Path.Combine(folder, "FOLDER"))
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            _sut.GetComparison(path).ShouldBe(expected);
            // A path spelled in other casings than the folder on disk gets the same answer.
            _sut.GetComparison(Path.Combine(folder, "fOLDER")).ShouldBe(expected);
            _sut.GetComparison(Path.Combine(folder, "FOLDER")).ShouldBe(expected);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
