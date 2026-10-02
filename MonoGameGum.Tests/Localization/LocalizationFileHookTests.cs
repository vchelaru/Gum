using Gum.Bundle;
using Gum.DataTypes;
using Gum.Localization;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.Localization;

/// <summary>
/// Loose localization reads go through FileManager.CustomGetStreamFromFile when a host installs one
/// (Android StreamingAssets). Every file here lives only in memory, none on disk.
/// </summary>
public class LocalizationFileHookTests : BaseTestClass
{
    private const string ProjectDirectory = "/hook-only-host/Content/";

    [Fact]
    public void Load_loose_csv_reads_through_the_hook()
    {
        InstallHook(("Loc/Strings.csv", "String ID,English,Spanish\nT_Hello,Hello,Hola\n"));
        GumProjectSave project = ProjectWithLocalizationFiles("Loc\\Strings.csv");
        LocalizationService service = new LocalizationService();
        List<string> warnings = new List<string>();

        ProjectLocalizationLoader.Load(project, service, null, warnings);

        warnings.ShouldBeEmpty();
        service.CurrentLanguage = 2;
        service.Translate("T_Hello").ShouldBe("Hola");
    }

    [Fact]
    public void Load_loose_resx_reads_the_base_file_through_the_hook()
    {
        InstallHook(("Strings.resx", Resx("T_Hello", "Hello")));
        GumProjectSave project = ProjectWithLocalizationFiles("Strings.resx");
        LocalizationService service = new LocalizationService();
        List<string> warnings = new List<string>();

        ProjectLocalizationLoader.Load(project, service, null, warnings);

        warnings.ShouldBeEmpty();
        service.CurrentLanguage = 1;
        service.Translate("T_Hello").ShouldBe("Hello");
    }

    [Fact]
    public void Load_loose_file_the_hook_lacks_is_skipped_with_a_warning()
    {
        InstallHook(("Other.csv", "String ID,English\nT_Hello,Hello\n"));
        GumProjectSave project = ProjectWithLocalizationFiles("Strings.csv");
        LocalizationService service = new LocalizationService();
        List<string> warnings = new List<string>();

        ProjectLocalizationLoader.Load(project, service, null, warnings);

        warnings.ShouldHaveSingleItem().ShouldContain("Strings.csv");
        service.HasDatabase.ShouldBeFalse();
    }

    [Fact]
    public void LooseFileGumFileProvider_serves_files_from_the_hook()
    {
        InstallHook(("Loc/Strings.csv", "abc"));
        LooseFileGumFileProvider provider = new LooseFileGumFileProvider(ProjectDirectory);

        provider.Exists("Loc/Strings.csv").ShouldBeTrue();
        provider.Exists("Loc/Missing.csv").ShouldBeFalse();
        using Stream stream = provider.OpenRead("Loc/Strings.csv");
        new StreamReader(stream).ReadToEnd().ShouldBe("abc");
    }

    private static void InstallHook(params (string relativePath, string content)[] files)
    {
        Dictionary<string, byte[]> bytes = files.ToDictionary(
            f => FileManager.Standardize(ProjectDirectory + f.relativePath, preserveCase: true, makeAbsolute: true),
            f => Encoding.UTF8.GetBytes(f.content));
        FileManager.CustomGetStreamFromFile = path =>
            bytes.TryGetValue(path, out byte[]? content) ? new MemoryStream(content) : null!;
    }

    private static GumProjectSave ProjectWithLocalizationFiles(params string[] files)
    {
        GumProjectSave project = new GumProjectSave { FullFileName = ProjectDirectory + "Project.gumx" };
        project.LocalizationFiles.AddRange(files);
        return project;
    }

    private static string Resx(string name, string value) =>
        $"<?xml version=\"1.0\" encoding=\"utf-8\"?><root><data name=\"{name}\"><value>{value}</value></data></root>";
}
