using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the File menu and the project's life on disk (inventory area FILE): saving
/// with auto-save off, Load Recent, reopening the last project on launch, legacy and JSON projects,
/// a change made outside the tool, and saving with no project. Every project is a temp copy.
/// </summary>
[Trait("Category", "EndToEnd")]
public class FileMenuScenarioTests
{
    private static readonly TimeSpan AsyncWork = TimeSpan.FromSeconds(60);

    private static IServiceProvider Services => TestAppBuilder.Services;

    private static IProjectManager ProjectManager => Services.GetRequiredService<IProjectManager>();

    [AvaloniaFact]
    [Trait("Feature", "FILE-005")]
    [Trait("Feature", "FILE-006")]
    public void WithAutoSaveOff_SaveProjectWritesOnlyTheProjectFile_AndSaveAllWritesTheElementsToo()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.SaveAll();
        ProjectPropertiesViewModel properties = ProjectProperties();
        bool autoSave = ProjectManager.AutoSave;
        try
        {
            properties.AutoSave = false;
            tree.Click(tree.NodeFor(card));
            tree.Grid.TypeAndEnter("Width", "175");
            properties.CanvasWidth = 1234;
            tree.Grid.Settle();
            VariableGridHarness.StoredValue(tree.Grid.ReadSaved(card), "Width").ShouldBeNull("auto-save is off");
            SavedProject(tree).DefaultCanvasWidth.ShouldNotBe(1234);

            tree.PickMainMenu("File", "Save Project");

            SavedProject(tree).DefaultCanvasWidth.ShouldBe(1234);
            VariableGridHarness.StoredValue(tree.Grid.ReadSaved(card), "Width").ShouldBeNull("Save Project writes only the project file");

            tree.PickMainMenu("File", "Save All");

            VariableGridHarness.StoredValue(tree.Grid.ReadSaved(card), "Width").ShouldBe(175f);
        }
        finally
        {
            properties.AutoSave = autoSave;
        }

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "FILE-003")]
    [Trait("Feature", "FILE-010")]
    public void LoadRecent_PickingAListedProject_OpensIt_AndTheNextLaunchReopensIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        tree.SaveAll();
        string otherFolder = Path.Combine(Path.GetTempPath(), "GumFileMenuScenarios", Guid.NewGuid().ToString("N"));
        string other = CopyProjectAs(tree, otherFolder, "Other.gumx");
        File.WriteAllText(Path.Combine(otherFolder, "Components", "Badge.gucx"),
            File.ReadAllText(Path.Combine(otherFolder, "Components", "Card.gucx")).Replace("Card", "Badge"));
        File.WriteAllText(other, File.ReadAllText(other).Replace("Name=\"Card\"", "Name=\"Badge\""));

        LoadThroughTheMenu(tree, other);
        LoadThroughTheMenu(tree, tree.Project.ProjectFilePath);
        MenuItemModel listed = Services.GetRequiredService<MenuModel>().GetItem("File")!.Items.Single(item => item.Header == "Load Recent")
            .Items.Single(item => item.Header?.Contains("Other", StringComparison.Ordinal) == true);

        tree.PickMainMenu("File", "Load Recent", listed.Header!);
        tree.WaitUntil(() => IsLoaded(other), AsyncWork, "the recent project to load");

        ProjectManager.GumProjectSave!.Components.Select(component => component.Name).ShouldBe(new[] { "Badge" });
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Badge" });

        // The next launch: startup reads the last project from the settings and opens it again.
        GumProjectSave loadedBefore = ProjectManager.GumProjectSave!;
        Pump(ProjectManager.Initialize());
        ProjectManager.GumProjectSave.ShouldNotBeSameAs(loadedBefore, "the launch loaded the project from disk");
        IsLoaded(other).ShouldBeTrue();
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Badge" });

        LoadThroughTheMenu(tree, tree.Project.ProjectFilePath);
        TryDeleteFolder(otherFolder);
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "FILE-011")]
    [Trait("Feature", "FILE-002")]
    public void LoadProject_OnALegacyVersion1Project_KeepsItsContent_AndPointsToTheUpgradeGuide()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        // A project saved by an old tool, in the verbose pre-attribute format.
        File.WriteAllText(tree.Project.ProjectFilePath, """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Version>1</Version>
              <DefaultCanvasWidth>1024</DefaultCanvasWidth>
              <DefaultCanvasHeight>768</DefaultCanvasHeight>
              <ComponentReference>
                <Name>Panel</Name>
                <ElementType>Component</ElementType>
                <LinkType>ReferenceOriginal</LinkType>
              </ComponentReference>
            </GumProjectSave>
            """);
        Directory.CreateDirectory(Path.Combine(tree.Project.ProjectFolder, "Components"));
        File.WriteAllText(Path.Combine(tree.Project.ProjectFolder, "Components", "Panel.gucx"), """
            <?xml version="1.0" encoding="utf-8"?>
            <ComponentSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Name>Panel</Name>
              <BaseType>Container</BaseType>
              <State>
                <Name>Default</Name>
                <Variable>
                  <Type>float</Type>
                  <Name>Width</Name>
                  <Value xsi:type="xsd:float">150</Value>
                  <SetsValue>true</SetsValue>
                </Variable>
              </State>
              <Instance>
                <Name>Background</Name>
                <BaseType>Rectangle</BaseType>
              </Instance>
            </ComponentSave>
            """);
        GumProjectSave loadedBefore = ProjectManager.GumProjectSave!;

        tree.Dialogs.AnswerNextOpenFile(tree.Project.ProjectFilePath);
        tree.PickMainMenu("File", "Load Project...");
        tree.WaitUntil(() => ProjectManager.GumProjectSave != loadedBefore && ProjectManager.GumProjectSave?.Components.Count == 1, AsyncWork, "the legacy project to load");

        GumProjectSave loaded = ProjectManager.GumProjectSave!;
        ProjectManager.HaveErrorsOccurredLoadingProject.ShouldBeFalse();
        ComponentSave panel = loaded.Components.Single();
        VariableGridHarness.StoredValue(panel, "Width").ShouldBe(150f);
        panel.Instances.Single().BaseType.ShouldBe("Rectangle");
        tree.OutputWritten.ShouldContain("legacy version 1");
        tree.OutputWritten.ShouldContain("upgrading-file-gumx-version");
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Panel" });
        tree.ChildTexts(tree.NodeFor(panel)).ShouldBe(new[] { "Background" });

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "FILE-012")]
    public void LoadProject_OnAJsonProject_OpensItFromTheJsonFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness("Harness.gumj");
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.Project.AddInstance(card, "Title", "Text");
        tree.SaveAll();
        File.ReadAllText(tree.Project.ProjectFilePath).TrimStart().ShouldStartWith("{");
        string cardFile = Path.Combine(tree.Project.ProjectFolder, "Components", "Card." + GumProjectSave.ComponentJsonExtension);
        File.Exists(cardFile).ShouldBeTrue();
        GumProjectSave loadedBefore = ProjectManager.GumProjectSave!;

        LoadThroughTheMenu(tree, tree.Project.ProjectFilePath);

        GumProjectSave loaded = ProjectManager.GumProjectSave!;
        loaded.ShouldNotBeSameAs(loadedBefore);
        loaded.Components.Single().Instances.Single().Name.ShouldBe("Title");
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Card" });
        Directory.GetFiles(tree.Project.ProjectFolder, "*.gumx").ShouldBeEmpty("a JSON project is saved as JSON only");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "FILE-013")]
    public void AnElementFileChangedOutsideTheTool_IsReloaded_AndTheVariablesTabShowsTheNewValue()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.SaveAll();
        tree.Click(tree.NodeFor(card));
        tree.Grid.TypeAndEnter("Width", "100");
        // The tool watches the folders of a project it has opened.
        tree.Project.SaveAndReload();
        string cardFile = Path.Combine(tree.Project.ProjectFolder, "Components", "Card.gucx");
        IFileWatchIgnoreList ignoreList = Services.GetRequiredService<IFileWatchIgnoreList>();
        IFileWatchManager fileWatch = Services.GetRequiredService<IFileWatchManager>();
        // The tool ignores changes to a file it has just saved itself for a few seconds.
        tree.WaitUntil(() => !ignoreList.TryGetIgnoreFileChange(new FilePath(cardFile)), TimeSpan.FromSeconds(15), "the tool's own save of Card to stop being ignored");

        string saved = File.ReadAllText(cardFile);
        string oldValue = "<Value xsi:type=\"xsd:float\">100</Value>";
        saved.ShouldContain(oldValue);
        // Written in the file's own encoding, as an editor saving it would.
        bool hasBom = File.ReadAllBytes(cardFile).Take(3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF });
        File.WriteAllText(cardFile, saved.Replace(oldValue, "<Value xsi:type=\"xsd:float\">321</Value>", StringComparison.Ordinal),
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: hasBom));

        // What the tool's periodic flush timer does.
        tree.WaitUntil(() =>
        {
            fileWatch.Flush();
            return VariableGridHarness.StoredValue(CurrentCard(), "Width") is 321f;
        }, TimeSpan.FromSeconds(20), $"the tool to reload Card (enabled {fileWatch.Enabled}, watching [{string.Join(", ", fileWatch.CurrentFilePathsWatching)}], waiting [{string.Join(", ", fileWatch.ChangedFilesWaitingForFlush)}], file {File.ReadAllText(cardFile)})");

        tree.Click(tree.RootNode("Components"));
        tree.Click(tree.NodeFor(CurrentCard()));
        tree.Grid.FieldText("Width").ShouldBe("321");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "FILE-014")]
    public void SaveProjectAndSaveAll_AfterAProjectFailsToLoad_SayThereIsNoProject()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.SaveAll();
        ProjectFileSnapshot start = tree.SnapshotFiles();
        // A project on the recent list that was deleted since: picking it loads nothing.
        string goneFolder = Path.Combine(Path.GetTempPath(), "GumFileMenuScenarios", Guid.NewGuid().ToString("N"));
        string gone = CopyProjectAs(tree, goneFolder, "Gone.gumx");
        LoadThroughTheMenu(tree, gone);
        LoadThroughTheMenu(tree, tree.Project.ProjectFilePath);
        Directory.Delete(goneFolder, recursive: true);
        for (int i = 0; i < 3; i++)
        {
            tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        }
        MenuItemModel listed = Services.GetRequiredService<MenuModel>().GetItem("File")!.Items.Single(item => item.Header == "Load Recent")
            .Items.Single(item => item.Header?.Contains("Gone", StringComparison.Ordinal) == true);
        tree.PickMainMenu("File", "Load Recent", listed.Header!);
        tree.WaitUntil(() => ProjectManager.GumProjectSave == null, AsyncWork, "the missing project's load to leave no project");
        int shownByTheLoad = tree.Dialogs.Messages.Count;

        tree.PickMainMenu("File", "Save Project");
        tree.PickMainMenu("File", "Save All");

        tree.Dialogs.Messages.Skip(shownByTheLoad).ShouldBe(new[]
        {
            "There is no project loaded.  Either load a project or create a new project before saving",
            "There is no project loaded.  Either load a project or create a new project before saving",
        }, "load messages: " + string.Join(" | ", tree.Dialogs.Messages.Take(shownByTheLoad)));
        tree.SnapshotFiles().ShouldMatch(start, "nothing is saved without a project");

        LoadThroughTheMenu(tree, tree.Project.ProjectFilePath);
        tree.AssertOracles();
    }

    [SkippableFact]
    [Trait("Feature", "FILE-007")]
    public void ExportAsImage_WritesTheCanvasToAPng_AndPutsTheEditorsGuidesBack() =>
        ExportButtonAsImage((canvas, image, box) =>
        {
            (image.Width, image.Height).ShouldBe(((int)canvas.Canvas.Bounds.Width, (int)canvas.Canvas.Bounds.Height), "the whole canvas is exported");
            SkiaSharp.SKColor background = image.GetPixel(image.Width - 5, image.Height - 5);
            box.ShouldContain(pixel => pixel != background, "the rectangle is drawn over the background");
        });

    [SkippableFact(Skip = "#5384: the editor's solid background rectangle fills the exported image")]
    [Trait("Feature", "FILE-007")]
    public void ExportAsImage_LeavesTheBackgroundTransparent() =>
        ExportButtonAsImage((_, image, _) =>
            image.GetPixel(image.Width - 5, image.Height - 5).Alpha.ShouldBe((byte)0, "nothing but the element is drawn"));

    // Exports a Button holding a rectangle at (20, 20, 60 x 40) through File > Export > Export as
    // Image, checks the canvas guides come back, and hands the image and the rectangle's pixels on.
    private static void ExportButtonAsImage(Action<CanvasHarness, SkiaSharp.SKBitmap, List<SkiaSharp.SKColor>> check)
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.AddInstance(button, "Box", "Rectangle", x: 20, y: 20, width: 60, height: 40);
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            canvas.Frame();
            WireframeGuides before = WireframeGuides.Take();
            string folder = Path.Combine(Path.GetTempPath(), "GumFileMenuScenarios", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string png = Path.Combine(folder, "Button.png");
            try
            {
                canvas.Project.Dialogs.AnswerNextSaveFile(png);
                canvas.Tree.PickMainMenu("File", "Export", "Export as Image");
                // The capture happens on the next frame the canvas draws.
                canvas.Frame();

                File.Exists(png).ShouldBeTrue(canvas.Describe());
                WireframeGuides.Take().ShouldBe(before, "the rulers, bounds, background and highlights come back");
                using SkiaSharp.SKBitmap image = SkiaSharp.SKBitmap.Decode(png);
                global::Avalonia.Point origin = global::Avalonia.VisualExtensions.TranslatePoint(canvas.Canvas, default, canvas.Input.Window)!.Value;
                global::Avalonia.Point topLeft = canvas.WindowPointOf(20, 20);
                global::Avalonia.Point bottomRight = canvas.WindowPointOf(80, 60);
                List<SkiaSharp.SKColor> box = new List<SkiaSharp.SKColor>();
                for (int x = (int)(topLeft.X - origin.X); x <= (int)(bottomRight.X - origin.X); x++)
                {
                    for (int y = (int)(topLeft.Y - origin.Y); y <= (int)(bottomRight.Y - origin.Y); y++)
                    {
                        box.Add(image.GetPixel(x, y));
                    }
                }
                check(canvas, image, box);
            }
            finally
            {
                TryDeleteFolder(folder);
            }

            canvas.AssertOracles();
        });
    }

    private sealed record WireframeGuides(bool Rulers, bool CanvasBounds, bool Background, bool Highlights)
    {
        public static WireframeGuides Take()
        {
            Gum.Commands.IWireframeCommands commands = Services.GetRequiredService<Gum.Commands.IWireframeCommands>();
            return new WireframeGuides(commands.AreRulersVisible, commands.AreCanvasBoundsVisible,
                commands.IsBackgroundGridVisible, commands.AreHighlightsVisible);
        }
    }

    private static ComponentSave CurrentCard() => ProjectManager.GumProjectSave!.Components.Single(component => component.Name == "Card");

    private static ProjectPropertiesViewModel ProjectProperties()
    {
        AvaloniaTabManager tabs = (AvaloniaTabManager)Services.GetRequiredService<ITabManager>();
        AvaloniaPluginTab tab = tabs.AllTabs.Single(candidate => candidate.Title == "Project Properties");
        return (ProjectPropertiesViewModel)((global::Avalonia.Controls.Control)tab.Content).DataContext!;
    }

    private static GumProjectSave SavedProject(ProjectTreeHarness tree) =>
        GumProjectSave.Load(tree.Project.ProjectFilePath, out _) ?? throw new InvalidOperationException("The project file did not load.");

    private static bool IsLoaded(string projectFile) =>
        ProjectManager.GumProjectSave?.FullFileName is { } loaded && new FilePath(loaded) == new FilePath(projectFile);

    private static void LoadThroughTheMenu(ProjectTreeHarness tree, string projectFile)
    {
        GumProjectSave? loadedBefore = ProjectManager.GumProjectSave;
        tree.Dialogs.AnswerNextOpenFile(projectFile);
        tree.PickMainMenu("File", "Load Project...");
        tree.WaitUntil(() => ProjectManager.GumProjectSave != loadedBefore && IsLoaded(projectFile), AsyncWork, $"{Path.GetFileName(projectFile)} to load");
    }

    private static void TryDeleteFolder(string folder)
    {
        try
        {
            Directory.Delete(folder, recursive: true);
        }
        catch (IOException)
        {
            // A file watcher may still hold it; the temp folder is cleaned up later.
        }
    }

    // Copies the harness's saved project into another folder, renaming the project file.
    private static string CopyProjectAs(ProjectTreeHarness tree, string folder, string projectFileName)
    {
        foreach (string file in Directory.GetFiles(tree.Project.ProjectFolder, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(tree.Project.ProjectFolder, file);
            if (relative.StartsWith("UserData", StringComparison.Ordinal))
            {
                continue;
            }
            string target = Path.GetFullPath(file) == Path.GetFullPath(tree.Project.ProjectFilePath)
                ? Path.Combine(folder, projectFileName)
                : Path.Combine(folder, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        return Path.Combine(folder, projectFileName);
    }

    // [AvaloniaFact] tests stay synchronous; the work posts to the UI thread it waits on.
    private static void Pump(Task task)
    {
        DateTime deadline = DateTime.UtcNow + AsyncWork;
        while (!task.IsCompleted)
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The launch's project load did not finish.");
            }
            Thread.Sleep(10);
            Dispatcher.UIThread.RunJobs();
        }
        task.GetAwaiter().GetResult();
        Dispatcher.UIThread.RunJobs();
    }
}
