using System;
using System.IO;
using GumPreview;
using Shouldly;

namespace Gum.Presentation.Tests.GumPreview;

public class PreviewFailureLogTests
{
    [Fact]
    public void TryRun_ThrowingAction_DoesNotEscapeAndWritesOneLine()
    {
        PreviewFailureLog log = new PreviewFailureLog();
        StringWriter errors = new StringWriter();

        bool succeeded = log.TryRun(() => throw new InvalidOperationException("bad element\nstack"), "could not show 'Main'", errors);

        succeeded.ShouldBeFalse();
        errors.ToString().ShouldBe("GumPreview: could not show 'Main': bad element" + Environment.NewLine);
    }

    [Fact]
    public void TryRun_SameFailureAgain_IsReportedOnce()
    {
        PreviewFailureLog log = new PreviewFailureLog();
        StringWriter errors = new StringWriter();

        log.TryRun(() => throw new Exception("x"), "ctx", errors);
        log.TryRun(() => throw new Exception("x"), "ctx", errors);

        errors.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length.ShouldBe(1);
    }

    [Fact]
    public void TryRun_DifferentFailure_IsReportedAgain()
    {
        PreviewFailureLog log = new PreviewFailureLog();
        StringWriter errors = new StringWriter();

        log.TryRun(() => throw new Exception("x"), "ctx", errors);
        log.TryRun(() => throw new Exception("y"), "ctx", errors);

        errors.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length.ShouldBe(2);
    }

    [Fact]
    public void TryRun_FailureAfterASuccess_IsReportedAgain()
    {
        PreviewFailureLog log = new PreviewFailureLog();
        StringWriter errors = new StringWriter();

        log.TryRun(() => throw new Exception("x"), "ctx", errors);
        log.TryRun(() => { }, "ctx", errors).ShouldBeTrue();
        log.TryRun(() => throw new Exception("x"), "ctx", errors);

        errors.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length.ShouldBe(2);
    }

    [Fact]
    public void LoadMessage_NamesThePathAndTheFirstLineOfTheReason()
    {
        string message = PreviewFailureLog.LoadMessage("C:/nope/Nope.gumx", new Exception("Could not find main project file\nat X"));

        message.ShouldBe("could not load C:/nope/Nope.gumx: Could not find main project file");
    }
}
