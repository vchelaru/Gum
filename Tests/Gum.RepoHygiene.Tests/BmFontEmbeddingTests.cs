using System.Diagnostics;
using System.Text.Json;
using Shouldly;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// bmfont.exe only runs on Windows, so a publish for another OS must not carry it (#5457). The
/// release workflow passes <c>-p:EmbedBmFont=false</c> for non-Windows RIDs (the SDK strips the
/// RuntimeIdentifier itself from library references); any other build keeps the resource.
/// </summary>
public class BmFontEmbeddingTests
{
    [Theory]
    [InlineData("", true)]
    [InlineData("EmbedBmFont=true", true)]
    [InlineData("EmbedBmFont=false", false)]
    public void ProjectServices_EmbedsBmFontExe_UnlessEmbedBmFontIsFalse(string property, bool expectedEmbedded)
    {
        string csproj = Path.Combine(RepoPaths.RepoRoot, "Tools", "Gum.ProjectServices", "Gum.ProjectServices.csproj");

        List<string> embedded = GetEmbeddedResourceIdentities(csproj, property);

        embedded.Any(identity => identity.EndsWith("bmfont.exe", StringComparison.OrdinalIgnoreCase))
            .ShouldBe(expectedEmbedded);
        // The template is tiny and shared by every head; it must not be affected by the RID.
        embedded.Any(identity => identity.EndsWith("BmfcTemplate.bmfc", StringComparison.OrdinalIgnoreCase))
            .ShouldBeTrue();
    }

    private static List<string> GetEmbeddedResourceIdentities(string csproj, string property)
    {
        ProcessStartInfo info = new("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        info.ArgumentList.Add("msbuild");
        info.ArgumentList.Add(csproj);
        info.ArgumentList.Add("-getItem:EmbeddedResource");
        info.ArgumentList.Add("-nologo");
        if (property.Length > 0)
        {
            info.ArgumentList.Add("-p:" + property);
        }

        using Process process = Process.Start(info)!;
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.ShouldBe(0, stdout + stderr);

        using JsonDocument json = JsonDocument.Parse(stdout);
        return json.RootElement.GetProperty("Items").GetProperty("EmbeddedResource")
            .EnumerateArray()
            .Select(item => item.GetProperty("Identity").GetString()!)
            .ToList();
    }
}
