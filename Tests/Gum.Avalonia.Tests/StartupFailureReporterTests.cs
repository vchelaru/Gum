using Gum.Avalonia.Diagnostics;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// A startup failure goes to stderr and the main window, except after an unattended run has given
/// up and started closing the window (#5184).
/// </summary>
public class StartupFailureReporterTests
{
    [Fact]
    public void Report_BeforeAnUnattendedExit_ShowsTheFailureInTheWindowAndOnStderr()
    {
        List<Exception> shown = new List<Exception>();
        StringWriter error = new StringWriter();
        StartupFailureReporter reporter = new StartupFailureReporter(shown.Add, error);
        InvalidOperationException failure = new InvalidOperationException("project load failed");

        reporter.Report(failure);

        shown.ShouldBe(new Exception[] { failure });
        error.ToString().ShouldContain("Startup failed: ");
        error.ToString().ShouldContain("project load failed");
    }

    [Fact]
    public void Report_AfterTheUnattendedExitStarted_OnlyWritesToStderr()
    {
        List<Exception> shown = new List<Exception>();
        StringWriter error = new StringWriter();
        StartupFailureReporter reporter = new StartupFailureReporter(shown.Add, error);

        reporter.OnUnattendedExitStarted();
        reporter.Report(new InvalidOperationException("project load failed"));

        shown.ShouldBeEmpty("the window is closing, so no dialog should open on it");
        error.ToString().ShouldContain("project load failed");
    }
}
