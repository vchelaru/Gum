using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gum.Bundle;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// Tests for <see cref="CompositeGumFileProvider"/>: first-hit-wins for <c>Exists</c>/<c>OpenRead</c>,
/// deduped union (first occurrence wins) for <c>EnumerateFiles</c>.
/// </summary>
public class CompositeGumFileProviderTests
{
    [Fact]
    public void Exists_is_true_when_any_provider_has_the_file()
    {
        CompositeGumFileProvider composite = new CompositeGumFileProvider(
            Provider(("a.txt", "first")),
            Provider(("b.txt", "second")));

        composite.Exists("a.txt").ShouldBeTrue();
        composite.Exists("b.txt").ShouldBeTrue();
        composite.Exists("missing.txt").ShouldBeFalse();
    }

    [Fact]
    public void OpenRead_returns_content_from_the_first_provider_that_has_the_file()
    {
        // Both providers contain "shared.txt"; the first one wins (overlay precedence).
        CompositeGumFileProvider composite = new CompositeGumFileProvider(
            Provider(("shared.txt", "from-first")),
            Provider(("shared.txt", "from-second")));

        ReadAll(composite.OpenRead("shared.txt")).ShouldBe("from-first");
    }

    [Fact]
    public void OpenRead_falls_through_to_a_later_provider_on_miss()
    {
        CompositeGumFileProvider composite = new CompositeGumFileProvider(
            Provider(("a.txt", "from-first")),
            Provider(("b.txt", "from-second")));

        ReadAll(composite.OpenRead("b.txt")).ShouldBe("from-second");
    }

    [Fact]
    public void OpenRead_throws_FileNotFoundException_when_no_provider_has_the_file()
    {
        CompositeGumFileProvider composite = new CompositeGumFileProvider(
            Provider(("a.txt", "first")));

        Should.Throw<FileNotFoundException>(() => composite.OpenRead("missing.txt"));
    }

    [Fact]
    public void EnumerateFiles_returns_the_deduped_union_with_first_occurrence_winning()
    {
        CompositeGumFileProvider composite = new CompositeGumFileProvider(
            Provider(("Screens/A.ganx", "a"), ("shared.ganx", "first")),
            Provider(("shared.ganx", "second"), ("Components/B.ganx", "b")));

        List<string> matches = composite.EnumerateFiles("*.ganx").ToList();

        matches.ShouldBe(new[] { "Screens/A.ganx", "shared.ganx", "Components/B.ganx" });
    }

    [Fact]
    public void Constructor_throws_when_providers_is_null()
    {
        Should.Throw<ArgumentNullException>(() => new CompositeGumFileProvider(null!));
    }

    private static IGumFileProvider Provider(params (string path, string content)[] entries)
    {
        Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        List<string> order = new List<string>();
        foreach ((string path, string content) in entries)
        {
            dictionary[path] = Encoding.UTF8.GetBytes(content);
            order.Add(path);
        }
        return new BundleGumFileProvider(new GumBundle(version: 1, entries: dictionary, entryPathsInOrder: order));
    }

    private static string ReadAll(Stream stream)
    {
        using (stream)
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }
}
