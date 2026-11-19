using EventOutputPlugin;
using EventOutputPlugin.Models;
using Gum.Commands;
using Gum.DataTypes;
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
    private readonly string _projectDirectory;

    public MainEventOutputPluginTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "MainEventOutputPluginTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_projectDirectory, "EventExport"));
    }

    public void Dispose()
    {
        Directory.Delete(_projectDirectory, recursive: true);
    }

    [Fact]
    public void ProjectLoad_PrunesOldEvents_OnlyOnceThePostedWorkRuns()
    {
        string eventsFile = Path.Combine(_projectDirectory, "EventExport", "gum_events.json");
        ExportedEventCollection stored = new ExportedEventCollection();
        stored.UserEvents["someUser"] = new List<ExportedEvent>
        {
            new ExportedEvent { NewName = "OldScreen", EventType = GumEventTypes.ElementAdded, TimestampUtc = DateTime.UtcNow.AddDays(-30) },
        };
        File.WriteAllText(eventsFile, JsonConvert.SerializeObject(stored));

        Mock<IProjectState> projectState = new Mock<IProjectState>();
        projectState.Setup(x => x.ProjectDirectory).Returns(_projectDirectory);
        Mock<IFileCommands> fileCommands = new Mock<IFileCommands>();
        fileCommands
            .Setup(x => x.SaveIfDiffers(It.IsAny<FilePath>(), It.IsAny<string>()))
            .Callback<FilePath, string>((filePath, contents) => File.WriteAllText(filePath.FullPath, contents));
        Mock<IRetryService> retryService = new Mock<IRetryService>();
        retryService
            .Setup(x => x.TryMultipleTimes(It.IsAny<Action>(), It.IsAny<int>()))
            .Callback<Action, int>((action, _) => action());
        List<Action> posted = new List<Action>();
        Mock<IDispatcher> dispatcher = new Mock<IDispatcher>();
        dispatcher.Setup(x => x.Post(It.IsAny<Action>())).Callback<Action>(posted.Add);

        MainEventOutputPlugin plugin = new MainEventOutputPlugin(projectState.Object, fileCommands.Object, retryService.Object, dispatcher.Object);
        plugin.StartUp();

        plugin.CallProjectLoad(new GumProjectSave());

        ExportedEventCollection.FromJson(File.ReadAllText(eventsFile)).UserEvents["someUser"].Count.ShouldBe(1);
        posted.Count.ShouldBe(1);

        posted[0]();

        ExportedEventCollection.FromJson(File.ReadAllText(eventsFile)).UserEvents["someUser"].ShouldBeEmpty();
    }
}
