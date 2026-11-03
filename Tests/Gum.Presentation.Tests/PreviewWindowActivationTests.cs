using System;
using System.Collections.Generic;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

public class PreviewWindowActivationTests
{
    private sealed class FakeActivator : IPreviewWindowActivator
    {
        public IntPtr Found { get; set; }
        public bool Throws { get; set; }
        public List<IntPtr> Activated { get; } = new List<IntPtr>();

        public IntPtr FindWindow(int processId) => Found;

        public string Activate(IntPtr window)
        {
            if (Throws)
            {
                throw new InvalidOperationException("boom");
            }
            Activated.Add(window);
            return "ok";
        }
    }

    [Fact]
    public void Activate_WithMainWindow_UsesItWithoutSearching()
    {
        FakeActivator activator = new FakeActivator { Found = new IntPtr(2) };
        List<string> log = new List<string>();

        bool result = PreviewWindowActivation.Activate(activator, 10, new IntPtr(1), log.Add);

        result.ShouldBeTrue();
        activator.Activated.ShouldBe(new[] { new IntPtr(1) });
        log.Count.ShouldBe(1);
    }

    [Fact]
    public void Activate_WithNoMainWindow_UsesTheFallbackWindow()
    {
        FakeActivator activator = new FakeActivator { Found = new IntPtr(2) };
        List<string> log = new List<string>();

        bool result = PreviewWindowActivation.Activate(activator, 10, IntPtr.Zero, log.Add);

        result.ShouldBeTrue();
        activator.Activated.ShouldBe(new[] { new IntPtr(2) });
        log[0].ShouldContain("fallback");
    }

    [Fact]
    public void Activate_WithNoWindowAtAll_LogsAndDoesNothing()
    {
        FakeActivator activator = new FakeActivator();
        List<string> log = new List<string>();

        bool result = PreviewWindowActivation.Activate(activator, 10, IntPtr.Zero, log.Add);

        result.ShouldBeFalse();
        activator.Activated.ShouldBeEmpty();
        log.Count.ShouldBe(1);
        log[0].ShouldContain("no window");
    }

    [Fact]
    public void Activate_WhenTheOsCallThrows_DoesNotThrowAndLogs()
    {
        FakeActivator activator = new FakeActivator { Throws = true };
        List<string> log = new List<string>();

        bool result = PreviewWindowActivation.Activate(activator, 10, new IntPtr(1), log.Add);

        result.ShouldBeFalse();
        log.Count.ShouldBe(1);
        log[0].ShouldContain("boom");
    }
}
