#pragma warning disable CS0618
using Gum.DataTypes;
using Shouldly;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracy2World;

namespace MonoGameGum.Tests.Hot;

// A real FileSystemWatcher on a project with many fonts: copying a font into the game's folder must not
// make the watcher queue the same fonts again (macOS reports a copy as a change of its source too).
public class HotReloadFontWatcherTests : BaseTestClass
{
    private const int FontCount = 60;
    private PreviewAccuracy2World? _world;

    public override void Dispose()
    {
        _world?.Manager?.Stop();
        _world?.Dispose();
        global::ToolsUtilities.FileManager.RelativeDirectory = _previousRelativeDirectory;
        base.Dispose();
    }

    private readonly string _previousRelativeDirectory = global::ToolsUtilities.FileManager.RelativeDirectory;

    // The game runs from a copy of the project that a plain recursive copy made: no write time survives.
    private PreviewAccuracy2World BootWithManyFonts(bool nestedBin)
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World();
        _world = world;
        world.Boot(
            separateBin: true,
            beforeBoot: dir =>
            {
                for (int i = 0; i < FontCount; i++)
                {
                    WriteFont(dir, "Font" + (30 + i) + "Pixel", advance: 10, lineHeight: 24);
                }
            });
        string bin = world.Bin;
        if (nestedBin)
        {
            bin = Path.Combine(world.Dir, "nest", "bin", "Content");
            CopyTree(world.Bin, bin);
        }
        foreach (string file in Directory.EnumerateFiles(bin, "*.*", SearchOption.AllDirectories))
        {
            File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddMinutes(1));
        }
        global::ToolsUtilities.FileManager.RelativeDirectory = bin + "/";
        // Let the events the writes above caused arrive before the watcher starts.
        Thread.Sleep(1500);
        world.Manager.Start(world.Gumx);
        return world;
    }

    private static void CopyTree(string from, string to)
    {
        foreach (string file in Directory.EnumerateFiles(from, "*.*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    // The project saved with one edit, the way the tool writes it, without World.Save's snapshot of the folder.
    private static void SaveProject(PreviewAccuracy2World world)
    {
        GumProjectSave edited = Baseline();
        Std(MainScreen(edited), "Caption", "Text", "Edited");
        edited.Initialize();
        SaveAll(edited, world.Gumx);
    }

    // The game's frame loop on the real clock.
    private static void RunFrames(PreviewAccuracy2World world, TimeSpan duration)
    {
        Stopwatch watch = Stopwatch.StartNew();
        while (watch.Elapsed < duration)
        {
            world.Now = DateTime.UtcNow;
            world.Manager.Update(new[] { world.Screen });
            Thread.Sleep(20);
        }
    }

    private static string Describe(PreviewAccuracy2World world) =>
        string.Join(" | ", world.Results.Select(result => string.Join(",", result.Issues.Select(issue => issue.Kind + ":" + issue.Message.Replace('\n', ' ')[..Math.Min(90, issue.Message.Length)]))));

    private void ShouldSettle(PreviewAccuracy2World world)
    {
        RunFrames(world, TimeSpan.FromSeconds(2));
        int settled = world.Results.Count;
        settled.ShouldBeLessThan(5, "reloads in the first two seconds: " + Describe(world));

        RunFrames(world, TimeSpan.FromSeconds(3));
        world.Results.Count.ShouldBe(settled, "reloads in the quiet three seconds after");
        world.Results.SelectMany(result => result.Issues).ShouldBeEmpty(string.Join(" | ", world.Results.SelectMany(result => result.Issues).Select(issue => issue.Kind + ": " + issue.Message)));
    }

    [Fact]
    public void SiblingBin_ProjectSaved_AfterAPlainCopy_Settles()
    {
        PreviewAccuracy2World world = BootWithManyFonts(nestedBin: false);

        SaveProject(world);

        ShouldSettle(world);
    }

    [Fact]
    public void SiblingBin_OneFontRegenerated_Settles()
    {
        PreviewAccuracy2World world = BootWithManyFonts(nestedBin: false);

        WriteFont(world.Dir, "Font30Pixel", advance: 13, lineHeight: 30);

        ShouldSettle(world);
    }

    [Fact]
    public void NestedBin_ProjectSaved_AfterAPlainCopy_Settles()
    {
        PreviewAccuracy2World world = BootWithManyFonts(nestedBin: true);

        SaveProject(world);

        ShouldSettle(world);
    }

    [Fact]
    public void NestedBin_OneFontRegenerated_Settles()
    {
        PreviewAccuracy2World world = BootWithManyFonts(nestedBin: true);

        WriteFont(world.Dir, "Font30Pixel", advance: 13, lineHeight: 30);

        ShouldSettle(world);
    }
}
