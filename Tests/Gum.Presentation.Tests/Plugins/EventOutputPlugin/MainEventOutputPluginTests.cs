using EventOutputPlugin;
using EventOutputPlugin.Models;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Plugins;
using Gum.Services;
using Gum.ToolStates;
using Moq;
using Newtonsoft.Json;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;

namespace Gum.Presentation.Tests.Plugins.EventOutput;

public class MainEventOutputPluginTests : IDisposable
{
    private readonly string _rootDirectory;
    private readonly Mock<IProjectState> _projectState = new Mock<IProjectState>();
    private readonly Mock<IFileCommands> _fileCommands = new Mock<IFileCommands>();
    private readonly List<Action> _posted = new List<Action>();
    private readonly PluginManager _pluginManager;
    private readonly MainEventOutputPlugin _plugin;

    public MainEventOutputPluginTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "MainEventOutputPluginTests", Guid.NewGuid().ToString("N"));

        _fileCommands
            .Setup(x => x.SaveIfDiffers(It.IsAny<FilePath>(), It.IsAny<string>()))
            .Callback<FilePath, string>((filePath, contents) => File.WriteAllText(filePath.FullPath, contents));
        Mock<IRetryService> retryService = new Mock<IRetryService>();
        retryService
            .Setup(x => x.TryMultipleTimes(It.IsAny<Action>(), It.IsAny<int>()))
            .Callback<Action, int>((action, _) => action());
        Mock<IDispatcher> dispatcher = new Mock<IDispatcher>();
        dispatcher.Setup(x => x.Post(It.IsAny<Action>())).Callback<Action>(_posted.Add);

        _pluginManager = new PluginManager(new Mock<IPluginEnablementStore>().Object, new Mock<IPluginHostConfiguration>().Object);
        _plugin = new MainEventOutputPlugin(_projectState.Object, _fileCommands.Object, retryService.Object, dispatcher.Object, _pluginManager);
        _pluginManager.Plugins = new List<Gum.Plugins.BaseClasses.PluginBase> { _plugin };
        PluginManager.StartupPlugin(_plugin, _pluginManager);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    private string CreateProjectWithEventsFile(string name, string contents)
    {
        string projectDirectory = Path.Combine(_rootDirectory, name);
        Directory.CreateDirectory(Path.Combine(projectDirectory, "EventExport"));
        File.WriteAllText(EventsFile(projectDirectory), contents);
        return projectDirectory;
    }

    private static string EventsFile(string projectDirectory) => Path.Combine(projectDirectory, "EventExport", "gum_events.json");

    private static string OneEventDaysOld(int days)
    {
        ExportedEventCollection stored = new ExportedEventCollection();
        stored.UserEvents["someUser"] = new List<ExportedEvent>
        {
            new ExportedEvent { NewName = "OldScreen", EventType = GumEventTypes.ElementAdded, TimestampUtc = DateTime.UtcNow.AddDays(-days) },
        };
        return JsonConvert.SerializeObject(stored);
    }

    private void Load(string projectDirectory)
    {
        _projectState.Setup(x => x.ProjectDirectory).Returns(projectDirectory);
        _pluginManager.ProjectLoad(new GumProjectSave());
    }

    private void RunPosted()
    {
        foreach (Action action in _posted)
        {
            action();
        }
        _posted.Clear();
    }

    [Fact]
    public void ProjectLoad_PrunesOldEvents_OnlyOnceThePostedWorkRuns()
    {
        string project = CreateProjectWithEventsFile("Project", OneEventDaysOld(30));

        Load(project);

        ExportedEventCollection.FromJson(File.ReadAllText(EventsFile(project))).UserEvents["someUser"].Count.ShouldBe(1);
        _posted.Count.ShouldBe(1);

        RunPosted();

        ExportedEventCollection.FromJson(File.ReadAllText(EventsFile(project))).UserEvents["someUser"].ShouldBeEmpty();
    }

    [Fact]
    public void ProjectLoad_SecondLoadBeforeThePostedWorkRuns_PrunesOnlyTheProjectThatIsOpen()
    {
        string first = CreateProjectWithEventsFile("First", OneEventDaysOld(30));
        string second = CreateProjectWithEventsFile("Second", OneEventDaysOld(30));

        Load(first);
        Load(second);
        RunPosted();

        ExportedEventCollection.FromJson(File.ReadAllText(EventsFile(first))).UserEvents["someUser"].Count.ShouldBe(1);
        ExportedEventCollection.FromJson(File.ReadAllText(EventsFile(second))).UserEvents["someUser"].ShouldBeEmpty();
        _fileCommands.Verify(x => x.SaveIfDiffers(It.IsAny<FilePath>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void ProjectLoad_CorruptEventsFile_DisablesThePluginInsteadOfThrowing()
    {
        string project = CreateProjectWithEventsFile("Corrupt", "{ this is not json");

        Load(project);

        Should.NotThrow(RunPosted);
        _pluginManager.PluginContainers[_plugin].IsEnabled.ShouldBeFalse();
    }
}
