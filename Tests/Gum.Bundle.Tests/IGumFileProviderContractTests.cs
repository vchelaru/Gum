using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Bundle;
using Shouldly;

namespace Gum.Bundle.Tests;

public abstract class IGumFileProviderContractTests
{
    protected abstract IGumFileProvider CreateProvider(IReadOnlyDictionary<string, byte[]> files);

    [Fact]
    public void EnumerateFiles_matches_pattern_with_question_mark_wildcard()
    {
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>
        {
            ["a1.txt"] = new byte[] { 1 },
            ["a2.txt"] = new byte[] { 2 },
            ["bb.txt"] = new byte[] { 3 },
        };
        IGumFileProvider provider = CreateProvider(files);

        List<string> result = provider.EnumerateFiles("a?.txt").ToList();

        result.Count.ShouldBe(2);
        result.ShouldContain("a1.txt");
        result.ShouldContain("a2.txt");
    }

    [Fact]
    public void EnumerateFiles_matches_pattern_with_star_wildcard()
    {
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>
        {
            ["a.txt"] = new byte[] { 1 },
            ["b.txt"] = new byte[] { 2 },
            ["a.png"] = new byte[] { 3 },
        };
        IGumFileProvider provider = CreateProvider(files);

        List<string> result = provider.EnumerateFiles("*.txt").ToList();

        result.Count.ShouldBe(2);
        result.ShouldContain("a.txt");
        result.ShouldContain("b.txt");
    }

    [Fact]
    public void EnumerateFiles_matches_pattern_with_subdirectory()
    {
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>
        {
            ["Screens/Main.gusx"] = new byte[] { 1 },
            ["Screens/Other.gusx"] = new byte[] { 2 },
            ["Components/Foo.gucx"] = new byte[] { 3 },
        };
        IGumFileProvider provider = CreateProvider(files);

        List<string> result = provider.EnumerateFiles("Screens/*.gusx").ToList();

        result.Count.ShouldBe(2);
        result.ShouldContain("Screens/Main.gusx");
        result.ShouldContain("Screens/Other.gusx");
    }

    [Fact]
    public void EnumerateFiles_returns_empty_when_no_match()
    {
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>
        {
            ["a.txt"] = new byte[] { 1 },
        };
        IGumFileProvider provider = CreateProvider(files);

        List<string> result = provider.EnumerateFiles("*.png").ToList();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Exists_is_case_sensitive()
    {
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>
        {
            ["MyFile.txt"] = new byte[] { 1 },
        };
        IGumFileProvider provider = CreateProvider(files);

        provider.Exists("myfile.txt").ShouldBeFalse();
    }

    [Fact]
    public void Exists_returns_false_for_absent_file()
    {
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>
        {
            ["present.txt"] = new byte[] { 1 },
        };
        IGumFileProvider provider = CreateProvider(files);

        provider.Exists("absent.txt").ShouldBeFalse();
    }

    [Fact]
    public void Exists_returns_true_for_present_file()
    {
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>
        {
            ["present.txt"] = new byte[] { 1 },
        };
        IGumFileProvider provider = CreateProvider(files);

        provider.Exists("present.txt").ShouldBeTrue();
    }

    [Fact]
    public void OpenRead_is_case_sensitive()
    {
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>
        {
            ["MyFile.txt"] = new byte[] { 1, 2, 3 },
        };
        IGumFileProvider provider = CreateProvider(files);

        Should.Throw<FileNotFoundException>(() => provider.OpenRead("myfile.txt"));
    }

    [Fact]
    public void OpenRead_returns_correct_bytes()
    {
        byte[] payload = new byte[] { 10, 20, 30, 40, 50 };
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>
        {
            ["data.bin"] = payload,
        };
        IGumFileProvider provider = CreateProvider(files);

        using Stream stream = provider.OpenRead("data.bin");
        using MemoryStream copy = new MemoryStream();
        stream.CopyTo(copy);
        copy.ToArray().ShouldBe(payload);
    }

    [Fact]
    public void OpenRead_throws_FileNotFoundException_for_absent_file()
    {
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>
        {
            ["present.txt"] = new byte[] { 1 },
        };
        IGumFileProvider provider = CreateProvider(files);

        Should.Throw<FileNotFoundException>(() => provider.OpenRead("absent.txt"));
    }

    [Fact]
    public void Paths_use_forward_slashes_in_EnumerateFiles_results()
    {
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>
        {
            ["Screens/Main.gusx"] = new byte[] { 1 },
            ["Components/Sub/Foo.gucx"] = new byte[] { 2 },
        };
        IGumFileProvider provider = CreateProvider(files);

        List<string> result = provider.EnumerateFiles("*.gusx").ToList();

        foreach (string path in result)
        {
            path.ShouldNotContain("\\");
        }
    }
}
