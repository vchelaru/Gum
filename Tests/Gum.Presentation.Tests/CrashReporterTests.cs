using Gum.Diagnostics;
using Shouldly;

namespace Gum.Presentation.Tests;

public class CrashReporterTests : IDisposable
{
    private readonly string _directory;
    private readonly List<string> _notifications;

    public CrashReporterTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "CrashReporterTests_" + Guid.NewGuid().ToString("N"));
        _notifications = new List<string>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void ReportRecoverable_WritesLogWithVersionAndStack_AndTellsTheUserWhereItIs()
    {
        CrashReporter reporter = new CrashReporter(_directory, "2026.9.1", _notifications.Add);
        Exception exception = CreateThrownException("something broke");

        string? logPath = reporter.ReportRecoverable(exception, "UI thread");

        logPath.ShouldNotBeNull();
        Path.GetDirectoryName(logPath).ShouldBe(_directory);
        string log = File.ReadAllText(logPath);
        log.ShouldContain("2026.9.1");
        log.ShouldContain("UI thread");
        log.ShouldContain("something broke");
        log.ShouldContain(nameof(CreateThrownException));
        _notifications.Count.ShouldBe(1);
        _notifications[0].ShouldContain(logPath);
    }

    [Fact]
    public void ReportRecoverable_SecondError_IsLoggedWithoutAnotherMessage()
    {
        CrashReporter reporter = new CrashReporter(_directory, "1.0", _notifications.Add);

        string? first = reporter.ReportRecoverable(new InvalidOperationException("first"), "UI thread");
        string? second = reporter.ReportRecoverable(new InvalidOperationException("second"), "UI thread");

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        second.ShouldNotBe(first);
        File.ReadAllText(second).ShouldContain("second");
        _notifications.Count.ShouldBe(1);
    }

    [Fact]
    public void ReportRecoverable_PastTheSessionCap_StopsWritingFiles()
    {
        CrashReporter reporter = new CrashReporter(_directory, "1.0", _notifications.Add);

        for (int i = 0; i < CrashReporter.MaxLogFilesPerSession; i++)
        {
            reporter.ReportRecoverable(new InvalidOperationException("error " + i), "UI thread").ShouldNotBeNull();
        }
        string? pastCap = reporter.ReportRecoverable(new InvalidOperationException("one too many"), "UI thread");

        pastCap.ShouldBeNull();
        Directory.GetFiles(_directory).Length.ShouldBe(CrashReporter.MaxLogFilesPerSession);
        reporter.ReportFatal(new InvalidOperationException("the crash"), "Unhandled exception").ShouldNotBeNull();
    }

    [Fact]
    public void ReportRecoverable_UnwritableFolder_DoesNotThrow_AndStillTellsTheUser()
    {
        string blockingFile = Path.Combine(Path.GetTempPath(), "CrashReporterTests_file_" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(blockingFile, "");
        try
        {
            CrashReporter reporter = new CrashReporter(blockingFile, "1.0", _notifications.Add);

            string? logPath = reporter.ReportRecoverable(new InvalidOperationException("no disk"), "UI thread");

            logPath.ShouldBeNull();
            _notifications.Count.ShouldBe(1);
            _notifications[0].ShouldContain("no disk");
        }
        finally
        {
            File.Delete(blockingFile);
        }
    }

    [Fact]
    public void PromptForPreviousCrash_AfterAFatalCrash_TellsTheUserOnce()
    {
        CrashReporter crashedSession = new CrashReporter(_directory, "1.0", _notifications.Add);
        string? fatalLog = crashedSession.ReportFatal(new InvalidOperationException("fatal"), "Background thread");
        fatalLog.ShouldNotBeNull();
        _notifications.ShouldBeEmpty();

        CrashReporter nextSession = new CrashReporter(_directory, "1.0", _notifications.Add);
        nextSession.PromptForPreviousCrash();
        nextSession.PromptForPreviousCrash();

        _notifications.Count.ShouldBe(1);
        _notifications[0].ShouldContain(Path.GetFileName(fatalLog));
        new CrashReporter(_directory, "1.0", _notifications.Add).PromptForPreviousCrash();
        _notifications.Count.ShouldBe(1);
    }

    [Fact]
    public void PromptForPreviousCrash_OnlyRecoverableErrorsLogged_SaysNothing()
    {
        CrashReporter previousSession = new CrashReporter(_directory, "1.0", _ => { });
        previousSession.ReportRecoverable(new InvalidOperationException("recovered"), "UI thread");

        new CrashReporter(_directory, "1.0", _notifications.Add).PromptForPreviousCrash();

        _notifications.ShouldBeEmpty();
    }

    private static Exception CreateThrownException(string message)
    {
        try
        {
            throw new InvalidOperationException(message);
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }
    }
}
