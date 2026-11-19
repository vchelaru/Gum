using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gum.Startup;
using Shouldly;

namespace Gum.Presentation.Tests.Startup;

public class ProjectSerializerWarmupLauncherTests
{
    private int _jsonWarmups;
    private int _xmlWarmups;
    private readonly List<Exception> _logged = new List<Exception>();
    private Action _warmJson;
    private Action _warmXml;

    public ProjectSerializerWarmupLauncherTests()
    {
        _warmJson = () => _jsonWarmups++;
        _warmXml = () => _xmlWarmups++;
    }

    // Runs the background work inline so the test sees its result.
    private ProjectSerializerWarmupLauncher Create() =>
        new ProjectSerializerWarmupLauncher(() => _warmJson(), () => _warmXml(), _logged.Add, work => { work(); return Task.CompletedTask; });

    [Theory]
    [InlineData("/projects/Last.gumj", 1, 0)]
    [InlineData("/projects/Last.gumx", 0, 1)]
    [InlineData("/projects/Notes.txt", 0, 0)]
    [InlineData(null, 0, 0)]
    public void Start_WithNoProjectOnTheCommandLine_WarmsTheLastProjectsFormat(string? lastProject, int expectedJson, int expectedXml)
    {
        Create().Start(new[] { "Gum.exe" }, lastProject);

        _jsonWarmups.ShouldBe(expectedJson);
        _xmlWarmups.ShouldBe(expectedXml);
    }

    [Theory]
    [InlineData("/projects/Other.gumx", "/projects/Last.gumj", 0, 1)]
    [InlineData("/projects/Other.gumj", "/projects/Last.gumx", 1, 0)]
    public void Start_WithAProjectOnTheCommandLine_GoesByThatProject(string commandLineProject, string lastProject, int expectedJson, int expectedXml)
    {
        Create().Start(new[] { "Gum.exe", commandLineProject }, lastProject);

        _jsonWarmups.ShouldBe(expectedJson);
        _xmlWarmups.ShouldBe(expectedXml);
    }

    [Theory]
    [InlineData("/projects/Last.gumj")]
    [InlineData("/projects/Last.gumx")]
    public void Start_WarmUpThrows_LogsTheFailureInsteadOfThrowing(string lastProject)
    {
        InvalidOperationException failure = new InvalidOperationException("warm-up failed");
        _warmJson = () => throw failure;
        _warmXml = () => throw failure;

        Task started = Create().Start(new[] { "Gum.exe" }, lastProject);

        started.IsFaulted.ShouldBeFalse();
        _logged.ShouldBe(new[] { failure });
    }
}
