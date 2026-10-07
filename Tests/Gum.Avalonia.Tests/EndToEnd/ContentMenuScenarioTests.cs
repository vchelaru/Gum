using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ConvertToJsonPlugin;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.ProjectServices.FontGeneration;
using Gum.Services;
using Gum.Services.Dialogs;
using HtmlToGumPlugin;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the Content menu (inventory area CONT): finding file references, the font
/// cache items, the orphaned-code scan, Convert to JSON and the HTML import's options, each picked
/// from the main menu over a temp project.
/// </summary>
[Trait("Category", "EndToEnd")]
public class ContentMenuScenarioTests
{
    private static readonly TimeSpan AsyncWork = TimeSpan.FromSeconds(60);

    private static IServiceProvider Services => TestAppBuilder.Services;

    private static IProjectManager ProjectManager => Services.GetRequiredService<IProjectManager>();

    // The head starts without the HTML import item when no converter is found, as in the test
    // output; a test that picks the item adds it once its converter folder is set.
    private static MainHtmlToGumPlugin HtmlPlugin =>
        Services.GetRequiredService<PluginManager>().InitializedPlugins.OfType<MainHtmlToGumPlugin>().Single();

    [AvaloniaFact]
    [Trait("Feature", "CONT-001")]
    public void FindFileReferences_ListsTheElementsUsingTheFile_OrSaysNothingDoes()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.Project.AddInstance(card, "Icon", "Sprite");
        card.GetDefaultStateOrThrow().SetValue("Icon.SourceFile", "Art/Hero.png", "string");
        tree.Project.AddComponent("Panel");
        tree.SaveAll();
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNextUserString("Hero");
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.PickMainMenu("Content", "Find file references...");
        tree.Dialogs.AnswerNextUserString("Villain.png");
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.PickMainMenu("Content", "Find file references...");

        tree.Dialogs.Messages.ShouldBe(new[]
        {
            "File referenced by:\nCard (Container)",
            "File referenced by:\nNothing references this file",
        });
        tree.SnapshotFiles().ShouldMatch(start, "finding references changes no file");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "CONT-002")]
    [Trait("Feature", "CONT-003")]
    [Trait("Feature", "CONT-004")]
    [Trait("Feature", "CONT-005")]
    public void FontCacheItems_ViewCreatesAndOpensTheFolder_RecreateSkipsFontsOnDisk_ForceRedoesThem_AndClearEmptiesTheFolder()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.Project.AddInstance(card, "Title", "Text");
        tree.SaveAll();
        RecordingFileSystemRevealService reveal = (RecordingFileSystemRevealService)Services.GetRequiredService<IFileSystemRevealService>();
        NoOpFontFileGenerator fonts = (NoOpFontFileGenerator)Services.GetRequiredService<IFontFileGenerator>();
        reveal.Clear();
        string fontCache = Path.Combine(tree.Project.ProjectFolder, "FontCache");
        TryDeleteFolder(fontCache);

        tree.PickMainMenu("Content", "View Font Cache");
        Directory.Exists(fontCache).ShouldBeTrue("viewing the cache creates its folder");
        new FilePath(reveal.Requests.Single().Substring("OpenFolder: ".Length)).ShouldBe(new FilePath(fontCache + "/"));

        // Nothing is on disk, so re-creating the missing fonts asks for the project's font.
        int requested = fonts.RequestedFntPaths().Count;
        tree.PickMainMenu("Content", "Re-create missing font files");
        tree.WaitUntil(() => fonts.RequestedFntPaths().Count > requested, AsyncWork, "the missing font to be requested");
        string font = fonts.RequestedFntPaths().Skip(requested).First();
        new FilePath(font).FullPath.ShouldStartWith(new FilePath(fontCache + "/").FullPath);

        // With every font on disk, re-creating the missing ones asks for none, and forcing asks again.
        foreach (string path in fonts.RequestedFntPaths().Skip(requested).Distinct())
        {
            File.WriteAllText(path, "info");
        }
        requested = fonts.RequestedFntPaths().Count;
        tree.PickMainMenu("Content", "Re-create missing font files");
        Settle(tree);
        fonts.RequestedFntPaths().Count.ShouldBe(requested, "no font is missing");
        tree.PickMainMenu("Content", "Force re-create all font files");
        tree.WaitUntil(() => fonts.RequestedFntPaths().Skip(requested).Contains(font), AsyncWork, "the font to be requested again");

        tree.PickMainMenu("Content", "Clear Font Cache");
        Directory.EnumerateFileSystemEntries(fontCache).ShouldBeEmpty();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "CONT-006")]
    public void ScanForOrphanedCodeFiles_FindsAGeneratedFileWithNoElement()
    {
        using CodeTabHarness code = new CodeTabHarness();
        ComponentSave card = code.Project.AddComponent("Card");
        code.Tree.SaveAll();
        code.Select(card);
        code.SetUpManualGeneration();
        code.ClickGenerate();
        string generated = code.CodeFile("Components/Card.Generated.cs");
        string orphan = code.CodeFile("Components/Gone.Generated.cs");
        File.WriteAllText(orphan, File.ReadAllText(generated).Replace("Card", "Gone"));

        code.Project.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        int shown = code.Project.Dialogs.Messages.Count;
        code.Tree.PickMainMenu("Content", "Scan for Orphaned Code Files…");
        code.Tree.WaitUntil(() => code.Project.Dialogs.Messages.Count > shown, AsyncWork, "the scan's summary");

        code.Project.Dialogs.Messages.Last().ShouldStartWith("Found 1 orphaned code file(s).");
        File.Exists(orphan).ShouldBeTrue("the scan only reports");
        File.Exists(generated).ShouldBeTrue();

        code.AssertOracles();
    }

    [AvaloniaFact]
    public void MigrateCodeFiles_AfterAnOutputLibrarySwitch_MovesCustomCode_AndRestoreLastPutsItBack()
    {
        using CodeTabHarness code = new CodeTabHarness();
        ComponentSave card = code.Project.AddComponent("Card");
        code.Tree.SaveAll();
        code.Select(card);
        code.SetUpManualGeneration(library: "MonoGame (deprecated)");
        code.ClickGenerate();
        string oldGenerated = code.CodeFile("Components/CardRuntime.Generated.cs");
        string oldCustom = code.CodeFile("Components/CardRuntime.cs");
        File.WriteAllText(oldCustom, File.ReadAllText(oldCustom).Replace("partial void CustomInitialize()", "int userField;\n        partial void CustomInitialize()"));
        // Gum Forms names the class Card, so the Runtime files are left at old paths.
        code.PickComboItem("Output Library", "Gum Forms (recommended)");
        code.ClickGenerate();
        string newCustom = code.CodeFile("Components/Card.cs");
        File.Exists(newCustom).ShouldBeTrue();

        string? planText = null;
        code.Project.Dialogs.AnswerNextMessageInWindow(window =>
        {
            planText = window.Text();
            if (PrScreenshot.OutputDirectory != null)
            {
                PrScreenshot.SaveWindow(window.Window, "migrate-code-files");
            }
            window.ClickButton("Migrate");
        });
        code.Project.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        int shown = code.Project.Dialogs.Messages.Count;
        code.Tree.PickMainMenu("Content", "Migrate Code Files…");
        code.Tree.WaitUntil(() => code.Project.Dialogs.Messages.Count >= shown + 2, AsyncWork, "the migration's plan and result");

        planText.ShouldNotBeNull().ShouldContain("Components/CardRuntime.cs -> Components/Card.cs");
        File.Exists(oldGenerated).ShouldBeFalse();
        File.Exists(oldCustom).ShouldBeFalse();
        File.ReadAllText(newCustom).ShouldContain("int userField;");
        File.ReadAllText(newCustom).ShouldContain("partial class Card");

        code.Project.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        code.Project.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        code.Tree.PickMainMenu("Content", "Restore Last Code File Migration…");

        File.ReadAllText(oldCustom).ShouldContain("int userField;");
        File.Exists(oldGenerated).ShouldBeTrue();
        code.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "CONT-009")]
    [Trait("Feature", "DLG-024")]
    public void ConvertToJson_WithRecycling_OpensTheJsonProject_AndRemovesTheXmlFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.Project.AddInstance(card, "Title", "Text");
        tree.Project.AddScreen("TitleScreen");
        tree.SaveAll();
        string jsonProject = Path.ChangeExtension(tree.Project.ProjectFilePath, GumProjectSave.ProjectJsonExtension);
        string shownMessage = "";
        tree.Dialogs.AnswerNextInWindow<ConvertToJsonDialogViewModel>(window =>
        {
            shownMessage = window.Text();
            window.Click(window.Find<CheckBox>());
            window.Click(window.AffirmativeButton);
        });
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);

        tree.PickMainMenu("Content", "Convert to JSON…");
        tree.WaitUntil(() => tree.Dialogs.Messages.Any(message => message.StartsWith("Converted", StringComparison.Ordinal)), AsyncWork, "the conversion to finish");

        shownMessage.ShouldContain("Harness.gumj");
        new FilePath(ProjectManager.GumProjectSave!.FullFileName!).ShouldBe(new FilePath(jsonProject));
        File.Exists(tree.Project.ProjectFilePath).ShouldBeFalse("the XML project was recycled");
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Card.gucx")).ShouldBeFalse();
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Card." + GumProjectSave.ComponentJsonExtension)).ShouldBeTrue();
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Card" });
        tree.ChildTexts(tree.RootNode("Screens")).ShouldBe(new[] { "TitleScreen" });
        // The harness's oracles follow its .gumx; the tree is checked against the .gumj instead.
        ProjectOracles.AssertTreeMatchesSavedProject(tree.TreeManager, jsonProject);
        GumProjectSave reopened = GumProjectSave.Load(jsonProject, out GumLoadResult result)!;
        result.ErrorMessage.ShouldBeNullOrEmpty();
        reopened.Components.Single().Instances.Single().Name.ShouldBe("Title");
        tree.ThrowIfCrashed();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-025")]
    public void ImportHtml_OptionsWindow_BrowsesForTheFile_RemembersTheChoices_AndSaysWhenTheConverterIsMissing()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        string folder = Path.Combine(Path.GetTempPath(), "GumHtmlImportScenario", Guid.NewGuid().ToString("N"));
        string converter = Path.Combine(folder, "NoConverter");
        Directory.CreateDirectory(converter);
        string page = Path.Combine(folder, "landing-page.html");
        File.WriteAllText(page, "<html><body><h1>Hi</h1></body></html>");
        string? originalConverter = Environment.GetEnvironmentVariable("HTMLTOGUM_CONVERTER");
        ProjectFileSnapshot start = tree.SnapshotFiles();
        string offeredScreen = "";
        try
        {
            // An empty converter folder: the import stops before it would start Node.js.
            Environment.SetEnvironmentVariable("HTMLTOGUM_CONVERTER", converter);
            HtmlPlugin.AddImportMenuEntryIfConverterFound();
            tree.Dialogs.AnswerNextOpenFile(page);
            tree.Dialogs.AnswerNextInWindow<ImportHtmlOptionsViewModel>(window =>
            {
                ImportHtmlOptionsViewModel options = (ImportHtmlOptionsViewModel)window.Window.DataContext!;
                window.Click(window.Find<Button>(button => button.Command == options.BrowseCommand));
                offeredScreen = options.ScreenName;
                window.TypeInto(window.FindAll<TextBox>()[1], "main");
                window.Click(window.AffirmativeButton);
            });
            tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
            tree.PickMainMenu("Content", "Import", "HTML…");
            tree.WaitUntil(() => tree.Dialogs.Messages.Count > 0, AsyncWork, "the import to stop");

            tree.Dialogs.Messages.Single().ShouldStartWith("Converter not found.");
            offeredScreen.ShouldNotBeNullOrEmpty();

            // The next import offers what this one was given.
            ImportHtmlOptionsViewModel? reopened = null;
            tree.Dialogs.AnswerNext<ImportHtmlOptionsViewModel>(options => { reopened = options; return false; });
            tree.PickMainMenu("Content", "Import", "HTML…");
            reopened.ShouldNotBeNull();
            (reopened.HtmlPath, reopened.Selector, reopened.ScreenName).ShouldBe((page, "main", offeredScreen));
        }
        finally
        {
            Environment.SetEnvironmentVariable("HTMLTOGUM_CONVERTER", originalConverter);
            TryDeleteFolder(folder);
        }

        tree.SnapshotFiles().ShouldMatch(start, "an import that stopped changes no file");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "CONT-007")]
    [Trait("Feature", "DLG-026")]
    public void ImportHtml_ImportsTheConvertedScreenAndItsImages_SelectsIt_AndShowsTheResult()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        string folder = Path.Combine(Path.GetTempPath(), "GumHtmlImportScenario", Guid.NewGuid().ToString("N"));
        string converter = Path.Combine(folder, "Converter");
        Directory.CreateDirectory(converter);
        File.WriteAllText(Path.Combine(converter, "convert.mjs"), "");
        string page = Path.Combine(folder, "landing-page.html");
        File.WriteAllText(page, "<html><body><h1>Hi</h1></body></html>");
        byte[] logo = { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 };
        FakeHtmlConverter fake = new FakeHtmlConverter(titleText: "Welcome", logo);
        MainHtmlToGumPlugin plugin = HtmlPlugin;
        IHtmlConverterProcessRunner originalRunner = plugin.ProcessRunner;
        string? originalConverter = Environment.GetEnvironmentVariable("HTMLTOGUM_CONVERTER");
        string resultMessage = "";
        string resultDetails = "";
        string? resultTitle = null;
        try
        {
            Environment.SetEnvironmentVariable("HTMLTOGUM_CONVERTER", converter);
            plugin.AddImportMenuEntryIfConverterFound();
            plugin.ProcessRunner = fake;
            tree.Dialogs.AnswerNextOpenFile(page);
            tree.Dialogs.AnswerNextInWindow<ImportHtmlOptionsViewModel>(window =>
            {
                ImportHtmlOptionsViewModel options = (ImportHtmlOptionsViewModel)window.Window.DataContext!;
                window.Click(window.Find<Button>(button => button.Command == options.BrowseCommand));
                window.Click(window.AffirmativeButton);
            });
            tree.Dialogs.AnswerNextInWindow<ImportHtmlResultViewModel>(window =>
            {
                resultTitle = window.Title;
                resultMessage = window.Text();
                window.ClickButton("Show details ▾");
                resultDetails = window.Find<TextBox>(box => box.IsEffectivelyVisible).Text ?? "";
                window.Click(window.AffirmativeButton);
            });
            tree.PickMainMenu("Content", "Import", "HTML…");
            tree.WaitUntil(() => resultTitle != null, AsyncWork, "the import's result dialog");
        }
        finally
        {
            plugin.ProcessRunner = originalRunner;
            Environment.SetEnvironmentVariable("HTMLTOGUM_CONVERTER", originalConverter);
            TryDeleteFolder(folder);
        }

        tree.Dialogs.Messages.ShouldBeEmpty();
        fake.ConverterArguments.ShouldContain(page);
        ScreenSave imported = ObjectFinder.Self.GumProjectSave!.Screens.ShouldHaveSingleItem();
        imported.Name.ShouldBe("landing_page");
        imported.Instances.ShouldHaveSingleItem().Name.ShouldBe("Title");
        tree.SelectedState.SelectedScreen.ShouldBe(imported);
        tree.ChildTexts(tree.RootNode("Screens")).ShouldBe(new[] { "landing_page" });
        File.ReadAllBytes(Path.Combine(tree.Project.ProjectFolder, "Images", "logo.png")).ShouldBe(logo);
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Screens", "landing_page.gusx")).ShouldBeTrue();
        resultMessage.ShouldContain("Imported and selected screen \"landing_page\".");
        resultTitle.ShouldBe("Import HTML");
        resultDetails.ShouldContain("converted Welcome");

        // Adding a whole screen records no undo step.
        ProjectFileSnapshot afterImport = tree.SnapshotFiles();
        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(afterImport, "undo after an import should leave the files alone");
        tree.AssertOracles();
    }

    // Stands in for Node.js and converter/convert.mjs: writes the screen and an image into the
    // staging folder the plugin passes with --out, as the converter does.
    private sealed class FakeHtmlConverter : IHtmlConverterProcessRunner
    {
        private readonly string _titleText;
        private readonly byte[] _logo;

        public FakeHtmlConverter(string titleText, byte[] logo)
        {
            _titleText = titleText;
            _logo = logo;
            ConverterArguments = "";
        }

        public string ConverterArguments { get; private set; }

        public bool TryFindNode(out string nodePath, out string hint)
        {
            nodePath = "node";
            hint = "Found v22 (fake)";
            return true;
        }

        public Task<(int exitCode, string stdout, string stderr)> RunAsync(
            string fileName, string arguments, string workingDirectory, IProgress<string> progress)
        {
            ConverterArguments = arguments;
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(
                arguments, "\" (\\S+) \\d+ \\d+ --out=\"([^\"]+)\"");
            match.Success.ShouldBeTrue($"the converter's arguments should name the screen and --out: {arguments}");
            string screenName = match.Groups[1].Value;
            string stageDir = match.Groups[2].Value;
            Directory.CreateDirectory(Path.Combine(stageDir, "Screens"));
            File.WriteAllText(Path.Combine(stageDir, "Screens", screenName + ".gusx"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<ScreenSave xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\n" +
                $"  <Name>{screenName}</Name>\n" +
                "  <Instance>\n    <Name>Title</Name>\n    <BaseType>Text</BaseType>\n    <DefinedByBase>false</DefinedByBase>\n  </Instance>\n" +
                "</ScreenSave>\n");
            Directory.CreateDirectory(Path.Combine(stageDir, "Images"));
            File.WriteAllBytes(Path.Combine(stageDir, "Images", "logo.png"), _logo);
            return Task.FromResult((0, $"converted {_titleText}\n", ""));
        }
    }

    // Lets posted and async menu work run out.
    private static void Settle(ProjectTreeHarness tree)
    {
        DateTime until = DateTime.UtcNow.AddMilliseconds(300);
        tree.WaitUntil(() => DateTime.UtcNow > until, AsyncWork, "the menu's work to settle");
    }

    private static void TryDeleteFolder(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (IOException)
        {
            // A file watcher may still hold it; the temp folder is cleaned up later.
        }
    }
}
