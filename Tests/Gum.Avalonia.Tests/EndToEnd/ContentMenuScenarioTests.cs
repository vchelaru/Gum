using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ConvertToJsonPlugin;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Managers;
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
