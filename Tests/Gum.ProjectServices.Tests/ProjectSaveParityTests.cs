using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Shouldly;
using Xunit;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Byte parity for saved projects: loading a project and saving it again must reproduce every file
/// exactly, in any culture and on any OS. Both tool heads save through this data-model layer, so the
/// corpus in <c>ParityCorpus/</c> is the baseline both are held to. See <c>ParityCorpus/README.md</c>
/// for what each project covers and how to update the baselines when a format change is intended.
/// </summary>
public class ProjectSaveParityTests : IDisposable
{
    /// <summary>Set to 1 to write the saved files back over the corpus instead of comparing.</summary>
    private const string UpdateBaselinesVariable = "GUM_UPDATE_PARITY_BASELINES";

    private readonly string _tempDirectory;

    public ProjectSaveParityTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumSaveParity_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDirectory, recursive: true); } catch { }
    }

    /// <summary>
    /// Every corpus project under an invariant-friendly culture, a comma-decimal culture, and the
    /// Turkish culture whose casing rules break naive case-insensitive comparisons.
    /// </summary>
    public static IEnumerable<object[]> Cases()
    {
        foreach (string corpus in new[] { "FormsXml", "SkiaJson", "SkiaXml" })
        {
            foreach (string culture in new[] { "de-DE", "en-US", "tr-TR" })
            {
                yield return new object[] { corpus, culture };
            }
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void LoadThenSave_ReproducesTheCorpusByteForByte(string corpus, string cultureName)
    {
        string corpusDirectory = Path.Combine(FindRepositoryRoot(), "Tests", "Gum.ProjectServices.Tests", "ParityCorpus", corpus);
        CopyDirectory(corpusDirectory, _tempDirectory);
        string projectFile = Directory.GetFiles(_tempDirectory)
            .Single(path => path.EndsWith(".gumx", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".gumj", StringComparison.OrdinalIgnoreCase));

        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

            GumProjectSave? project = GumProjectSave.Load(projectFile, out GumLoadResult loadResult);
            project.ShouldNotBeNull(loadResult.ErrorMessage);
            project!.Save(projectFile, saveElements: true);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        if (Environment.GetEnvironmentVariable(UpdateBaselinesVariable) == "1")
        {
            CopyDirectory(_tempDirectory, corpusDirectory);
            return;
        }

        List<string> changed = Directory.GetFiles(_tempDirectory, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(_tempDirectory, path))
            .Select(relative => DescribeDifference(relative, Path.Combine(corpusDirectory, relative), Path.Combine(_tempDirectory, relative)))
            .OfType<string>()
            .ToList();

        changed.ShouldBeEmpty($"re-saving {corpus} under {cultureName} changed: {string.Join(" | ", changed)}");
    }

    // Names a file whose saved bytes differ from the corpus (or that the corpus lacks) and shows
    // where they first diverge.
    private static string? DescribeDifference(string relativePath, string corpusFile, string savedFile)
    {
        if (!File.Exists(corpusFile))
        {
            return $"{relativePath} was written but is not in the corpus";
        }

        byte[] committed = File.ReadAllBytes(corpusFile);
        byte[] saved = File.ReadAllBytes(savedFile);
        int length = Math.Min(committed.Length, saved.Length);
        int offset = 0;
        while (offset < length && committed[offset] == saved[offset])
        {
            offset++;
        }
        if (offset == length && committed.Length == saved.Length)
        {
            return null;
        }

        return $"{relativePath} ({committed.Length} -> {saved.Length} bytes, first difference at {offset}): " +
            $"corpus [{Excerpt(committed, offset)}] saved [{Excerpt(saved, offset)}]";
    }

    private static string Excerpt(byte[] bytes, int offset)
    {
        int start = Math.Max(0, offset - 40);
        int end = Math.Min(bytes.Length, offset + 40);
        return Encoding.UTF8.GetString(bytes, start, end - start)
            .Replace("\r", "<CR>")
            .Replace("\n", "<LF>");
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GumFull.sln")))
            {
                return directory.FullName;
            }
        }
        throw new InvalidOperationException("The test is not running inside the Gum repository.");
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}
