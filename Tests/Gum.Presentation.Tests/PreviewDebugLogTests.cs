using System;
using System.IO;
using Gum;
using GumPreview;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

public class PreviewDebugLogTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "PreviewDebugLogTests_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static readonly string[] Required = { "--project", "a.gumx", "--element", "Main" };

    [Fact]
    public void Parse_LogWithPath_SetsLogPath()
    {
        PreviewOptions options = PreviewOptions.Parse(new[] { "--project", "a.gumx", "--element", "Main", "--log", "out/p.log" });

        options.Error.ShouldBeNull();
        options.LogPath.ShouldBe(Path.GetFullPath("out/p.log"));
    }

    [Fact]
    public void Parse_NoLog_LeavesLogPathNull()
    {
        PreviewOptions.Parse(Required).LogPath.ShouldBeNull();
    }

    [Fact]
    public void Parse_LogWithNoValue_IsAnError()
    {
        PreviewOptions options = PreviewOptions.Parse(new[] { "--project", "a.gumx", "--element", "Main", "--log" });

        options.Error.ShouldNotBeNull();
        options.Error.ShouldContain("--log");
    }

    [Fact]
    public void Open_NullPath_ReturnsNullAndCreatesNothing()
    {
        PreviewDebugLog.Open(null).ShouldBeNull();

        Directory.Exists(_folder).ShouldBeFalse();
    }

    [Fact]
    public void LogReloadAndKeys_WithLog_WritesTimestampedLines()
    {
        string path = Path.Combine(_folder, "preview.log");
        HotReloadResult result = new HotReloadResult(new[]
        {
            new HotReloadIssue(HotReloadIssueKind.InstanceRebuilt, "rebuilt Box", "Main", "Box"),
        });

        using (PreviewDebugLog log = PreviewDebugLog.Open(path)!)
        {
            log.LogReload(result);
            log.LogFocus("TextBoxRuntime 'Input'");
            log.LogKeys(new[] { "A", "Back" }, "Ctrl", "TextBox 'Input'");
            log.LogException("show", new InvalidOperationException("boom"));
        }

        string text = File.ReadAllText(path);
        text.ShouldContain("reload finished, 1 issue(s)");
        text.ShouldContain("InstanceRebuilt Main/Box");
        text.ShouldContain("focus TextBoxRuntime 'Input'");
        text.ShouldContain("key A,Back modifiers=Ctrl focused=TextBox 'Input'");
        text.ShouldContain("error show: System.InvalidOperationException: boom");
        text.Split('\n')[0].ShouldMatch(@"^\d\d:\d\d:\d\d\.\d{3} log started");
    }

    [Fact]
    public void Open_UnwritablePath_ReturnsNull()
    {
        Directory.CreateDirectory(_folder);

        PreviewDebugLog.Open(_folder).ShouldBeNull();
    }
}
