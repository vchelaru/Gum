using System;
using System.IO;
using System.Linq;
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
    public void Parse_LogWithInvalidCharacter_IsAnErrorNotAnException()
    {
        PreviewOptions options = PreviewOptions.Parse(new[] { "--project", "a.gumx", "--element", "Main", "--log", "bad\0path.log" });

        options.Error.ShouldNotBeNull();
        options.Error.ShouldContain("--log");
        options.LogPath.ShouldBeNull();
    }

    [Fact]
    public void Open_InvalidCharacterPath_ReturnsNull()
    {
        PreviewDebugLog.Open("bad\0path.log").ShouldBeNull();
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

    [Fact]
    public void DefaultPath_IsFixedFileInTempFolder()
    {
        PreviewDebugLog.DefaultPath.ShouldBe(Path.Combine(Path.GetTempPath(), "GumPreview", "preview.log"));
    }

    [Fact]
    public void Parse_LogDefault_SetsLogPathToDefaultPath()
    {
        PreviewOptions options = PreviewOptions.Parse(new[] { "--project", "a.gumx", "--element", "Main", "--log-default" });

        options.Error.ShouldBeNull();
        options.LogPath.ShouldBe(PreviewDebugLog.DefaultPath);
    }

    [Fact]
    public void Parse_LogDefaultBeforeValueFlag_DoesNotSwallowItAsValue()
    {
        PreviewOptions options = PreviewOptions.Parse(new[] { "--project", "a.gumx", "--log-default", "--element", "Main" });

        options.Error.ShouldBeNull();
        options.ElementName.ShouldBe("Main");
    }

    [Fact]
    public void Write_WhileLogStillOpen_LineIsReadableFromFile()
    {
        string path = Path.Combine(_folder, "preview.log");

        using PreviewDebugLog log = PreviewDebugLog.Open(path)!;
        log.Write("test", "visible now");

        using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using StreamReader reader = new StreamReader(stream);
        reader.ReadToEnd().ShouldContain("test visible now");
    }

    [Fact]
    public void LogMouseSelectionAndInstances_WriteExpectedFormat()
    {
        string path = Path.Combine(_folder, "preview.log");

        using (PreviewDebugLog log = PreviewDebugLog.Open(path)!)
        {
            log.LogMouse("down", 10, 20);
            log.LogSelection("TextBox 'Input'", 2, 3, 5);
            log.LogSelectionInstances("TextBox 'Input'", true, 12, 3, 2);
        }

        string text = File.ReadAllText(path);
        text.ShouldContain(" mouse down 10,20");
        text.ShouldContain(" selection TextBox 'Input' start=2 length=3 caret=5");
        text.ShouldContain(" selection-instances TextBox 'Input' wrap=True textLength=12 count=3 visible=2");
    }

    private static TextBoxSnapshot Box(string text = "abc", bool wrap = false, int start = 0, int length = 0, int caret = 0, int count = 1, int visible = 0) =>
        new TextBoxSnapshot("Input", text, wrap, start, length, caret, count, visible);

    private string[] TrackedLines(Action<PreviewInputTracker> act)
    {
        string path = Path.Combine(_folder, "tracker.log");
        using (PreviewDebugLog log = PreviewDebugLog.Open(path)!)
        {
            act(new PreviewInputTracker(log));
        }
        return File.ReadAllLines(path).Skip(1).ToArray();
    }

    [Fact]
    public void ObserveMouse_PressDragRelease_LogsDownDragUp()
    {
        string[] lines = TrackedLines(t =>
        {
            t.ObserveMouse(5, 5, false);
            t.ObserveMouse(5, 5, true);
            t.ObserveMouse(5, 5, true);
            t.ObserveMouse(9, 5, true);
            t.ObserveMouse(9, 5, false);
        }).Where(l => !l.Contains(" mouse move ")).ToArray(); // cursor moves are logged since #7533; this test is about buttons

        lines.Length.ShouldBe(3);
        lines[0].ShouldEndWith("mouse down 5,5");
        lines[1].ShouldEndWith("mouse drag 9,5");
        lines[2].ShouldEndWith("mouse up 9,5");
    }

    [Fact]
    public void ObserveTextBox_SelectionChange_LogsOnlyOnChange()
    {
        string[] lines = TrackedLines(t =>
        {
            t.ObserveTextBox(Box());
            t.ObserveTextBox(Box());
            t.ObserveTextBox(Box(start: 1, length: 2, caret: 3));
        });

        lines.Count(l => l.Contains(" selection ")).ShouldBe(2);
        lines.Last(l => l.Contains(" selection ")).ShouldEndWith("selection Input start=1 length=2 caret=3");
    }

    [Fact]
    public void ObserveTextBox_WrapOrTextChange_LogsInstanceCountAndVisibility()
    {
        string[] lines = TrackedLines(t =>
        {
            t.ObserveTextBox(Box());
            t.ObserveTextBox(Box(wrap: true, count: 3, visible: 2));
            t.ObserveTextBox(Box(wrap: true, count: 3, visible: 2));
        });

        string[] instances = lines.Where(l => l.Contains(" selection-instances ")).ToArray();
        instances.Length.ShouldBe(2);
        instances[1].ShouldEndWith("selection-instances Input wrap=True textLength=3 count=3 visible=2");
    }

    [Fact]
    public void ObserveTextBox_NoFocusedTextBox_LogsNothing()
    {
        TrackedLines(t => t.ObserveTextBox(null)).ShouldBeEmpty();
    }
}
