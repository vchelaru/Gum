using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gum.Startup;
using Shouldly;

namespace Gum.Presentation.Tests.Startup;

public class ProjectSerializerWarmupLauncherTests
{
    private int _jsonWarmups;
    private readonly List<Exception> _logged = new List<Exception>();
    private Action _warmJson;

    public ProjectSerializerWarmupLauncherTests()
    {
        _warmJson = () => _jsonWarmups++;
    }

    // Runs the background work inline so the test sees its result.
    private ProjectSerializerWarmupLauncher Create() =>
        new ProjectSerializerWarmupLauncher(() => _warmJson(), _logged.Add, work => { work(); return Task.CompletedTask; });

    [Theory]
    [InlineData("/projects/Last.gumj", 1)]
    [InlineData("/projects/Last.gumx", 0)]
    [InlineData(null, 0)]
    public void Start_WithNoProjectOnTheCommandLine_WarmsJsonOnlyForAJsonLastProject(string? lastProject, int expectedWarmups)
    {
        Create().Start(new[] { "Gum.exe" }, lastProject);

        _jsonWarmups.ShouldBe(expectedWarmups);
    }

    [Theory]
    [InlineData("/projects/Other.gumx", "/projects/Last.gumj", 0)]
    [InlineData("/projects/Other.gumj", "/projects/Last.gumx", 1)]
    public void Start_WithAProjectOnTheCommandLine_GoesByThatProject(string commandLineProject, string lastProject, int expectedWarmups)
    {
        Create().Start(new[] { "Gum.exe", commandLineProject }, lastProject);

        _jsonWarmups.ShouldBe(expectedWarmups);
    }

    [Fact]
    public void Start_WarmUpThrows_LogsTheFailureInsteadOfThrowing()
    {
        InvalidOperationException failure = new InvalidOperationException("warm-up failed");
        _warmJson = () => throw failure;

        Task started = Create().Start(new[] { "Gum.exe" }, "/projects/Last.gumj");

        started.IsFaulted.ShouldBeFalse();
        _logged.ShouldBe(new[] { failure });
    }
}
