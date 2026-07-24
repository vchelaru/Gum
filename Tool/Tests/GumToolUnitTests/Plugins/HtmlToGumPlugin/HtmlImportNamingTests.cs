using HtmlToGumPlugin;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace GumToolUnitTests.Plugins.HtmlToGumPlugin;

public class HtmlImportNamingTests
{
    [Fact]
    public void IsUrl_ReturnsFalse_ForLocalPath()
    {
        bool result = HtmlImportNaming.IsUrl(@"C:\pages\index.html");

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsUrl_ReturnsTrue_ForHttpUrl()
    {
        bool result = HtmlImportNaming.IsUrl("http://example.com/page.html");

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsUrl_ReturnsTrue_ForHttpsUrl()
    {
        bool result = HtmlImportNaming.IsUrl("https://example.com/page.html");

        result.ShouldBeTrue();
    }

    [Fact]
    public void QualifyScreenName_PrefixesWithSubfolder_WhenSubfolderProvided()
    {
        string result = HtmlImportNaming.QualifyScreenName("MyScreen", "Imported");

        result.ShouldBe("Imported/MyScreen");
    }

    [Fact]
    public void QualifyScreenName_ReturnsScreenNameUnchanged_WhenSubfolderIsNullOrWhitespace()
    {
        HtmlImportNaming.QualifyScreenName("MyScreen", null).ShouldBe("MyScreen");
        HtmlImportNaming.QualifyScreenName("MyScreen", "   ").ShouldBe("MyScreen");
    }

    [Fact]
    public void QualifyScreenName_TrimsTrailingSlash_FromSubfolder()
    {
        string result = HtmlImportNaming.QualifyScreenName("MyScreen", "Imported/");

        result.ShouldBe("Imported/MyScreen");
    }

    [Fact]
    public void ResolveUniqueScreenName_AppendsSuffix_WhenQualifiedNameConflicts()
    {
        HashSet<string> existing = new() { "MyScreen" };

        string result = HtmlImportNaming.ResolveUniqueScreenName("MyScreen", null, existing.Contains);

        result.ShouldBe("MyScreen_2");
    }

    [Fact]
    public void ResolveUniqueScreenName_IncrementsSuffix_UntilNameIsFree()
    {
        HashSet<string> existing = new() { "MyScreen", "MyScreen_2", "MyScreen_3" };

        string result = HtmlImportNaming.ResolveUniqueScreenName("MyScreen", null, existing.Contains);

        result.ShouldBe("MyScreen_4");
    }

    [Fact]
    public void ResolveUniqueScreenName_ReturnsDesiredName_WhenNoConflict()
    {
        HashSet<string> existing = new();

        string result = HtmlImportNaming.ResolveUniqueScreenName("MyScreen", null, existing.Contains);

        result.ShouldBe("MyScreen");
    }

    [Fact]
    public void ResolveUniqueScreenName_ScopesConflictCheck_ToQualifiedName()
    {
        // "MyScreen" is taken at the project root, but "Imported/MyScreen" is free —
        // the subfolder should avoid the conflict entirely.
        HashSet<string> existing = new() { "MyScreen" };

        string result = HtmlImportNaming.ResolveUniqueScreenName("MyScreen", "Imported", existing.Contains);

        result.ShouldBe("MyScreen");
    }
}
