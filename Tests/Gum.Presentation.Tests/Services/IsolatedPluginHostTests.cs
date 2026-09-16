using System;
using Gum.Services;
using PluginHostFixture;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests.Services;

/// <summary>
/// Pins the isolation guarantee IsolatedPluginHost exists for (issue #4723): invoking a plugin
/// assembly's static method through the host must not share static state with a copy of that
/// same assembly already loaded into the default AssemblyLoadContext. This is what lets the
/// Avalonia head load gumcli's SVG export logic in-process without gumcli's ObjectFinder.Self /
/// RenderingLibrary.SystemManagers colliding with the head's own copies.
/// </summary>
public class IsolatedPluginHostTests
{
    [Fact]
    public void InvokeStaticMethod_isolates_static_state_from_default_load_context()
    {
        // Load the fixture into the default context first, and mutate its static state.
        Counter.IncrementAndGet().ShouldBe(1);
        Counter.IncrementAndGet().ShouldBe(2);

        string fixtureAssemblyPath = typeof(Counter).Assembly.Location;

        using IsolatedPluginHost host = new(fixtureAssemblyPath);

        // The isolated copy starts fresh - it does not see the default context's Value of 2.
        object? isolatedResult = host.InvokeStaticMethod(
            fixtureAssemblyPath, "PluginHostFixture.Counter", "IncrementAndGet", Array.Empty<object?>());
        isolatedResult.ShouldBe(1);

        // Mutating the isolated copy again must not leak back into the default context's copy.
        Counter.Value.ShouldBe(2);
    }

    [Fact]
    public void InvokeStaticMethod_throws_when_type_not_found()
    {
        string fixtureAssemblyPath = typeof(Counter).Assembly.Location;
        using IsolatedPluginHost host = new(fixtureAssemblyPath);

        Should.Throw<InvalidOperationException>(() =>
            host.InvokeStaticMethod(fixtureAssemblyPath, "PluginHostFixture.NoSuchType", "IncrementAndGet", Array.Empty<object?>()));
    }

    [Fact]
    public void InvokeStaticMethod_throws_when_method_not_found()
    {
        string fixtureAssemblyPath = typeof(Counter).Assembly.Location;
        using IsolatedPluginHost host = new(fixtureAssemblyPath);

        Should.Throw<InvalidOperationException>(() =>
            host.InvokeStaticMethod(fixtureAssemblyPath, "PluginHostFixture.Counter", "NoSuchMethod", Array.Empty<object?>()));
    }
}
