using Gum.ProjectServices;
using Shouldly;
using System.Diagnostics;
using System.Text.Json;

namespace Gum.Cli.Tests;

/// <summary>
/// A <c>dotnet tool install -g GumCli</c> install has no checkout and no release layout around it, so
/// <c>gumcli new --theme X</c> only works when the package itself carries the themes (#8379). Builds the
/// folder the package would extract to from the csproj's pack items, without running a real pack.
/// </summary>
public class ToolPackageThemeLayoutTests : BaseTestClass
{
    private const string PackedToolFolder = @"tools\net10.0\any\";

    [Fact]
    public void FindThemeDirectory_PackedToolLayout_FindsEveryThemeAndStagesItsBehaviors()
    {
        string checkout = GumSourceLocator.Find(AppContext.BaseDirectory)!;
        string[] expectedThemes = Directory
            .GetDirectories(Path.Combine(checkout, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes"))
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray()!;

        string toolFolder = Path.Combine(TempDirectory, "GumCli", "tools", "net10.0", "any");
        ExtractPackedFiles(Path.Combine(checkout, "Tools", "Gum.Cli", "Gum.Cli.csproj"), Path.Combine(TempDirectory, "GumCli"));

        // Neither the tool folder nor the working folder is inside a checkout, like an installed tool.
        string workingDirectory = Path.Combine(TempDirectory, "Work");
        Directory.CreateDirectory(workingDirectory);
        FormsThemeLocator locator = FormsThemeLocator.CreateDefault(toolFolder, workingDirectory);

        locator.GetAvailableThemes().ShouldBe(expectedThemes);

        string? hazard = locator.FindThemeDirectory("Hazard");
        hazard.ShouldNotBeNull();
        string projectPath = Path.Combine(TempDirectory, "NewProject", "GumProject.gumx");
        new FormsTemplateCreator().CreateFromTheme(projectPath, hazard);

        File.Exists(Path.Combine(TempDirectory, "NewProject", "Behaviors", "ButtonBehavior.behx")).ShouldBeTrue();
    }

    private static void ExtractPackedFiles(string csprojPath, string packageRoot)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo(
            Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(csprojPath);
        startInfo.ArgumentList.Add("-getItem:None");

        using Process process = Process.Start(startInfo)!;
        process.StandardInput.Close();
        Task<string> error = process.StandardError.ReadToEndAsync();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.ShouldBe(0, error.GetAwaiter().GetResult() + output);

        int copied = 0;
        using JsonDocument document = JsonDocument.Parse(output);
        foreach (JsonElement item in document.RootElement.GetProperty("Items").GetProperty("None").EnumerateArray())
        {
            string packagePath = item.TryGetProperty("PackagePath", out JsonElement packagePathElement) ? packagePathElement.GetString() ?? string.Empty : string.Empty;
            if (!packagePath.StartsWith(PackedToolFolder, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string fullPath = item.GetProperty("FullPath").GetString()!;
            string destination = Path.Combine(
                packageRoot,
                packagePath.Replace('\\', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(fullPath, destination, overwrite: true);
            copied++;
        }

        copied.ShouldBeGreaterThan(0, "Gum.Cli.csproj packs nothing under " + PackedToolFolder);
    }
}
