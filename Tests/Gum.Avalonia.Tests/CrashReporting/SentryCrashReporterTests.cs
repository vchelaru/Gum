using Gum.Avalonia.Diagnostics.CrashReporting;
using Gum.Diagnostics;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests.CrashReporting;

public class SentryCrashReporterTests
{
    private readonly Mock<ICrashReporter> _inner = new();
    private readonly Mock<ICrashReportSender> _sender = new();
    private readonly InvalidOperationException _exception = new InvalidOperationException("boom");
    private bool _isSendingAllowed;

    private SentryCrashReporter Create() => new SentryCrashReporter(_inner.Object, _sender.Object, () => _isSendingAllowed);

    [Fact]
    public void EveryCall_IsForwardedToTheInnerReporter()
    {
        _inner.SetupGet(x => x.DirectoryPath).Returns("logs");
        _inner.Setup(x => x.ReportRecoverable(_exception, "ui")).Returns("r.txt");
        _inner.Setup(x => x.LogRecoverable(_exception, "dbus")).Returns("l.txt");
        _inner.Setup(x => x.ReportFatal(_exception, "domain")).Returns("f.txt");
        SentryCrashReporter reporter = Create();

        reporter.DirectoryPath.ShouldBe("logs");
        reporter.ReportRecoverable(_exception, "ui").ShouldBe("r.txt");
        reporter.LogRecoverable(_exception, "dbus").ShouldBe("l.txt");
        reporter.ReportFatal(_exception, "domain").ShouldBe("f.txt");
        reporter.PromptForPreviousCrash();

        _inner.Verify(x => x.PromptForPreviousCrash(), Times.Once);
    }

    [Fact]
    public void Reports_AreSentOnlyWhenTheUserOptedIn()
    {
        SentryCrashReporter reporter = Create();

        reporter.ReportRecoverable(_exception, "ui");
        reporter.ReportFatal(_exception, "domain");
        _sender.VerifyNoOtherCalls();

        _isSendingAllowed = true;
        reporter.ReportRecoverable(_exception, "ui");
        reporter.ReportFatal(_exception, "domain");

        _sender.Verify(x => x.Send(_exception, "ui", false), Times.Once);
        _sender.Verify(x => x.Send(_exception, "domain", true), Times.Once);
    }

    [Fact]
    public void LogRecoverable_IsNeverSent()
    {
        _isSendingAllowed = true;

        Create().LogRecoverable(_exception, "dbus");

        _sender.VerifyNoOtherCalls();
    }

    [Fact]
    public void FailuresInTheInnerReporterOrTheSender_AreSwallowed()
    {
        _isSendingAllowed = true;
        _inner.Setup(x => x.ReportRecoverable(It.IsAny<Exception>(), It.IsAny<string>())).Throws<IOException>();
        _inner.Setup(x => x.ReportFatal(It.IsAny<Exception>(), It.IsAny<string>())).Throws<IOException>();
        _inner.Setup(x => x.LogRecoverable(It.IsAny<Exception>(), It.IsAny<string>())).Throws<IOException>();
        _inner.Setup(x => x.PromptForPreviousCrash()).Throws<IOException>();
        _sender.Setup(x => x.Send(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<bool>())).Throws<HttpRequestException>();
        SentryCrashReporter reporter = Create();

        Should.NotThrow(() =>
        {
            reporter.ReportRecoverable(_exception, "ui").ShouldBeNull();
            reporter.ReportFatal(_exception, "domain").ShouldBeNull();
            reporter.LogRecoverable(_exception, "dbus").ShouldBeNull();
            reporter.PromptForPreviousCrash();
        });
        // The fatal report is still attempted after the local log failed.
        _sender.Verify(x => x.Send(_exception, "domain", true), Times.Once);
    }

    [Fact]
    public void Wrap_EmptyDsn_ReturnsTheInnerReporterWithoutStartingSentry()
    {
        bool started = false;

        ICrashReporter result = SentryCrashReporter.Wrap(_inner.Object, "", () =>
        {
            started = true;
            return _sender.Object;
        }, () => true);

        result.ShouldBeSameAs(_inner.Object);
        started.ShouldBeFalse();
    }

    [Fact]
    public void Wrap_StartFails_ReturnsTheInnerReporter()
    {
        ICrashReporter result = SentryCrashReporter.Wrap(_inner.Object, "https://key@example.invalid/1",
            () => throw new ArgumentException("bad dsn"), () => true);

        result.ShouldBeSameAs(_inner.Object);
    }

    [Fact]
    public void Wrap_Dsn_WrapsTheInnerReporter()
    {
        ICrashReporter result = SentryCrashReporter.Wrap(_inner.Object, "https://key@example.invalid/1", () => _sender.Object, () => true);

        result.ShouldBeOfType<SentryCrashReporter>();
    }
}
