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
        private int _reads;

        public IntPtr Found { get; set; }
        public bool Throws { get; set; }
        public bool Accepts { get; set; } = true;
        public List<IntPtr> Foregrounds { get; set; } = new List<IntPtr>();
        public List<IntPtr> Activated { get; } = new List<IntPtr>();

        public IntPtr FindWindow(int processId) => Found;

        public bool Activate(IntPtr window, out string detail)
        {
            if (Throws)
            {
                throw new InvalidOperationException("boom");
            }
            Activated.Add(window);
            detail = "ok";
            return Accepts;
        }

        public IntPtr GetForegroundWindow()
        {
            IntPtr value = Foregrounds.Count == 0 ? IntPtr.Zero : Foregrounds[System.Math.Min(_reads, Foregrounds.Count - 1)];
            _reads++;
            return value;
        }
    }

    private static string Run(FakeActivator activator, IntPtr main)
    {
        List<string> log = new List<string>();
        PreviewWindowActivation.Activate(activator, 10, main, log.Add, _ => { });
        log.Count.ShouldBe(1);
        return log[0];
    }

    [Fact]
    public void Activate_WithMainWindow_UsesItWithoutSearching()
    {
        FakeActivator activator = new FakeActivator { Found = new IntPtr(2), Foregrounds = { new IntPtr(1) } };

        Run(activator, new IntPtr(1));

        activator.Activated.ShouldBe(new[] { new IntPtr(1) });
    }

    [Fact]
    public void Activate_WithNoMainWindow_UsesTheFallbackWindow()
    {
        FakeActivator activator = new FakeActivator { Found = new IntPtr(2), Foregrounds = { new IntPtr(2) } };

        string line = Run(activator, IntPtr.Zero);

        activator.Activated.ShouldBe(new[] { new IntPtr(2) });
        line.ShouldContain("fallback");
    }

    [Fact]
    public void Activate_WithNoWindowAtAll_LogsAndDoesNothing()
    {
        FakeActivator activator = new FakeActivator();

        bool result = PreviewWindowActivation.Activate(activator, 10, IntPtr.Zero, _ => { }, _ => { });
        string line = Run(activator, IntPtr.Zero);

        result.ShouldBeFalse();
        activator.Activated.ShouldBeEmpty();
        line.ShouldContain("no window");
    }

    [Fact]
    public void Activate_WhenTheOsCallThrows_DoesNotThrowAndLogs()
    {
        FakeActivator activator = new FakeActivator { Throws = true };

        string line = Run(activator, new IntPtr(1));

        line.ShouldContain("boom");
    }

    [Fact]
    public void Activate_WhenForegroundIsZeroThenThePreview_IsNotRefused()
    {
        FakeActivator activator = new FakeActivator { Foregrounds = { IntPtr.Zero, new IntPtr(1) } };

        string line = Run(activator, new IntPtr(1));

        line.ShouldContain("foreground is the preview");
        line.ShouldNotContain("NOT");
        line.ShouldNotContain("refused");
    }

    [Fact]
    public void Activate_WhenAnotherWindowStaysForeground_IsReportedAsNotThePreview()
    {
        FakeActivator activator = new FakeActivator { Foregrounds = { new IntPtr(9) } };

        string line = Run(activator, new IntPtr(1));

        line.ShouldContain("NOT the preview");
    }

    [Fact]
    public void Activate_WhenForegroundStaysZero_IsUnknownNotRefused()
    {
        FakeActivator activator = new FakeActivator { Foregrounds = { IntPtr.Zero } };

        string line = Run(activator, new IntPtr(1));

        line.ShouldContain("unknown");
        line.ShouldNotContain("refused");
    }

    [Fact]
    public void Activate_WhenSetForegroundWindowReturnsFalse_IsRefused()
    {
        FakeActivator activator = new FakeActivator { Accepts = false, Foregrounds = { new IntPtr(1) } };

        string line = Run(activator, new IntPtr(1));

        line.ShouldContain("refused");
    }
}
