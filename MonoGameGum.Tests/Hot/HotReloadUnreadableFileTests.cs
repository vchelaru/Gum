#pragma warning disable CS0618
using Gum;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #6978: a project file that cannot be read during a reload (half written, locked, deleted) leaves the
/// running UI untouched, reports the file and the reason, and the next complete write reloads normally.
/// </summary>
public class HotReloadUnreadableFileTests : BaseTestClass
{
    private static readonly SweepSpec Reader = new SweepSpec { BoxReadsStyle = true };

    private readonly SweepWorld _world = new SweepWorld();
    private readonly List<string> _errors = new List<string>();

    public HotReloadUnreadableFileTests()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError += _errors.Add;
    }

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError -= _errors.Add;
        _world.Dispose();
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    // The first failed read is retried after the debounce; the second is reported.
    private void SettleThroughRetry(string path)
    {
        _world.NotifyAndSettle(path);
        _world.Advance(300);
        _world.Update();
    }

    [Fact]
    public void HalfWrittenFile_ReportsTheFileAndTheReason()
    {
        _world.Boot(Reader);
        _world.EditInPlace(_world.ScreenFile, "<GumScreen");

        SettleThroughRetry(_world.ScreenFile);

        _errors.Count.ShouldBe(1);
        _errors[0].ShouldContain("MainScreen");
        _errors[0].ShouldContain("unchanged");
    }

    [Fact]
    public void FailedReload_KeepsTheSameProjectInstanceInTheFinder()
    {
        _world.Boot(Reader);
        GumProjectSave before = ObjectFinder.Self.GumProjectSave!;
        _world.EditInPlace(_world.Gumx, "");

        SettleThroughRetry(_world.Gumx);

        ObjectFinder.Self.GumProjectSave.ShouldBeSameAs(before);
        _world.Reloads.ShouldBe(0);
    }

    [Fact]
    public void FileLockedByTheEditor_KeepsTheRunningStateAndReports()
    {
        _world.Boot(Reader);
        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { BoxX = 50 }, "MainScreen"));

        using (new FileStream(_world.ScreenFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Should.NotThrow(() => SettleThroughRetry(_world.ScreenFile));
        }

        _world.Find("Box")!.X.ShouldBe(5f);
        _errors.Count.ShouldBe(1);
        _errors[0].ShouldContain("MainScreen");
    }

    [Fact]
    public void FileLockedBrieflyAtTheFirstAttempt_IsReadOnTheRetry()
    {
        _world.Boot(Reader);
        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { BoxX = 50 }, "MainScreen"));

        FileStream locked = new FileStream(_world.ScreenFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        try
        {
            _world.NotifyAndSettle(_world.ScreenFile);
        }
        finally
        {
            locked.Dispose();
        }
        _world.Advance(300);
        _world.Update();

        _world.Find("Box")!.X.ShouldBe(50f);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void NextCompleteWriteAfterAFailedReload_Applies()
    {
        _world.Boot(Reader);
        _world.EditInPlace(_world.ScreenFile, "");
        SettleThroughRetry(_world.ScreenFile);
        _world.Reloads.ShouldBe(0);

        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { BoxX = 60 }, "MainScreen"));
        _world.NotifyAndSettle(_world.ScreenFile);

        _world.Reloads.ShouldBe(1);
        _world.Find("Box")!.X.ShouldBe(60f);
    }

    [Fact]
    public void HalfWrittenFileCompletedWithinTheDebounceWindow_ReloadsOnceWithoutError()
    {
        _world.Boot(Reader);
        _world.EditInPlace(_world.ScreenFile, "<GumScreen");
        _world.Notify(_world.ScreenFile);
        _world.Advance(100);
        _world.Update();

        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { BoxX = 70 }, "MainScreen"));
        _world.NotifyAndSettle(_world.ScreenFile);

        _world.Reloads.ShouldBe(1);
        _world.Find("Box")!.X.ShouldBe(70f);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void DeletedElementFileStillListed_ReportsItAndReloadsTheRest()
    {
        _world.Boot(Reader);
        File.Delete(_world.WidgetFile);
        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { BoxX = 50 }, "MainScreen"));

        _world.NotifyAndSettle(_world.ScreenFile);

        _world.Find("Box")!.X.ShouldBe(50f);
        _errors.Count.ShouldBe(1);
        _errors[0].ShouldContain("Widget");
    }
}
