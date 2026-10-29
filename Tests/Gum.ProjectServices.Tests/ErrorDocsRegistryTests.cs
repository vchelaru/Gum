using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class ErrorDocsRegistryTests
{
    private readonly ErrorDocsRegistry _sut;

    public ErrorDocsRegistryTests()
    {
        _sut = new ErrorDocsRegistry();
    }

    [Fact]
    public void GetUrl_ShouldReturnNull_WhenCodeIsNotRegistered()
    {
        string? url = _sut.GetUrl("GUM9999");
        url.ShouldBeNull();
    }

    [Fact]
    public void GetUrl_ShouldReturnPublicDocsUrl_WhenCodeIsRegistered()
    {
        string? url = _sut.GetUrl("GUM0001");
        url.ShouldNotBeNull();
        url.ShouldStartWith("https://docs.flatredball.com/gum/");
    }

    [Fact]
    public void GetDocPath_ShouldPointAtBaseTypeDocs_ForCircularBaseTypeCode()
    {
        string? path = _sut.GetDocPath("GUM0014");
        path.ShouldNotBeNull();
        path.ShouldBe("gum-tool/gum-elements/general-properties/base-type-inheritance#circular-base-types-gum0014");
    }

    [Fact]
    public void EveryCodeTheToolEmits_ShouldBeRegistered()
    {
        string repoRoot = Directory.GetParent(FindDocsRoot())!.FullName;
        Regex emittedCode = new Regex(@"\b(?:Code|ErrorCode)\s*=\s*""(GUM\d{4})""");
        HashSet<string> unregistered = new HashSet<string>();
        int emittedCount = 0;

        foreach (string folder in new[] { "Tools", "Tool", "Gum", "GumCommon" })
        {
            string folderPath = Path.Combine(repoRoot, folder);
            foreach (string file in Directory.EnumerateFiles(folderPath, "*.cs", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(repoRoot, file);
                string[] segments = relative.Split(Path.DirectorySeparatorChar);
                bool skip = segments.Any(segment =>
                    segment == "bin" || segment == "obj" || segment == "Tests" || segment.EndsWith(".Tests"));
                if (skip)
                {
                    continue;
                }

                foreach (Match match in emittedCode.Matches(File.ReadAllText(file)))
                {
                    emittedCount++;
                    string code = match.Groups[1].Value;
                    if (_sut.GetDocPath(code) == null)
                    {
                        unregistered.Add(code);
                    }
                }
            }
        }

        emittedCount.ShouldBeGreaterThan(10, "the scan found almost no error codes, so it is looking in the wrong place");
        unregistered.ShouldBeEmpty(
            "these codes are emitted by the tool but have no entry in ErrorDocsRegistry: " + string.Join(", ", unregistered));
    }

    [Fact]
    public void AllRegisteredCodes_ShouldPointAtExistingDocsFileAndHeading()
    {
        string docsRoot = FindDocsRoot();

        foreach (string code in _sut.AllCodes)
        {
            string? path = _sut.GetDocPath(code);
            path.ShouldNotBeNull($"Code {code} returned null doc path");

            (string filePath, string? anchor) = ResolveDocsFile(docsRoot, path);
            File.Exists(filePath).ShouldBeTrue(
                $"Code {code} points at {path} but file {filePath} does not exist");

            if (anchor != null)
            {
                string content = File.ReadAllText(filePath);
                bool hasHeading = ContainsHeadingWithSlug(content, anchor);
                hasHeading.ShouldBeTrue(
                    $"Code {code} points at anchor #{anchor} in {filePath} but no heading slugs to that value");
            }
        }
    }

    private static (string filePath, string? anchor) ResolveDocsFile(string docsRoot, string docPath)
    {
        string pathPart = docPath;
        string? anchor = null;
        int hashIndex = docPath.IndexOf('#');
        if (hashIndex >= 0)
        {
            pathPart = docPath.Substring(0, hashIndex);
            anchor = docPath.Substring(hashIndex + 1);
        }

        string asMd = Path.Combine(docsRoot, pathPart.Replace('/', Path.DirectorySeparatorChar) + ".md");
        if (File.Exists(asMd))
        {
            return (asMd, anchor);
        }

        string asReadme = Path.Combine(docsRoot, pathPart.Replace('/', Path.DirectorySeparatorChar), "README.md");
        return (asReadme, anchor);
    }

    private static bool ContainsHeadingWithSlug(string content, string expectedSlug)
    {
        Regex headingPattern = new Regex(@"^#{1,6}\s+(.+)$", RegexOptions.Multiline);
        foreach (Match match in headingPattern.Matches(content))
        {
            string headingText = match.Groups[1].Value.Trim();
            string slug = Slugify(headingText);
            if (slug == expectedSlug)
            {
                return true;
            }
        }
        return false;
    }

    private static string Slugify(string heading)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in heading.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else if (c == ' ' || c == '-' || c == '_')
            {
                sb.Append('-');
            }
        }
        string result = sb.ToString();
        while (result.Contains("--"))
        {
            result = result.Replace("--", "-");
        }
        return result.Trim('-');
    }

    private static string FindDocsRoot()
    {
        DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "docs");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "SUMMARY.md")))
            {
                return candidate;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate docs/ folder by walking up from test bin directory");
    }
}
