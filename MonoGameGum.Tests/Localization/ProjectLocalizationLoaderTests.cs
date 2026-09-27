using Gum.Bundle;
using Gum.DataTypes;
using Gum.Localization;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Localization;

/// <summary>
/// Bundle-mode loading, where RESX satellites come from the bundle's entries instead of a directory,
/// and path separators in both modes. Loose-mode loading is covered end to end by
/// GumServiceLocalizationAutoLoadTests.
/// </summary>
public class ProjectLocalizationLoaderTests
{
    [Fact]
    public void Load_from_bundle_merges_resx_files_with_their_own_folder_satellites_and_warns_on_missing_files()
    {
        GumProjectSave project = ProjectWithLocalizationFiles("Strings.resx", "Text/Buttons.resx", "Missing.resx");
        IGumFileProvider bundle = BundleWith(
            ("Strings.resx", Resx("T_Hello", "Hello")),
            ("Strings.es.resx", Resx("T_Hello", "Hola")),
            ("Text/Buttons.resx", Resx("T_Ok", "OK")),
            ("Text/Buttons.es.resx", Resx("T_Ok", "Vale")),
            ("Other/Strings.fr.resx", Resx("T_Hello", "Bonjour")));
        LocalizationService service = new LocalizationService();
        List<string> warnings = new List<string>();

        ProjectLocalizationLoader.Load(project, service, bundle, warnings);

        warnings.ShouldHaveSingleItem().ShouldContain("Missing.resx");
        service.Languages.ShouldBe(new[] { "Default", "es" });
        service.CurrentLanguage = 2;
        service.Translate("T_Hello").ShouldBe("Hola");
        service.Translate("T_Ok").ShouldBe("Vale");
    }

    [Fact]
    public void Load_from_bundle_skips_mixed_csv_and_resx_with_a_warning()
    {
        GumProjectSave project = ProjectWithLocalizationFiles("Strings.csv", "Strings.resx");
        IGumFileProvider bundle = BundleWith(
            ("Strings.csv", "ID,English\nT_Hello,Hello\n"),
            ("Strings.resx", Resx("T_Hello", "Hello")));
        LocalizationService service = new LocalizationService();
        List<string> warnings = new List<string>();

        ProjectLocalizationLoader.Load(project, service, bundle, warnings);

        warnings.ShouldHaveSingleItem().ShouldContain("not all are .resx");
        service.HasDatabase.ShouldBeFalse();
    }

    [Fact]
    public void Load_from_bundle_finds_a_file_saved_with_windows_separators()
    {
        GumProjectSave project = ProjectWithLocalizationFiles("Text\\Strings.resx");
        IGumFileProvider bundle = BundleWith(
            ("Text/Strings.resx", Resx("T_Hello", "Hello")),
            ("Text/Strings.es.resx", Resx("T_Hello", "Hola")));
        LocalizationService service = new LocalizationService();
        List<string> warnings = new List<string>();

        ProjectLocalizationLoader.Load(project, service, bundle, warnings);

        warnings.ShouldBeEmpty();
        service.CurrentLanguage = 2;
        service.Translate("T_Hello").ShouldBe("Hola");
    }

    // A project saved on Windows stores "Localization\Strings.resx". A loose path goes straight to
    // File.Exists and Directory.GetFiles, so both separators must come out as the native one:
    // a backslash is a file-name character on macOS/Linux.
    [Theory]
    [InlineData("Localization\\Strings.resx")]
    [InlineData("Localization/Strings.resx")]
    public void Loose_file_paths_use_native_separators(string storedPath)
    {
        string projectDirectory = "/game/Content/GumProject/";

        string relativePath = ProjectLocalizationLoader.ToRelativePaths(new[] { storedPath }).ShouldHaveSingleItem();
        string loosePath = ProjectLocalizationLoader.ToLooseFilePath(projectDirectory, relativePath);

        loosePath.ShouldBe("/game/Content/GumProject/Localization/Strings.resx".Replace('/', System.IO.Path.DirectorySeparatorChar));
    }

    [Fact]
    public void ToRelativePaths_skips_empty_entries()
    {
        List<string> relativePaths = ProjectLocalizationLoader.ToRelativePaths(new[] { "", null, "Strings.csv" });

        relativePaths.ShouldBe(new[] { "Strings.csv" });
    }

    [Fact]
    public void Load_loose_csv_with_a_short_row_falls_back_to_the_string_id()
    {
        using TempProjectDirectory directory = new TempProjectDirectory();
        directory.Write("Loc/Strings.csv", "String ID,English,Spanish\nT_Hello,Hello\nT_Bye,Bye,Adios\n");
        GumProjectSave project = directory.ProjectWithLocalizationFiles("Loc\\Strings.csv");
        LocalizationService service = new LocalizationService();
        List<string> warnings = new List<string>();

        ProjectLocalizationLoader.Load(project, service, null, warnings);

        warnings.ShouldBeEmpty();
        service.CurrentLanguage = 2;
        service.Translate("T_Hello").ShouldBe("T_Hello");
        service.Translate("T_Bye").ShouldBe("Adios");
    }

    [Fact]
    public void Load_loose_csv_with_a_repeated_id_warns_and_keeps_the_later_row()
    {
        using TempProjectDirectory directory = new TempProjectDirectory();
        directory.Write("Strings.csv", "String ID,English\nT_Hello,First\nT_Hello,Second\n");
        GumProjectSave project = directory.ProjectWithLocalizationFiles("Strings.csv");
        LocalizationService service = new LocalizationService();
        List<string> warnings = new List<string>();

        ProjectLocalizationLoader.Load(project, service, null, warnings);

        warnings.ShouldHaveSingleItem().ShouldContain("'T_Hello'");
        service.CurrentLanguage = 1;
        service.Translate("T_Hello").ShouldBe("Second");
    }

    [Fact]
    public void Load_loose_resx_finds_its_satellites()
    {
        using TempProjectDirectory directory = new TempProjectDirectory();
        directory.Write("Loc/Strings.resx", Resx("T_Hello", "Hello"));
        directory.Write("Loc/Strings.es.resx", Resx("T_Hello", "Hola"));
        GumProjectSave project = directory.ProjectWithLocalizationFiles("Loc\\Strings.resx");
        LocalizationService service = new LocalizationService();
        List<string> warnings = new List<string>();

        ProjectLocalizationLoader.Load(project, service, null, warnings);

        warnings.ShouldBeEmpty();
        service.Languages.ShouldBe(new[] { "Default", "es" });
        service.CurrentLanguage = 2;
        service.Translate("T_Hello").ShouldBe("Hola");
    }

    // One missing file warns and skips, like one missing file among several already did.
    [Theory]
    [InlineData("Missing.csv")]
    [InlineData("Missing.resx")]
    public void Load_loose_single_missing_file_warns_instead_of_throwing(string fileName)
    {
        using TempProjectDirectory directory = new TempProjectDirectory();
        GumProjectSave project = directory.ProjectWithLocalizationFiles(fileName);
        LocalizationService service = new LocalizationService();
        List<string> warnings = new List<string>();

        ProjectLocalizationLoader.Load(project, service, null, warnings);

        warnings.ShouldHaveSingleItem().ShouldContain(fileName);
        service.HasDatabase.ShouldBeFalse();
    }

    private sealed class TempProjectDirectory : IDisposable
    {
        private readonly string _path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "GumProjectLocalizationLoaderTests_" + Guid.NewGuid().ToString("N"));

        public TempProjectDirectory() => System.IO.Directory.CreateDirectory(_path);

        public void Write(string relativePath, string content)
        {
            string fullPath = System.IO.Path.Combine(_path, relativePath);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
            System.IO.File.WriteAllText(fullPath, content);
        }

        public GumProjectSave ProjectWithLocalizationFiles(params string[] files)
        {
            GumProjectSave project = new GumProjectSave { FullFileName = System.IO.Path.Combine(_path, "Project.gumx") };
            project.LocalizationFiles.AddRange(files);
            return project;
        }

        public void Dispose()
        {
            try
            {
                System.IO.Directory.Delete(_path, recursive: true);
            }
            catch (System.IO.IOException)
            {
                // Best-effort cleanup of a temp folder.
            }
        }
    }

    private static GumProjectSave ProjectWithLocalizationFiles(params string[] files)
    {
        GumProjectSave project = new GumProjectSave { FullFileName = "C:/Game/Content/Project.gumx" };
        project.LocalizationFiles.AddRange(files);
        return project;
    }

    private static string Resx(string name, string value) =>
        $"<?xml version=\"1.0\" encoding=\"utf-8\"?><root><data name=\"{name}\"><value>{value}</value></data></root>";

    private static IGumFileProvider BundleWith(params (string path, string content)[] entries)
    {
        Dictionary<string, byte[]> bytes = entries.ToDictionary(e => e.path, e => Encoding.UTF8.GetBytes(e.content), StringComparer.Ordinal);
        List<string> ordered = entries.Select(e => e.path).OrderBy(p => p, StringComparer.Ordinal).ToList();
        return new BundleGumFileProvider(new GumBundle(1, bytes, ordered));
    }
}
