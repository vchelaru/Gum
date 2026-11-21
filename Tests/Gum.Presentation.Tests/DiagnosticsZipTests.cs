using System.IO.Compression;
using Gum.Diagnostics;
using Shouldly;

namespace Gum.Presentation.Tests;

public class DiagnosticsZipTests : IDisposable
{
    private readonly string _root;
    private readonly string _gumFolder;
    private readonly string _gameFolder;
    private readonly string _logFile;
    private readonly string _settingsFile;
    private readonly DiagnosticsSystemInfo _info = new DiagnosticsSystemInfo(
        "2026.11", @"Windows 11 C:\Users\sam", ".NET 10", "X64", "screen 150%", "Test GPU");

    public DiagnosticsZipTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumDiagZip-" + Guid.NewGuid().ToString("N"));
        _gameFolder = Path.Combine(_root, "Game");
        _gumFolder = Path.Combine(_gameFolder, "Content", "GumProject");
        Directory.CreateDirectory(Path.Combine(_gumFolder, "Screens"));
        Directory.CreateDirectory(Path.Combine(_gameFolder, "Source", "Nested"));
        foreach (string skipped in new[] { "bin", "obj", ".git", ".vs", "node_modules" })
        {
            Directory.CreateDirectory(Path.Combine(_gameFolder, skipped));
            File.WriteAllText(Path.Combine(_gameFolder, skipped, "junk.txt"), "junk");
        }
        File.WriteAllText(Path.Combine(_gumFolder, "Game.gumx"), "<gumx/>");
        File.WriteAllText(Path.Combine(_gumFolder, "Screens", "Main.gusx"), "<screen/>");
        File.WriteAllText(Path.Combine(_gumFolder, "notes.txt"), "not a gum file");
        File.WriteAllText(Path.Combine(_gameFolder, "Game.csproj"), "<Project/>");
        File.WriteAllText(Path.Combine(_gameFolder, "Source", "Nested", "Deep.cs"), @"// C:\Users\sam\Game stays in source");
        File.WriteAllText(Path.Combine(_gameFolder, "Program.cs"), "class P {}");
        _logFile = Path.Combine(_root, "tool-1.log");
        File.WriteAllText(_logFile, @"opened C:\Users\sam\Game\Game.gumx by sam");
        _settingsFile = Path.Combine(_root, "settings.json");
        File.WriteAllText(_settingsFile, @"{ ""last"": ""C:\\Users\\sam\\x"" }".Replace(@"\\", @"\"));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private DiagnosticsContent Content() => new DiagnosticsContent(
        new[] { _logFile },
        @"1 error(s): C:\Users\sam\Game broke",
        @"MyPlugin 1.0 - C:\Users\sam\plugins\my.dll",
        new[] { _settingsFile },
        Path.Combine(_gumFolder, "Game.gumx"),
        _gameFolder);

    private List<string> Entries(string zipPath)
    {
        using ZipArchive zip = ZipFile.OpenRead(zipPath);
        return zip.Entries.Select(entry => entry.FullName).OrderBy(name => name).ToList();
    }

    private string ReadEntry(string zipPath, string entryName)
    {
        using ZipArchive zip = ZipFile.OpenRead(zipPath);
        using StreamReader reader = new StreamReader(zip.GetEntry(entryName)!.Open());
        return reader.ReadToEnd();
    }

    private string Write(DiagnosticsRequest request)
    {
        string zipPath = Path.Combine(_root, Guid.NewGuid().ToString("N") + ".zip");
        DiagnosticsZipWriter.Write(zipPath, request, _info, Content(), new PathScrubber("sam", @"C:\Users\sam"));
        return zipPath;
    }

    [Fact]
    public void Write_NoOptionalBoxes_HasOnlyTheAlwaysIncludedFiles()
    {
        string zip = Write(new DiagnosticsRequest("", IncludeGumProject: false, IncludeCSharpProject: false));

        Entries(zip).ShouldBe(new[] { "errors.txt", "logs/tool-1.log", "plugins.txt", "settings/settings.json", "system-info.txt" });
    }

    [Fact]
    public void Write_GumProjectOnly_AddsGumFilesWithoutOtherFiles()
    {
        string zip = Write(new DiagnosticsRequest("", true, false));

        Entries(zip).ShouldContain("gum-project/Game.gumx");
        Entries(zip).ShouldContain("gum-project/Screens/Main.gusx");
        Entries(zip).ShouldNotContain("gum-project/notes.txt");
        Entries(zip).ShouldNotContain(entry => entry.StartsWith("csharp-project/"));
    }

    [Fact]
    public void Write_CSharpProjectOnly_AddsNestedSourceAndSkipsBuildFolders()
    {
        string zip = Write(new DiagnosticsRequest("", false, true));

        List<string> entries = Entries(zip);
        entries.ShouldContain("csharp-project/Game.csproj");
        entries.ShouldContain("csharp-project/Program.cs");
        entries.ShouldContain("csharp-project/Source/Nested/Deep.cs");
        entries.ShouldContain("csharp-project/Content/GumProject/Game.gumx");
        entries.ShouldNotContain(entry => entry.StartsWith("gum-project/"));
        foreach (string skipped in DiagnosticsZipWriter.SkippedFolders)
        {
            entries.ShouldNotContain(entry => entry.StartsWith("csharp-project/" + skipped + "/"));
        }
    }

    [Fact]
    public void Write_BothBoxes_AddsBothProjects()
    {
        string zip = Write(new DiagnosticsRequest("", true, true));

        Entries(zip).ShouldContain("gum-project/Game.gumx");
        Entries(zip).ShouldContain("csharp-project/Program.cs");
    }

    [Fact]
    public void Write_BoxesCheckedButNoProjectLocated_AddsNothingForThem()
    {
        DiagnosticsContent none = Content() with { GumProjectFile = null, CSharpProjectFolder = null };
        string zipPath = Path.Combine(_root, "none.zip");

        DiagnosticsZipWriter.Write(zipPath, new DiagnosticsRequest("", true, true), _info, none, new PathScrubber("sam", @"C:\Users\sam"));

        Entries(zipPath).ShouldNotContain(entry => entry.Contains("project/"));
    }

    [Fact]
    public void Write_TextWithDescription_SavesItAsTypedWithoutScrubbing()
    {
        string zip = Write(new DiagnosticsRequest(@"It broke in C:\Users\sam\Game", false, false));

        ReadEntry(zip, "description.txt").ShouldBe(@"It broke in C:\Users\sam\Game");
    }

    [Fact]
    public void Write_EmptyDescription_HasNoDescriptionFile()
    {
        string zip = Write(new DiagnosticsRequest("  ", false, false));

        Entries(zip).ShouldNotContain("description.txt");
    }

    [Fact]
    public void Write_ToolTextFiles_AreScrubbed()
    {
        string zip = Write(new DiagnosticsRequest("", false, false));

        foreach (string name in new[] { "system-info.txt", "errors.txt", "plugins.txt", "logs/tool-1.log", "settings/settings.json" })
        {
            string text = ReadEntry(zip, name);
            text.ShouldNotContain("sam", Case.Sensitive, name);
            text.ShouldContain("%USER%", Case.Sensitive, name);
        }
    }

    [Fact]
    public void Write_SystemInfo_HasEveryFact()
    {
        string text = ReadEntry(Write(new DiagnosticsRequest("", false, false)), "system-info.txt");

        text.ShouldContain("Gum version: 2026.11");
        text.ShouldContain(".NET runtime: .NET 10");
        text.ShouldContain("CPU architecture: X64");
        text.ShouldContain("Display scale: screen 150%");
        text.ShouldContain("Graphics card: Test GPU");
        text.ShouldContain("Operating system: Windows 11");
    }

    [Fact]
    public void Write_UserSourceFiles_AreNotScrubbed()
    {
        string zip = Write(new DiagnosticsRequest("", false, true));

        ReadEntry(zip, "csharp-project/Source/Nested/Deep.cs").ShouldContain(@"C:\Users\sam\Game");
    }

    [Fact]
    public void Write_ExistingFile_Throws()
    {
        string zipPath = Path.Combine(_root, "exists.zip");
        File.WriteAllText(zipPath, "keep me");

        Should.Throw<IOException>(() => DiagnosticsZipWriter.Write(
            zipPath, new DiagnosticsRequest("", false, false), _info, Content(), new PathScrubber("sam", @"C:\Users\sam")));

        File.ReadAllText(zipPath).ShouldBe("keep me");
    }

    [Fact]
    public void FileName_Next_UsesDateThenNumbersOnCollision()
    {
        DateTime day = new DateTime(2026, 11, 21);

        DiagnosticsFileName.Next(_root, day).ShouldBe("GumDiagnostics-2026-11-21.zip");
        File.WriteAllText(Path.Combine(_root, "GumDiagnostics-2026-11-21.zip"), "x");
        DiagnosticsFileName.Next(_root, day).ShouldBe("GumDiagnostics-2026-11-21-2.zip");
        File.WriteAllText(Path.Combine(_root, "GumDiagnostics-2026-11-21-2.zip"), "x");
        DiagnosticsFileName.Next(_root, day).ShouldBe("GumDiagnostics-2026-11-21-3.zip");
    }

    [Fact]
    public void CSharpProjectLocator_FindsNearestFolderWithCsproj()
    {
        CSharpProjectLocator.FindFolder(Path.Combine(_gumFolder, "Game.gumx")).ShouldBe(_gameFolder);
    }

    [Fact]
    public void CSharpProjectLocator_NoCsprojAbove_ReturnsNull()
    {
        string lonely = Path.Combine(_root, "Lonely");
        Directory.CreateDirectory(lonely);
        File.Delete(Path.Combine(_gameFolder, "Game.csproj"));

        CSharpProjectLocator.FindFolder(Path.Combine(lonely, "x.gumx")).ShouldBeNull();
    }

    [Fact]
    public void CSharpProjectLocator_NoProject_ReturnsNull()
    {
        CSharpProjectLocator.FindFolder(null).ShouldBeNull();
    }
}
