#pragma warning disable CS0618
using Gum;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
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
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Collect;
    }

    private string? _originalRelativeDirectory;

    // The event is static, so font-loading noise from other tests can arrive too.
    private void Collect(string message)
    {
        if (message.Contains("Hot reload") || message.Contains("Could not copy font"))
        {
            _errors.Add(message);
        }
    }

    public override void Dispose()
    {
        if (_originalRelativeDirectory != null)
        {
            global::ToolsUtilities.FileManager.RelativeDirectory = _originalRelativeDirectory;
        }
        CustomSetPropertyOnRenderable.PropertyAssignmentError -= Collect;
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
        // The element the missing file held is left empty, so its base type is gone too.
        _errors.Count.ShouldBe(2);
        _errors[0].ShouldContain("Widget");
        _errors[1].ShouldContain("changed its base type");
    }

    // Replaces the world's manager with one whose host hooks the test controls.
    private void UseManager(Func<GumProjectSave, Gum.Bundle.IGumFileProvider, int> loadAnimations, Action<GumProjectSave> applyFilter)
    {
        GumHotReloadManager manager = new GumHotReloadManager(
            applyFilter, loadAnimations, path => _world.Disposed.Add(path), () => _world.Now);
        manager.ReloadCompleted += () => _world.Reloads++;
        manager.Start(_world.Gumx);
        manager.Stop();
        _world.Manager = manager;
    }

    [Fact]
    public void FailureAfterTheLoad_RestoresTheFinderAndFilter_ReportsOnceWithItsOwnWording_AndTheNextSaveReloads()
    {
        _world.Boot(Reader);
        GumProjectSave before = ObjectFinder.Self.GumProjectSave!;
        List<GumProjectSave> filterCalls = new List<GumProjectSave>();
        bool fail = true;
        UseManager(
            (project, provider) => fail ? throw new InvalidOperationException("animation boom") : 0,
            filterCalls.Add);
        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { BoxX = 50 }, "MainScreen"));

        SettleThroughRetry(_world.ScreenFile);

        ObjectFinder.Self.GumProjectSave.ShouldBeSameAs(before);
        filterCalls[filterCalls.Count - 1].ShouldBeSameAs(before);
        _errors.Count.ShouldBe(1);
        _errors[0].ShouldContain("animation boom");
        _errors[0].ShouldContain("partially updated");
        _errors[0].ShouldNotContain("unchanged");

        fail = false;
        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { BoxX = 60 }, "MainScreen"));
        _world.NotifyAndSettle(_world.ScreenFile);

        _world.Reloads.ShouldBe(1);
        _world.Find("Box")!.X.ShouldBe(60f);
    }

    [Fact]
    public void NewChangeAfterAFailure_GetsItsOwnRetryBeforeAnythingIsReported()
    {
        _world.Boot(Reader);
        _world.EditInPlace(_world.ScreenFile, "");

        _world.NotifyAndSettle(_world.ScreenFile);
        _world.NotifyAndSettle(_world.ScreenFile);

        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void FontLockedByTheEditor_IsReportedAndCopiedOnTheNextReload()
    {
        _originalRelativeDirectory = global::ToolsUtilities.FileManager.RelativeDirectory;
        string bin = Path.Combine(_world.Dir, "bin") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(bin);
        global::ToolsUtilities.FileManager.RelativeDirectory = bin;
        _world.Boot(Reader);
        string fontCache = Path.Combine(_world.Dir, "FontCache");
        Directory.CreateDirectory(fontCache);
        string fnt = Path.Combine(fontCache, "Font18Arial.fnt");
        File.WriteAllText(fnt, "info");
        _world.Notify(fnt);

        FileStream locked = new FileStream(fnt, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        try
        {
            Should.NotThrow(() => _world.NotifyAndSettle(_world.ScreenFile));
        }
        finally
        {
            locked.Dispose();
        }

        _errors.Count.ShouldBe(1);
        _errors[0].ShouldContain("Font18Arial.fnt");
        _world.Disposed.ShouldBeEmpty();

        _world.NotifyAndSettle(_world.ScreenFile);

        _world.Disposed.ShouldContain(p => p.EndsWith("Font18Arial.fnt"));
    }
}
