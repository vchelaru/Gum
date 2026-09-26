using EventOutputPlugin;
using EventOutputPlugin.Managers;
using EventOutputPlugin.Models;
using Gum.Commands;
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

public class ExportEventFileManagerTests : IDisposable
{
    private readonly string _rootDirectory;
    private readonly Mock<IProjectState> _projectState;
    private readonly ExportEventFileManager _sut;

    public ExportEventFileManagerTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "ExportEventFileManagerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootDirectory);

        _projectState = new Mock<IProjectState>();

        Mock<IFileCommands> fileCommands = new Mock<IFileCommands>();
        fileCommands
            .Setup(x => x.SaveIfDiffers(It.IsAny<FilePath>(), It.IsAny<string>()))
            .Callback<FilePath, string>((filePath, contents) => File.WriteAllText(filePath.FullPath, contents));

        Mock<IRetryService> retryService = new Mock<IRetryService>();
        retryService
            .Setup(x => x.TryMultipleTimes(It.IsAny<Action>(), It.IsAny<int>()))
            .Callback<Action, int>((action, _) => action());

        _sut = new ExportEventFileManager(_projectState.Object, fileCommands.Object, retryService.Object);
    }

    public void Dispose()
    {
        Directory.Delete(_rootDirectory, recursive: true);
    }

    [Fact]
    public void DeleteOldEventFiles_AfterSwitchingProjects_SavesOnlyTheNewProjectsEvents()
    {
        string projectA = Path.Combine(_rootDirectory, "ProjectA");
        string projectB = Path.Combine(_rootDirectory, "ProjectB");
        string eventsFileA = Path.Combine(projectA, "EventExport", "gum_events.json");
        string eventsFileB = Path.Combine(projectB, "EventExport", "gum_events.json");
        Directory.CreateDirectory(Path.GetDirectoryName(eventsFileA)!);
        Directory.CreateDirectory(projectB);

        ExportedEventCollection eventsInA = new ExportedEventCollection();
        eventsInA.UserEvents["userFromA"] = new List<ExportedEvent>
        {
            new ExportedEvent { NewName = "ScreenA", EventType = GumEventTypes.ElementAdded, TimestampUtc = DateTime.UtcNow }
        };
        File.WriteAllText(eventsFileA, JsonConvert.SerializeObject(eventsInA));

        _projectState.Setup(x => x.ProjectDirectory).Returns(projectA);
        _sut.DeleteOldEventFiles();

        _projectState.Setup(x => x.ProjectDirectory).Returns(projectB);
        _sut.DeleteOldEventFiles();

        ExportedEventCollection savedB = ExportedEventCollection.FromJson(File.ReadAllText(eventsFileB));
        savedB.UserEvents.ShouldBeEmpty();
        ExportedEventCollection savedA = ExportedEventCollection.FromJson(File.ReadAllText(eventsFileA));
        savedA.UserEvents.Keys.ShouldBe(new[] { "userFromA" });
    }
}
