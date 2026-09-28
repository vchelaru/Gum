using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.XUnit;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Avalonia.Services;
using Gum.Avalonia.Tests.Harness;
using Gum.CommandLine;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Extensions;
using Gum.Managers;
using Gum.Messages;
using Gum.ProjectServices.FontGeneration;
using Gum.Services.Fonts;
using Gum.Settings;
using Gum.Startup;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the head's command line (inventory area CLI), without opening the
/// tool's window: each launch line is split by the head's own parsers (<see cref="HeadOptions"/>
/// and the tool's <see cref="CommandLineManager"/>), and what they read is handed to the same
/// services startup hands it to, on a temp project.
/// </summary>
[Trait("Category", "EndToEnd")]
public class HeadCommandLineScenarioTests
{
    private static readonly TimeSpan AsyncWork = TimeSpan.FromSeconds(60);

    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    [Trait("Feature", "CLI-002")]
    [Trait("Feature", "CLI-003")]
    [Trait("Feature", "CLI-006")]
    public void UnattendedLaunchLine_TheHeadReadsItsOptions_AndTheToolReadsOnlyTheProject()
    {
        string folder = Path.Combine(Path.GetTempPath(), "GumHeadCommandLine", Guid.NewGuid().ToString("N"));
        string project = Path.Combine(folder, "Game.gumx");
        string[] args =
        {
            project, "--exit-after", "60", "--screenshot", Path.Combine(folder, "shot.png"), "--select", "Controls/Button#Label",
            "--theme", "light", "--zoom-to-fit", "--user-data", Path.Combine(folder, "UserData"),
        };

        HeadOptions options = HeadOptions.Parse(args);
        CommandLineManager commandLine = NewCommandLineManager();
        Pump(commandLine.ReadCommandLine(args));

        options.ExitAfterSeconds.ShouldBe(60);
        options.ScreenshotPath.ShouldBe(Path.Combine(folder, "shot.png"));
        options.SelectPath.ShouldBe("Controls/Button#Label");
        options.Theme.ShouldBe("light");
        options.ZoomToFit.ShouldBeTrue();
        options.UserDataFolder.ShouldBe(Path.Combine(folder, "UserData"));
        commandLine.GlueProjectToLoad.ShouldBe(project);
        commandLine.ElementName.ShouldBeNull();
        commandLine.ShouldExitImmediately.ShouldBeFalse();
        commandLine.UsageError.ShouldBeNull();
    }

    [AvaloniaFact]
    [Trait("Feature", "CLI-001")]
    public void PositionalElementFile_OpensItsProject_AndSelectsTheElement()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumHeadCommandLine");
        fixture.AddComponent("Card");
        fixture.SaveAndReload();
        string cardFile = Path.Combine(fixture.ProjectFolder, "Components", "Card.gucx");
        fixture.SelectedState.SelectedElement = null;
        CommandLineManager commandLine = NewCommandLineManager();

        Pump(commandLine.ReadCommandLine(new[] { cardFile }));
        // What ProjectManager.Initialize does with a project on the command line.
        Pump(Services.GetRequiredService<IFileCommands>().LoadProjectAsync(commandLine.GlueProjectToLoad!));
        fixture.SelectedState.SelectedElement = ObjectFinder.Self.GetElementSave(commandLine.ElementName!);

        new FilePath(commandLine.GlueProjectToLoad!).ShouldBe(new FilePath(fixture.ProjectFilePath));
        new FilePath(ProjectManager.GumProjectSave!.FullFileName!).ShouldBe(new FilePath(fixture.ProjectFilePath));
        fixture.SelectedState.SelectedElement.ShouldNotBeNull().Name.ShouldBe("Card");
    }

    [AvaloniaFact]
    [Trait("Feature", "CLI-004")]
    public void SelectOption_SelectsTheElementAndInstance_AndIgnoresNamesTheProjectLacks()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumHeadCommandLine");
        ComponentSave button = fixture.AddComponent("Controls/Button");
        InstanceSave label = fixture.AddInstance(button, "Label", "Text");
        string select = HeadOptions.Parse(new[] { "--select", "Controls/Button#Label" }).SelectPath!;
        fixture.SelectedState.SelectedElement = null;

        App.SelectStartupPath("Missing#Label", fixture.SelectedState);
        fixture.SelectedState.SelectedElement.ShouldBeNull();

        App.SelectStartupPath("Controls/Button#Missing", fixture.SelectedState);
        fixture.SelectedState.SelectedElement.ShouldBeSameAs(button);
        fixture.SelectedState.SelectedInstance.ShouldBeNull();

        App.SelectStartupPath(select, fixture.SelectedState);
        fixture.SelectedState.SelectedElement.ShouldBeSameAs(button);
        fixture.SelectedState.SelectedInstance.ShouldBeSameAs(label);
    }

    [AvaloniaFact]
    [Trait("Feature", "CLI-005")]
    public void ThemeOption_NamesTheVariantShownForTheRun()
    {
        App.ThemeVariantFor(HeadOptions.Parse(new[] { "--theme", "light" }).Theme!).ShouldBe(ThemeVariant.Light);
        App.ThemeVariantFor(HeadOptions.Parse(new[] { "--theme", "LIGHT" }).Theme!).ShouldBe(ThemeVariant.Light);
        App.ThemeVariantFor(HeadOptions.Parse(new[] { "--theme", "dark" }).Theme!).ShouldBe(ThemeVariant.Dark);
        HeadOptions.Parse(new[] { "Game.gumx" }).Theme.ShouldBeNull("without the option the saved theme applies");
    }

    [AvaloniaFact]
    [Trait("Feature", "CLI-007")]
    public void UserDataOption_SendsThePerUserFilesToThatFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "GumHeadCommandLine", Guid.NewGuid().ToString("N"), "RunData");
        string? original = FileManager.UserApplicationDataFolderOverride;
        try
        {
            Program.ApplyUserDataFolder(HeadOptions.Parse(new[] { "--user-data", folder }));

            GeneralSettingsFile settings = GeneralSettingsFile.LoadOrCreateNew();
            settings.LastProject = "Recent.gumx";
            settings.Save();

            File.Exists(Path.Combine(folder, "GeneralSettings.xml")).ShouldBeTrue();
            Path.GetFullPath(Program.GetAppDataDirectory()).ShouldBe(Path.GetFullPath(folder));
            GeneralSettingsFile.LoadOrCreateNew().LastProject.ShouldBe("Recent.gumx");
        }
        finally
        {
            FileManager.UserApplicationDataFolderOverride = original;
        }
    }

    [AvaloniaFact]
    [Trait("Feature", "CLI-008")]
    public void RebuildFontsOption_LoadsTheProject_RequestsItsMissingFonts_AndClosesTheTool()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumHeadCommandLine");
        fixture.SaveAndReload();
        NoOpFontFileGenerator fonts = (NoOpFontFileGenerator)Services.GetRequiredService<IFontFileGenerator>();
        int requestedBefore = fonts.RequestedFntPaths().Count;
        // Its own messenger: the tool's would close the test run's main window.
        StrongReferenceMessenger messenger = new StrongReferenceMessenger();
        int closeRequests = 0;
        messenger.Register<CloseMainWindowMessage>(this, (_, _) => closeRequests++);
        CommandLineManager commandLine = NewCommandLineManager(messenger);

        Pump(commandLine.ReadCommandLine(new[] { "--rebuildfonts", fixture.ProjectFilePath }));

        commandLine.ShouldExitImmediately.ShouldBeTrue();
        commandLine.UsageError.ShouldBeNull();
        new FilePath(ProjectManager.GumProjectSave!.FullFileName!).ShouldBe(new FilePath(fixture.ProjectFilePath));
        fonts.RequestedFntPaths().Skip(requestedBefore).ShouldContain(path => path.EndsWith(".fnt", StringComparison.OrdinalIgnoreCase),
            "the new project's default Text font has no .fnt yet");
        closeRequests.ShouldBe(1);

        CommandLineManager withoutProject = NewCommandLineManager(messenger);
        Pump(withoutProject.ReadCommandLine(new[] { "--rebuildfonts" }));
        withoutProject.UsageError.ShouldBe("--rebuildfonts requires a project file");
    }

    [AvaloniaFact]
    [Trait("Feature", "CLI-009")]
    public void GenerateCodeOption_GeneratesEveryScreenAndComponent_OfTheProjectItNames()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumHeadCommandLine");
        fixture.AddComponent("Card");
        fixture.AddScreen("TitleScreen");
        File.WriteAllText(Path.Combine(fixture.ProjectFolder, "ProjectCodeSettings.codsj"),
            """{ "CodeProjectRoot": "Code/", "OutputLibrary": 5, "ObjectInstantiationType": 1 }""");
        Services.GetRequiredService<IFileCommands>().ForceSaveProject(forceSaveContainedElements: true);
        CommandLineManager commandLine = NewCommandLineManager();

        Pump(commandLine.ReadCommandLine(new[] { "--generatecode", fixture.ProjectFilePath }));
        commandLine.ShouldCodeGenAll.ShouldBeTrue();
        commandLine.ShouldExitImmediately.ShouldBeTrue();
        // What ProjectManager.Initialize does for --generatecode.
        Pump(Services.GetRequiredService<IFileCommands>().LoadProjectAsync(commandLine.GlueProjectToLoad!));
        Pump(Services.GetRequiredService<IMessenger>().SendAsync(new RequestCodeGenerationMessage()));

        string code = Path.Combine(fixture.ProjectFolder, "Code");
        File.ReadAllText(Path.Combine(code, "Components", "Card.Generated.cs")).ShouldContain("partial class Card");
        File.ReadAllText(Path.Combine(code, "Screens", "TitleScreen.Generated.cs")).ShouldContain("partial class TitleScreen");
        File.Exists(Path.Combine(code, "StandardElements.Generated.cs")).ShouldBeTrue();

        CommandLineManager withoutProject = NewCommandLineManager();
        Pump(withoutProject.ReadCommandLine(new[] { "--generatecode" }));
        withoutProject.UsageError.ShouldBe("--generatecode requires a project file");
    }

    [AvaloniaFact]
    [Trait("Feature", "CLI-010")]
    public void FileActivation_BeforeStartupFinishes_OpensTheProjectOnceItDoes_AndAfterItOpensAtOnce()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumHeadCommandLine");
        fixture.AddComponent("Card");
        fixture.SaveAndReload();
        string otherFolder = Path.Combine(fixture.ProjectFolder, "Other");
        CopyProject(fixture.ProjectFolder, otherFolder);
        string otherProject = Path.Combine(otherFolder, Path.GetFileName(fixture.ProjectFilePath));
        ProjectOpenRequestRouter router = new ProjectOpenRequestRouter(fixture.SelectedState,
            new Lazy<IFileCommands>(() => Services.GetRequiredService<IFileCommands>()));
        FileActivationHandler handler = new FileActivationHandler(new Lazy<IProjectOpenRequestRouter>(() => router));

        handler.HandleActivated(null, Activation(otherProject));
        Dispatcher.UIThread.RunJobs();
        new FilePath(ProjectManager.GumProjectSave!.FullFileName!).ShouldBe(new FilePath(fixture.ProjectFilePath), "startup has not finished");

        Pump(router.CompleteStartupAsync());
        new FilePath(ProjectManager.GumProjectSave!.FullFileName!).ShouldBe(new FilePath(otherProject));

        handler.HandleActivated(null, Activation(fixture.ProjectFilePath));
        WaitUntil(() => new FilePath(ProjectManager.GumProjectSave!.FullFileName!) == new FilePath(fixture.ProjectFilePath), "the second activation to open its project");
        ProjectManager.GumProjectSave!.Components.Select(component => component.Name).ShouldBe(new[] { "Card" });
    }

    [Fact]
    [Trait("Feature", "CLI-011")]
    public void EchoOutputVariable_WritesEveryOutputLineToStandardError()
    {
        string? originalVariable = Environment.GetEnvironmentVariable(MainOutputViewModel.EchoEnvironmentVariable);
        TextWriter originalError = Console.Error;
        using StringWriter error = new StringWriter();
        try
        {
            Console.SetError(error);
            Environment.SetEnvironmentVariable(MainOutputViewModel.EchoEnvironmentVariable, null);
            new MainOutputViewModel().AddOutput("quiet line");
            Environment.SetEnvironmentVariable(MainOutputViewModel.EchoEnvironmentVariable, "1");
            MainOutputViewModel output = new MainOutputViewModel();

            output.AddOutput("loaded project");
            output.AddError("missing file");
        }
        finally
        {
            Console.SetError(originalError);
            Environment.SetEnvironmentVariable(MainOutputViewModel.EchoEnvironmentVariable, originalVariable);
        }

        string written = error.ToString();
        written.ShouldNotContain("quiet line");
        written.ShouldContain("loaded project");
        written.ShouldContain("ERROR:  missing file");
    }

    private static IProjectManager ProjectManager => Services.GetRequiredService<IProjectManager>();

    private static CommandLineManager NewCommandLineManager(IMessenger? messenger = null) =>
        new CommandLineManager(
            Services.GetRequiredService<IFontManager>(),
            Services.GetRequiredService<IGuiCommands>(),
            Services.GetRequiredService<IFileCommands>(),
            messenger ?? Services.GetRequiredService<IMessenger>(),
            ProjectManager);

    private static FileActivatedEventArgs Activation(string path)
    {
        Mock<IStorageFile> file = new Mock<IStorageFile>();
        file.SetupGet(candidate => candidate.Path).Returns(new Uri(path));
        return new FileActivatedEventArgs(new IStorageItem[] { file.Object });
    }

    private static void CopyProject(string from, string to)
    {
        List<string> files = Directory.GetFiles(from, "*", SearchOption.AllDirectories)
            .Where(file => !file.StartsWith(to, StringComparison.Ordinal))
            .ToList();
        foreach (string file in files)
        {
            string target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    // [AvaloniaFact] tests stay synchronous; the work posts to the UI thread it waits on.
    private static void Pump(Task task)
    {
        DateTime deadline = DateTime.UtcNow + AsyncWork;
        while (!task.IsCompleted)
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The command line's work did not finish.");
            }
            Thread.Sleep(10);
            Dispatcher.UIThread.RunJobs();
        }
        task.GetAwaiter().GetResult();
        Dispatcher.UIThread.RunJobs();
    }

    private static void WaitUntil(Func<bool> condition, string what)
    {
        DateTime deadline = DateTime.UtcNow + AsyncWork;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Timed out waiting for {what}.");
            }
            Thread.Sleep(10);
            Dispatcher.UIThread.RunJobs();
        }
    }
}
