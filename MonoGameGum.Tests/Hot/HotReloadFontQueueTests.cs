#pragma warning disable CS0618
using Gum;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracy2World;

namespace MonoGameGum.Tests.Hot;

// The font queue on the manager's fake clock: what is queued, copied and evicted, and what stops a
// reload that fonts alone keep asking for.
public class HotReloadFontQueueTests : BaseTestClass
{
    private PreviewAccuracy2World _world = null!;
    private GumHotReloadManager _manager = null!;
    private readonly List<string> _evicted = new List<string>();
    private readonly List<HotReloadResult> _results = new List<HotReloadResult>();
    private string _previousRelativeDirectory = global::ToolsUtilities.FileManager.RelativeDirectory;

    public override void Dispose()
    {
        _manager?.Stop();
        _world?.Dispose();
        global::ToolsUtilities.FileManager.RelativeDirectory = _previousRelativeDirectory;
        base.Dispose();
    }

    private void Boot(bool nestedBin = false, bool loadFromProjectFolder = false)
    {
        _world = new PreviewAccuracy2World();
        _world.Boot(separateBin: !loadFromProjectFolder);
        string bin = _world.Bin;
        if (nestedBin)
        {
            bin = Path.Combine(_world.Dir, "nest", "bin");
            foreach (string file in Directory.EnumerateFiles(_world.Bin, "*.*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(bin, Path.GetRelativePath(_world.Bin, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: true);
            }
        }
        global::ToolsUtilities.FileManager.RelativeDirectory = bin + "/";
        _manager = new GumHotReloadManager(
            GumService.ApplyProjectTextureFilter, GumAnimationLoader.LoadAnimationsFromProvider, _evicted.Add, () => _world.Now);
        _manager.ReloadFinished += _results.Add;
        _manager.Start(_world.Gumx);
        _manager.Stop();
        _binFolder = bin;
    }

    private string _binFolder = "";

    private string Source(string name) => Path.Combine(_world.Dir, "FontCache", name + ".fnt");

    // Waits out the debounce and runs a frame, as many times as it takes for the manager to stop asking for one.
    private int Frames()
    {
        int before = _results.Count;
        for (int i = 0; i < 30; i++)
        {
            _world.Now = _world.Now.AddMilliseconds(300);
            _manager.Update(new[] { _world.Screen });
        }
        return _results.Count - before;
    }

    private void Regenerate(string name, int lineHeight)
    {
        WriteFont(_world.Dir, name, advance: 11, lineHeight: lineHeight);
    }

    [Fact]
    public void FontNotifiedManyTimes_IsCopiedAndEvictedOnce()
    {
        Boot();
        Regenerate("Font20Pixel", 30);
        for (int i = 0; i < 1000; i++)
        {
            _manager.NotifyFileChanged(Source("Font20Pixel"));
        }

        Frames().ShouldBe(1);

        _evicted.Count(path => path.EndsWith("Font20Pixel.fnt")).ShouldBe(1);
    }

    [Fact]
    public void ChangeEventForACopyTheLoadFolderAlreadyHolds_IsIgnored()
    {
        Boot();
        Regenerate("Font20Pixel", 30);
        _manager.NotifyFileChanged(Source("Font20Pixel"));
        Frames().ShouldBe(1);
        int evicted = _evicted.Count;

        _manager.NotifyFileChanged(Source("Font20Pixel"));

        Frames().ShouldBe(0);
        _evicted.Count.ShouldBe(evicted);
    }

    [Fact]
    public void FontsThatKeepAskingForReloadsAlone_StopAfterAFewReloads_AndAreReportedOnce()
    {
        Boot();
        int reloads = 0;
        for (int i = 0; i < 12; i++)
        {
            Regenerate("Font20Pixel", 30 + i);
            _manager.NotifyFileChanged(Source("Font20Pixel"));
            reloads += Frames();
        }

        reloads.ShouldBeLessThan(12);
        _results.SelectMany(result => result.Issues).Count(issue => issue.Kind == HotReloadIssueKind.ReloadFailed).ShouldBe(1);

        // A change to the project makes the manager believe font events again.
        _manager.NotifyFileChanged(_world.Gumx);
        Frames();
        Regenerate("Font20Pixel", 99);
        _manager.NotifyFileChanged(Source("Font20Pixel"));
        Frames().ShouldBe(1);
    }

    [Fact]
    public void FontLockedByTheEditor_IsReportedOnce_TriedAgainOnce_ThenDropped()
    {
        Boot();
        Regenerate("Font20Pixel", 30);
        _manager.NotifyFileChanged(Source("Font20Pixel"));

        using (new FileStream(Source("Font20Pixel"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Frames();
        }

        _results.SelectMany(result => result.Issues).Count(issue => issue.Kind == HotReloadIssueKind.FontFailure).ShouldBeLessThanOrEqualTo(1);
        _world.Now = _world.Now.AddSeconds(5);
        Frames().ShouldBe(0);
    }

    [Fact]
    public void ChangesInsideABuildFolderUnderTheProject_AreIgnored()
    {
        Boot(nestedBin: true);
        string copy = Path.Combine(_binFolder, "FontCache", "Font20Pixel.fnt");
        File.SetLastWriteTimeUtc(copy, DateTime.UtcNow);

        _manager.NotifyFileChanged(copy);
        _manager.NotifyFileChanged(Path.Combine(_binFolder, "Proj.gumx"));

        Frames().ShouldBe(0);
    }

    [Fact]
    public void MoreFontsThanOneReloadCopies_ContinueInTheNextReload()
    {
        Boot();
        Directory.CreateDirectory(Path.Combine(_world.Dir, "FontCache"));
        List<string> fonts = new List<string>();
        for (int i = 0; i < 520; i++)
        {
            string path = Source("Bulk" + i);
            File.WriteAllText(path, "info");
            fonts.Add(path);
            _manager.NotifyFileChanged(path);
        }

        Frames().ShouldBeGreaterThanOrEqualTo(2);

        _evicted.Count(path => path.Contains("Bulk")).ShouldBe(520);
        fonts.All(path => File.Exists(Path.Combine(_binFolder, "FontCache", Path.GetFileName(path)))).ShouldBeTrue();
    }

    [Fact]
    public void FontLoadedFromTheProjectFolder_IsEvictedNotCopiedOntoItself()
    {
        Boot(loadFromProjectFolder: true);
        Regenerate("Font20Pixel", 30);
        _manager.NotifyFileChanged(Source("Font20Pixel"));

        Frames().ShouldBe(1);

        _results.SelectMany(result => result.Issues).ShouldBeEmpty();
        _evicted.Count(path => path.EndsWith("Font20Pixel.fnt")).ShouldBe(1);
        File.ReadAllText(Source("Font20Pixel")).ShouldContain("lineHeight=30");
    }

    [Fact]
    public void FontLoadedFromTheProjectFolder_ThenDeleted_IsEvictedAsARestartWouldNotFindIt()
    {
        Boot(loadFromProjectFolder: true);
        File.Delete(Source("Font20Pixel"));
        _manager.NotifyFileChanged(Source("Font20Pixel"));

        Frames().ShouldBe(1);

        _evicted.Count(path => path.EndsWith("Font20Pixel.fnt")).ShouldBe(1);
        _results.SelectMany(result => result.Issues).ShouldBeEmpty();
    }

    [Fact]
    public void QueuedFontThatDisappearsBeforeTheReload_IsNotAFailure()
    {
        Boot();
        string path = Source("Font99Temp");
        File.WriteAllText(path, "info");
        _manager.NotifyFileChanged(path);
        File.Delete(path);

        Frames();

        _results.SelectMany(result => result.Issues).ShouldBeEmpty();
    }

    [Fact]
    public void HiddenTempFileAnEditorSavesThrough_IsNotQueued()
    {
        Boot();

        _manager.NotifyFileChanged(Path.Combine(_world.Dir, "FontCache", ".!12345!Font20Pixel.fnt"));

        Frames().ShouldBe(0);
    }
}
