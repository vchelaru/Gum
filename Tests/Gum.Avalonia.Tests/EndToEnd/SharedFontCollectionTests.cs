using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Tests.Harness;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ProjectServices;
using Gum.ProjectServices.FontGeneration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Project load runs four passes that read every Text's font: the tree's error icons, the Errors tab, the
/// file watch and font generation. They share one collection while the project is unchanged; these pin
/// that what each pass reports is what it reported when each collected on its own, that an edit is never
/// answered with fonts collected before it, and that a load collects each element once.
/// </summary>
public class SharedFontCollectionTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void Load_ReportsTheSameErrorsWatchedFoldersAndGeneratedFonts()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumSharedFontsLoad");
        string outsideFolder = BuildProject(fixture);
        Settle();
        NoOpFontFileGenerator generator = (NoOpFontFileGenerator)Services.GetRequiredService<IFontFileGenerator>();
        int requestsBefore = generator.RequestedFntPaths().Count;

        ReloadAndSettle(fixture);

        IErrorChecker errorChecker = Services.GetRequiredService<IErrorChecker>();
        ErrorCodesFor(errorChecker, fixture, "Broken").ShouldContain("GUM0006");
        ErrorCodesFor(errorChecker, fixture, "Label").ShouldNotContain("GUM0006");
        ErrorCodesFor(errorChecker, fixture, "Wide").ShouldNotContain("GUM0006");
        Services.GetRequiredService<IFileWatchManager>().CurrentFilePathsWatching
            .ShouldContain(path => path == new FilePath(outsideFolder));
        // NoOpFontFileGenerator writes nothing, so a font can be asked for again; which fonts is what counts.
        generator.RequestedFntPaths().Skip(requestsBefore).Select(path => Path.GetFileName(path)!).Distinct().Order()
            .ShouldBe(new[]
            {
                "Font18LiberationSans-Regular_ttf_ba33345d.fnt", "Font20Missing_ttf_4b648472.fnt",
                "Font20Outside_ttf_bc6b78d3.fnt", "Font20Present_ttf_2ea6f75d.fnt",
            });
    }

    [AvaloniaFact]
    public void Load_CollectsEachElementsFontsOnce_AcrossItsPasses()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumSharedFontsOnce");
        BuildProject(fixture);
        Services.GetRequiredService<IFileCommands>().ForceSaveProject(forceSaveContainedElements: true);
        Settle();
        ProjectFontReferences references = Services.GetRequiredService<ProjectFontReferences>();
        int cachesBefore = references.CachesStarted;
        int collectionsBefore = references.CollectionCount;

        GumProjectSave loaded = Load(fixture.ProjectFilePath);

        (references.CachesStarted - cachesBefore).ShouldBe(1);
        (references.CollectionCount - collectionsBefore).ShouldBe(loaded.AllElements.Count());
    }

    [AvaloniaFact]
    public void Edits_AreNeverAnsweredWithFontsCollectedBeforeThem()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumSharedFontsEdits");
        BuildProject(fixture);
        Settle();
        ReloadAndSettle(fixture);
        IErrorChecker errorChecker = Services.GetRequiredService<IErrorChecker>();
        ComponentSave label = fixture.Project.Components.Single(component => component.Name == "Label");
        ComponentSave broken = fixture.Project.Components.Single(component => component.Name == "Broken");
        ErrorCodesFor(errorChecker, fixture, "Label").ShouldNotContain("GUM0006");

        // A variable set through the Variables tab's own logic.
        fixture.SelectedState.SelectedElement = label;
        SetFont(label, "Fonts/Missing.ttf");
        ErrorCodesFor(errorChecker, fixture, "Label").ShouldContain("GUM0006", "after setting a missing font");

        // Undo puts the old font back.
        fixture.UndoManager.PerformUndo();
        ErrorCodesFor(errorChecker, fixture, "Label").ShouldNotContain("GUM0006", "after undo");

        // A Text added to a component uses the default font, so the component's only missing font goes with
        // the Text that had it.
        InstanceSave brokenText = broken.Instances.Single();
        fixture.SelectedState.SelectedElement = broken;
        Services.GetRequiredService<IDeleteLogic>().RemoveInstance(brokenText, broken);
        ErrorCodesFor(errorChecker, fixture, "Broken").ShouldNotContain("GUM0006", "after deleting the Text");

        // The component's file changes on disk and the file watch reloads it.
        string brokenFile = Path.Combine(fixture.ProjectFolder, "Components", "Broken.gucx");
        File.WriteAllText(brokenFile, File.ReadAllText(Path.Combine(fixture.ProjectFolder, "Components", "Label.gucx"))
            .Replace("<Name>Label</Name>", "<Name>Broken</Name>")
            .Replace("Fonts/Present.ttf", "Fonts/Missing.ttf"));
        Services.GetRequiredService<FileChangeReactionLogic>().ReactToFileChanged(new FilePath(brokenFile));
        Dispatcher.UIThread.RunJobs();
        ErrorCodesFor(errorChecker, fixture, "Broken").ShouldContain("GUM0006", "after the file watch reloaded the component");
    }

    // Label and Wide use font files that exist, Wide's outside the project folder (a watch root of its own),
    // and Broken one that is missing. Returns the outside folder.
    private static string BuildProject(ToolProjectFixture fixture)
    {
        // A fixed name, so the font's cache name (which hashes the path as typed) is the same every run.
        string outsideFolder = Path.GetFullPath(Path.Combine(fixture.ProjectFolder, "..", "SharedFonts"));
        Directory.CreateDirectory(Path.Combine(fixture.ProjectFolder, "Fonts"));
        File.WriteAllText(Path.Combine(fixture.ProjectFolder, "Fonts", "Present.ttf"), string.Empty);
        Directory.CreateDirectory(outsideFolder);
        File.WriteAllText(Path.Combine(outsideFolder, "Outside.ttf"), string.Empty);
        string outsideRelative = "../SharedFonts/Outside.ttf";

        AddTextComponent(fixture, "Label", "Fonts/Present.ttf");
        AddTextComponent(fixture, "Broken", "Fonts/Missing.ttf");
        AddTextComponent(fixture, "Wide", outsideRelative);
        return outsideFolder;
    }

    private static void AddTextComponent(ToolProjectFixture fixture, string name, string font)
    {
        ComponentSave component = fixture.AddComponent(name);
        fixture.AddInstance(component, "Caption", "Text");
        StateSave state = component.DefaultState.ShouldNotBeNull();
        state.SetValue("Caption.Font", font, "string");
        state.SetValue("Caption.FontSize", 20, "int");
    }

    private static void SetFont(ComponentSave component, string font)
    {
        StateSave state = component.DefaultState.ShouldNotBeNull();
        InstanceSave caption = component.Instances.Single();
        object? oldValue = state.GetValue("Caption.Font");
        state.SetValue("Caption.Font", font, "string");
        Services.GetRequiredService<ISetVariableLogic>().PropertyValueChanged("Font", oldValue, caption, state);
        Dispatcher.UIThread.RunJobs();
    }

    private static string[] ErrorCodesFor(IErrorChecker errorChecker, ToolProjectFixture fixture, string componentName)
    {
        ComponentSave component = fixture.Project.Components.Single(item => item.Name == componentName);
        return errorChecker.GetErrorsFor(component, fixture.Project).Select(error => error.Code ?? string.Empty).ToArray();
    }

    // Reloads through the tool's own load path, then runs what the load posted for later: the Errors tab's
    // project check, the file watch's root scan and the font cache check.
    private static void ReloadAndSettle(ToolProjectFixture fixture)
    {
        fixture.SaveAndReload();
        Settle();
    }

    // Opens the saved project through the tool's load path and runs what the load posted, with nothing
    // else (a save, an earlier edit's refresh) mixed in.
    private static GumProjectSave Load(string projectFilePath)
    {
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        Task load = projectManager.LoadProjectAsync(new FilePath(projectFilePath));
        System.Diagnostics.Stopwatch waited = System.Diagnostics.Stopwatch.StartNew();
        while (!load.IsCompleted)
        {
            waited.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(30), "the load did not finish");
            Thread.Sleep(10);
            Dispatcher.UIThread.RunJobs();
        }
        load.GetAwaiter().GetResult();
        Settle();
        return projectManager.GumProjectSave.ShouldNotBeNull();
    }

    // Runs work posted to the UI thread, including what that work posts in turn.
    private static void Settle()
    {
        for (int i = 0; i < 20; i++)
        {
            Thread.Sleep(10);
            Dispatcher.UIThread.RunJobs();
        }
    }
}
