using System;
using System.Collections.Generic;
using System.IO;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// A Text whose value is a localization key gets the BBCode of every translation scanned (#7398); the
/// runtime translates a key before it reads tags, so tags that live only in a translation need fonts.
/// </summary>
public class FontReferenceCollectorLocalizationTests : IDisposable
{
    private const string Font = "Arial";
    private const int BaseSize = 18;

    private readonly string _directory;

    public FontReferenceCollectorLocalizationTests()
    {
        StandardElementsManager.Self.Initialize();
        _directory = Path.Combine(Path.GetTempPath(), "gum-loc-fonts-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void Collect_bakes_fonts_of_a_translated_key()
    {
        Dictionary<string, BmfcSave> fonts = Collect("MENU_PLAY", "strings.csv",
            "id,English\nMENU_PLAY,\"[Font=Verdana]Play[/Font]\"\n");

        fonts.Keys.ShouldBe(new[] { Key(BaseSize), Key(BaseSize, "Verdana") }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_bakes_the_union_of_every_language()
    {
        Dictionary<string, BmfcSave> fonts = Collect("MENU_PLAY", "strings.csv",
            "id,English,Spanish\nMENU_PLAY,[Font=Verdana]Play[/Font],[FontSize=30]Jugar[/FontSize]\n");

        fonts.Keys.ShouldBe(new[] { Key(BaseSize), Key(BaseSize, "Verdana"), Key(30) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_bakes_a_key_present_in_only_one_language()
    {
        Dictionary<string, BmfcSave> fonts = Collect("MENU_PLAY", "strings.csv",
            "id,English,Spanish\nMENU_PLAY,Play,[FontSize=30]Jugar[/FontSize]\n");

        fonts.Keys.ShouldBe(new[] { Key(BaseSize), Key(30) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_reads_a_csv_with_a_bom_and_quoted_commas()
    {
        Dictionary<string, BmfcSave> fonts = Collect("MENU_PLAY", "strings.csv",
            "﻿id,English\nMENU_PLAY,\"Hello, [Font=Verdana]world[/Font]\"\n");

        fonts.Keys.ShouldContain(Key(BaseSize, "Verdana"));
    }

    [Fact]
    public void Collect_reads_translations_from_resx_satellites()
    {
        File.WriteAllText(Path.Combine(_directory, "Strings.resx"), Resx("MENU_PLAY", "Play"));
        File.WriteAllText(Path.Combine(_directory, "Strings.es.resx"),
            Resx("MENU_PLAY", "[FontSize=30]Jugar[/FontSize]"));

        Dictionary<string, BmfcSave> fonts = Collect("MENU_PLAY", "Strings.resx", null);

        fonts.Keys.ShouldBe(new[] { Key(BaseSize), Key(30) }, ignoreOrder: true);
    }

    [Fact]
    public void Lookup_returns_each_distinct_tagged_translation_once_and_nothing_for_a_non_key()
    {
        File.WriteAllText(Path.Combine(_directory, "strings.csv"),
            "id,English,Spanish,French\nMENU_PLAY,[b]x[/b],[b]x[/b],plain\n");
        GumProjectSave project = TestProjectBuilder.BuildProject();
        project.FullFileName = Path.Combine(_directory, "Project.gumx");
        project.LocalizationFiles.Add("strings.csv");

        Func<string, IEnumerable<string>> lookup = ProjectTranslations.CreateLookup(project)!;

        lookup("MENU_PLAY").ShouldBe(new[] { "[b]x[/b]" });
        lookup("Not a key").ShouldBeEmpty();
    }

    [Fact]
    public void Collect_ignores_a_text_that_is_not_a_key()
    {
        Dictionary<string, BmfcSave> fonts = Collect("Not a key", "strings.csv",
            "id,English\nMENU_PLAY,[Font=Verdana]Play[/Font]\n");

        fonts.Keys.ShouldBe(new[] { Key(BaseSize) });
    }

    [Fact]
    public void Collect_does_not_translate_a_value_that_already_has_tags()
    {
        // The runtime reads BBCode from the raw value and skips translation.
        Dictionary<string, BmfcSave> fonts = Collect("[FontSize=30]MENU_PLAY[/FontSize]", "strings.csv",
            "id,English\n[FontSize=30]MENU_PLAY[/FontSize],[Font=Verdana]x[/Font]\n");

        fonts.Keys.ShouldBe(new[] { Key(BaseSize), Key(30) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_bakes_the_fonts_of_a_state_tag_inside_a_translation()
    {
        // Pinned against TextRuntimeLocalizedFontTests in MonoGameGum.Tests, which asks the runtime for the
        // same fonts with each language selected.
        Dictionary<string, BmfcSave> fonts = Collect("MENU_PLAY", "strings.csv",
            "id,English,Spanish\nMENU_PLAY,[state=Big]Play[/state],[Font=Verdana]Jugar[/Font]\n",
            text => text.States.Add(BigState()));

        fonts.Keys.ShouldBe(
            new[]
            {
                Key(BaseSize), Key(BaseSize, "StateFaceB"), Key(40, "StateFaceB"), Key(40, Font),
                Key(BaseSize, "Verdana"),
            },
            ignoreOrder: true);
    }

    private static StateSave BigState()
    {
        StateSave state = new StateSave { Name = "Big" };
        SetVariable(state, "Font", "StateFaceB", "string");
        SetVariable(state, "FontSize", 40, "int");
        return state;
    }

    [Fact]
    public void Collect_survives_a_missing_localization_file()
    {
        Dictionary<string, BmfcSave> fonts = Collect("MENU_PLAY", "missing.csv", null);

        fonts.Keys.ShouldBe(new[] { Key(BaseSize) });
    }

    [Fact]
    public void Collect_survives_a_translation_with_an_unknown_state_tag()
    {
        Dictionary<string, BmfcSave> fonts = Collect("MENU_PLAY", "strings.csv",
            "id,English\nMENU_PLAY,[state=Nope]Play[/state]\n");

        fonts.Keys.ShouldBe(new[] { Key(BaseSize) });
    }

    [Fact]
    public void Collect_matches_the_untranslated_scan_when_the_project_has_no_localization()
    {
        Dictionary<string, BmfcSave> fonts = Collect("MENU_PLAY", null, null);

        fonts.Keys.ShouldBe(new[] { Key(BaseSize) });
    }

    private Dictionary<string, BmfcSave> Collect(string textValue, string? localizationFile, string? csv,
        Action<StandardElementSave>? configureText = null)
    {
        if (localizationFile != null && csv != null)
        {
            File.WriteAllText(Path.Combine(_directory, localizationFile), csv);
        }

        ScreenSave screen = TestProjectBuilder.BuildScreen("Menu");
        screen.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = screen });
        SetVariable(screen.DefaultState!, "Label.Text", textValue, "string");

        StandardElementSave text = TestProjectBuilder.BuildStandard("Text");
        SetVariable(text.DefaultState!, "Font", Font, "string");
        SetVariable(text.DefaultState!, "FontSize", BaseSize, "int");

        configureText?.Invoke(text);

        GumProjectSave project = TestProjectBuilder.BuildProject(
            screens: new[] { screen }, standards: new[] { text });
        project.FullFileName = Path.Combine(_directory, "Project.gumx");
        if (localizationFile != null)
        {
            project.LocalizationFiles.Add(localizationFile);
        }
        ObjectFinder.Self.GumProjectSave = project;

        FontReferenceCollector collector = new FontReferenceCollector(
            instance => ObjectFinder.Self.GetElementSave(instance))
        {
            TranslationsOf = ProjectTranslations.CreateLookup(project)
        };
        return collector.Collect(project, new[] { screen });
    }

    private static string Resx(string key, string value) =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><root>"
        + "<resheader name=\"resmimetype\"><value>text/microsoft-resx</value></resheader>"
        + $"<data name=\"{key}\" xml:space=\"preserve\"><value>{value}</value></data></root>";

    private static void SetVariable(StateSave state, string name, object value, string type)
    {
        state.Variables.Add(new VariableSave { SetsValue = true, Name = name, Value = value, Type = type });
    }

    private static string Key(int size, string name = Font) =>
        BmfcSave.GetFontCacheFileNameFor(
            fontSize: size, fontName: name, outline: 0, useFontSmoothing: true,
            isItalic: false, isBold: false, fontFilePath: null);

    public void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        try { Directory.Delete(_directory, true); } catch (IOException) { }
    }
}
