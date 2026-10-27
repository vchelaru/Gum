#pragma warning disable CS0618
using Gum;
using Gum.DataTypes;
using Gum.Managers;
using GumRuntime;
using Shouldly;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #6878 sweep, part A: a project file caught half written, or an element file that is gone while
/// the project still lists it, must not crash the game or wipe what is running.
/// </summary>
public class HotReloadSweepAUnreadableFileTests : BaseTestClass
{
    private readonly SweepWorld _world = new SweepWorld();

    public override void Dispose()
    {
        _world.Dispose();
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static readonly SweepSpec Reader = new SweepSpec { BoxReadsStyle = true };


    public enum BrokenFile { Screen, Component, ProjectFile }

    public enum Breakage { Empty, TruncatedMidElement, Garbage }

    private string PathOf(BrokenFile file) => file switch
    {
        BrokenFile.Screen => _world.ScreenFile,
        BrokenFile.Component => _world.StylesFile,
        _ => _world.Gumx
    };

    private string ElementOf(BrokenFile file) => file switch
    {
        BrokenFile.Screen => "MainScreen",
        BrokenFile.Component => "Styles",
        _ => ""
    };

    private static string Break(string good, Breakage breakage) => breakage switch
    {
        Breakage.Empty => "",
        Breakage.TruncatedMidElement => good.Substring(0, good.Length / 2),
        _ => "\0\0\0not xml at all"
    };

    [Theory]
    [InlineData(BrokenFile.Screen, Breakage.Empty)]
    [InlineData(BrokenFile.Screen, Breakage.TruncatedMidElement)]
    [InlineData(BrokenFile.Screen, Breakage.Garbage)]
    [InlineData(BrokenFile.Component, Breakage.Empty)]
    [InlineData(BrokenFile.Component, Breakage.TruncatedMidElement)]
    [InlineData(BrokenFile.ProjectFile, Breakage.Empty)]
    [InlineData(BrokenFile.ProjectFile, Breakage.TruncatedMidElement)]
    [InlineData(BrokenFile.ProjectFile, Breakage.Garbage)]
    public void HalfWrittenFile_ReloadDoesNotCrashAndTheRunningStateIsKept(BrokenFile file, Breakage breakage)
    {
        _world.Boot(Reader);
        string path = PathOf(file);
        _world.EditInPlace(path, Break(File.ReadAllText(path), breakage));

        Should.NotThrow(() => _world.NotifyAndSettle(path));

        _world.Red("Box").ShouldBe(20);
        _world.Find("Box")!.X.ShouldBe(5f);
        _world.Screen.Children.Count.ShouldBe(3);
        _world.WidgetFillRed().ShouldBe(30);
        ObjectFinder.Self.GumProjectSave.ShouldNotBeNull();
        ObjectFinder.Self.GumProjectSave!.Screens.Count.ShouldBe(1, "the finder still holds a usable project");
    }

    [Fact]
    public void GitCheckout_ElementFileDeletedButStillInstantiated_DoesNotThrowAndKeepsTheOtherValues()
    {
        _world.Boot(Reader);

        File.Delete(_world.WidgetFile);
        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { BoxX = 50 }, "MainScreen"));

        Should.NotThrow(() => _world.NotifyAndSettle(_world.ScreenFile));

        _world.Find("Box")!.X.ShouldBe(50f);
    }
}
