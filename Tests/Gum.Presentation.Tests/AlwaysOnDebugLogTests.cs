using System;
using System.IO;
using Gum.Diagnostics;
using GumPreview;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>#7533: Debug builds always log to a fixed path; Release is off; explicit paths override; a run rotates the previous log.</summary>
public class AlwaysOnDebugLogTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "AlwaysOnDebugLogTests_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void ResolvePath_DebugBuildNoExplicitPath_UsesDefaultPath()
    {
        DiagnosticLogFile.ResolvePath(null, "default.log", isDebugBuild: true).ShouldBe("default.log");
    }

    [Fact]
    public void ResolvePath_ReleaseBuildNoExplicitPath_IsOff()
    {
        DiagnosticLogFile.ResolvePath(null, "default.log", isDebugBuild: false).ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ResolvePath_ExplicitPath_OverridesInEitherBuild(bool isDebugBuild)
    {
        DiagnosticLogFile.ResolvePath("mine.log", "default.log", isDebugBuild).ShouldBe("mine.log");
    }

    [Fact]
    public void PreviewResolvePath_DebugBuild_IsPreviewDefaultPath()
    {
        PreviewDebugLog.ResolvePath(null, isDebugBuild: true).ShouldBe(PreviewDebugLog.DefaultPath);
        PreviewDebugLog.ResolvePath(null, isDebugBuild: false).ShouldBeNull();
    }

    [Fact]
    public void ToolLogDefaultPath_IsFixedFileInTempFolder()
    {
        ToolLog.DefaultPath.ShouldBe(Path.Combine(Path.GetTempPath(), "Gum", "tool.log"));
    }

    [Fact]
    public void Open_ExistingLog_MovesItToPreviousAndStartsFresh()
    {
        string path = Path.Combine(_folder, "preview.log");
        Directory.CreateDirectory(_folder);
        File.WriteAllText(path, "old run\n");

        using (PreviewDebugLog log = PreviewDebugLog.Open(path)!)
        {
            log.Write("test", "new run");
        }

        File.ReadAllText(Path.Combine(_folder, "preview.previous.log")).ShouldBe("old run\n");
        string text = File.ReadAllText(path);
        text.ShouldContain("new run");
        text.ShouldNotContain("old run");
    }

    [Fact]
    public void ToolLogOpen_DebugBuildNoVariable_WritesToDefaultPath()
    {
        string path = Path.Combine(_folder, "tool.log");

        using (System.IO.StreamWriter? writer = ToolLog.Open(null, isDebugBuild: true, path))
        {
            writer.ShouldNotBeNull();
            writer!.WriteLine("hello");
        }

        File.ReadAllText(path).ShouldContain("hello");
    }

    [Fact]
    public void ToolLogOpen_ReleaseBuildNoVariable_IsOffAndCreatesNothing()
    {
        ToolLog.Open(null, isDebugBuild: false, Path.Combine(_folder, "tool.log")).ShouldBeNull();

        Directory.Exists(_folder).ShouldBeFalse();
    }

    [Fact]
    public void ToolLogOpen_ReleaseBuildWithVariable_WritesToThatPath()
    {
        string path = Path.Combine(_folder, "custom.log");

        using System.IO.StreamWriter? writer = ToolLog.Open(path, isDebugBuild: false, Path.Combine(_folder, "tool.log"));

        writer.ShouldNotBeNull();
        File.Exists(path).ShouldBeTrue();
        File.Exists(Path.Combine(_folder, "tool.log")).ShouldBeFalse();
    }

    [Fact]
    public void ToolLogOpen_UnwritablePath_ReturnsNullWithoutThrowing()
    {
        Directory.CreateDirectory(_folder);

        ToolLog.Open(_folder, isDebugBuild: true, _folder).ShouldBeNull();
    }

    [Fact]
    public void LogUnhandled_WritesErrorLineWithStack()
    {
        string path = Path.Combine(_folder, "preview.log");
        Exception thrown;
        try
        {
            throw new InvalidOperationException("kaboom");
        }
        catch (Exception exception)
        {
            thrown = exception;
        }

        using (PreviewDebugLog log = PreviewDebugLog.Open(path)!)
        {
            log.LogUnhandled(thrown);
        }

        string text = File.ReadAllText(path);
        text.ShouldContain(" error unhandled: System.InvalidOperationException: kaboom");
        text.ShouldContain("LogUnhandled_WritesErrorLineWithStack");
    }

    [Fact]
    public void Write_AfterDispose_DoesNotThrow()
    {
        PreviewDebugLog log = PreviewDebugLog.Open(Path.Combine(_folder, "preview.log"))!;
        log.Dispose();

        Should.NotThrow(() => log.Write("test", "after dispose"));
    }

    [Fact]
    public void LogToolSelectionAndVariable_WriteExpectedFormat()
    {
        string path = Path.Combine(_folder, "preview.log");

        using (PreviewDebugLog log = PreviewDebugLog.Open(path)!)
        {
            log.LogToolSelection("Screens/Main", null, "Pressed");
            log.LogToolVariable("Screens/Main", "Input", "Height", 128f);
            log.LogToolVariable("Screens/Main", null, "Width", null);
        }

        string text = File.ReadAllText(path);
        text.ShouldContain(" tool-selection Screens/Main category=(none) state=Pressed");
        text.ShouldContain(" tool-var Screens/Main/Input Height=128");
        text.ShouldContain(" tool-var Screens/Main/(root) Width=(null)");
    }
}
