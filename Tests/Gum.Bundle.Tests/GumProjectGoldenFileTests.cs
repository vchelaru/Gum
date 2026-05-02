using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Gum.Bundle;
using Gum.DataTypes;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// Regression nets for the bundle pipeline against the real <c>GumProject.zip</c> fixture
/// shipped with <c>MonoGameGum.Tests</c>. These complement the structural-equivalence test
/// in <c>GumServiceBundleLoadTests</c>: that one proves a bundled load matches a loose load
/// today, these ones catch silent walker drift and packer corruption over time.
///
/// Path manifests are used as the gold files (not bundle bytes) because brotli output is
/// not byte-stable across .NET versions, so byte-level gold files would be flaky. Path
/// manifests are stable, human-reviewable in PRs, and catch the same regressions.
/// </summary>
public class GumProjectGoldenFileTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _projectDir;
    private readonly string _gumxPath;
    private readonly Dictionary<string, byte[]> _entriesByRelativePath;

    public GumProjectGoldenFileTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "GumProjectGoldenFileTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_tempRoot);
        _projectDir = Path.Combine(_tempRoot, "GumProject");
        Directory.CreateDirectory(_projectDir);

        _entriesByRelativePath = ExtractZipToBundleEntries();
        WriteEntriesAsLooseFiles(_projectDir, _entriesByRelativePath);
        _gumxPath = Path.Combine(_projectDir, "FromZipFileGumProject.gumx");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public void Pack_compressed_size_is_smaller_than_uncompressed_size()
    {
        WalkResult walkResult = WalkFixture();
        List<(string path, byte[] content)> entries = BuildEntriesFromWalk(walkResult);

        long uncompressed = entries.Sum(e => (long)e.content.Length);

        using MemoryStream output = new MemoryStream();
        GumBundleWriter.Write(output, entries);
        long compressed = output.Length;

        compressed.ShouldBeLessThan(uncompressed);
    }

    [Fact]
    public void Pack_then_extract_yields_expected_file_set()
    {
        WalkResult walkResult = WalkFixture();
        List<(string path, byte[] content)> entries = BuildEntriesFromWalk(walkResult);

        using MemoryStream output = new MemoryStream();
        GumBundleWriter.Write(output, entries);
        output.Position = 0;
        GumBundle bundle = GumBundleReader.Read(output);

        List<string> actualPaths = bundle.EntryPathsInOrder
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        List<string> expectedPaths = ReadGoldenManifest("GumProjectZip_BundleEntries.txt");

        if (expectedPaths.Count == 0)
        {
            // Snapshot bootstrap: write the actual output to the gold file location so the
            // maintainer can inspect, copy into the source tree, and re-run for green.
            WriteSnapshotForBootstrap("GumProjectZip_BundleEntries.txt", actualPaths);
            throw new Xunit.Sdk.XunitException(
                "Gold file 'GumProjectZip_BundleEntries.txt' was empty. A snapshot has been written next to the test binary; copy it into Tests/Gum.Bundle.Tests/GoldenManifests/ and re-run.");
        }

        actualPaths.ShouldBe(expectedPaths);
    }

    [Fact]
    public void Pack_then_extract_yields_expected_per_entry_bytes()
    {
        WalkResult walkResult = WalkFixture();
        List<(string path, byte[] content)> entries = BuildEntriesFromWalk(walkResult);

        using MemoryStream output = new MemoryStream();
        GumBundleWriter.Write(output, entries);
        output.Position = 0;
        GumBundle bundle = GumBundleReader.Read(output);

        bundle.Entries.Count.ShouldBe(entries.Count);

        foreach ((string path, byte[] content) entry in entries)
        {
            bundle.Entries.ContainsKey(entry.path).ShouldBeTrue($"Bundle missing entry '{entry.path}'.");
            byte[] roundTripped = bundle.Entries[entry.path];
            byte[] original = _entriesByRelativePath[entry.path];

            roundTripped.ShouldBe(original, $"Entry '{entry.path}' bytes did not round-trip identically.");
            roundTripped.ShouldBe(entry.content, $"Entry '{entry.path}' bytes differ from packer input.");
        }
    }

    [Fact]
    public void Walker_dependency_list_matches_golden_manifest()
    {
        WalkResult walkResult = WalkFixture();

        List<string> actualPaths = walkResult.AllIncludedFiles
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        List<string> expectedPaths = ReadGoldenManifest("GumProjectZip_WalkerDependencies.txt");

        if (expectedPaths.Count == 0)
        {
            WriteSnapshotForBootstrap("GumProjectZip_WalkerDependencies.txt", actualPaths);
            throw new Xunit.Sdk.XunitException(
                "Gold file 'GumProjectZip_WalkerDependencies.txt' was empty. A snapshot has been written next to the test binary; copy it into Tests/Gum.Bundle.Tests/GoldenManifests/ and re-run.");
        }

        actualPaths.ShouldBe(expectedPaths);
    }

    private WalkResult WalkFixture()
    {
        GumProjectSave? project = GumProjectSave.Load(_gumxPath, out GumLoadResult result);
        result.ErrorMessage.ShouldBeNullOrEmpty();
        project.ShouldNotBeNull();

        GumProjectDependencyWalker walker = new GumProjectDependencyWalker();
        WalkResult walkResult = walker.Walk(
            project!,
            _projectDir,
            GumBundleInclusion.Core | GumBundleInclusion.FontCache | GumBundleInclusion.ExternalFiles);

        walkResult.MissingFiles.ShouldBeEmpty();
        return walkResult;
    }

    private List<(string path, byte[] content)> BuildEntriesFromWalk(WalkResult walkResult)
    {
        LooseFileGumFileProvider provider = new LooseFileGumFileProvider(_projectDir);
        List<(string path, byte[] content)> entries = new List<(string path, byte[] content)>();
        foreach (string relativePath in walkResult.AllIncludedFiles)
        {
            using Stream stream = provider.OpenRead(relativePath);
            using MemoryStream memory = new MemoryStream();
            stream.CopyTo(memory);
            entries.Add((relativePath, memory.ToArray()));
        }
        return entries;
    }

    private static Dictionary<string, byte[]> ExtractZipToBundleEntries()
    {
        const string zipPrefix = "GumProject/";
        string zipPath = Path.Combine(AppContext.BaseDirectory, "TestContent", "GumProject.zip");
        File.Exists(zipPath).ShouldBeTrue($"Test zip missing at {zipPath}");

        Dictionary<string, byte[]> entries = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        using FileStream fs = File.OpenRead(zipPath);
        using ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.FullName.EndsWith("/"))
            {
                continue;
            }

            string key = entry.FullName.StartsWith(zipPrefix, StringComparison.Ordinal)
                ? entry.FullName.Substring(zipPrefix.Length)
                : entry.FullName;

            using Stream s = entry.Open();
            using MemoryStream ms = new MemoryStream();
            s.CopyTo(ms);
            entries[key] = ms.ToArray();
        }
        return entries;
    }

    private static void WriteEntriesAsLooseFiles(string projectDir, IReadOnlyDictionary<string, byte[]> entries)
    {
        foreach (KeyValuePair<string, byte[]> kvp in entries)
        {
            string fullPath = Path.Combine(projectDir, kvp.Key.Replace('/', Path.DirectorySeparatorChar));
            string? parent = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }
            File.WriteAllBytes(fullPath, kvp.Value);
        }
    }

    private static List<string> ReadGoldenManifest(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "GoldenManifests", fileName);
        if (!File.Exists(path))
        {
            return new List<string>();
        }

        return File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();
    }

    private static void WriteSnapshotForBootstrap(string fileName, IEnumerable<string> lines)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "GoldenManifests", fileName);
        string? parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }
        File.WriteAllText(path, string.Join("\n", lines) + "\n");
    }
}
