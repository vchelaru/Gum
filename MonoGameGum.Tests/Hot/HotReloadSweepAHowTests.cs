// ColoredRectangle is the standard the project files in this sweep instantiate.
#pragma warning disable CS0618
using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #6878 sweep, part A, "how it changed": timing of the saves, whole-tree rewrites such as a git
/// checkout, the project file listing new elements, and files caught half written.
/// </summary>
public class HotReloadSweepAHowTests : BaseTestClass
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

    [Fact]
    public void ToolSave_EveryFileRewrittenInOneBurst_ReloadsOnceWithAllChanges()
    {
        _world.Boot(Reader);
        SweepSpec edited = Reader with { Primary = 120, BoxX = 50, WidgetRed = 130, BaseX = 70, Extras = new[] { "Extra1" } };

        _world.SaveAll(edited);
        _world.NotifyAndSettle(_world.Gumx, _world.ScreenFile, _world.StylesFile, _world.WidgetFile, _world.BaseFile);

        _world.Reloads.ShouldBe(1);
        _world.Red("Box").ShouldBe(120);
        _world.Find("Box")!.X.ShouldBe(50f);
        _world.WidgetFillRed().ShouldBe(130);
        _world.Find("Widget1")!.X.ShouldBe(70f);
        _world.Find("Extra1").ShouldNotBeNull();
    }

    [Fact]
    public void SeveralFilesInsideTheDebounce_ReloadOnceAfterTheLastOne()
    {
        _world.Boot(Reader);
        _world.EditInPlace(_world.StylesFile, _world.ElementText(Reader with { Primary = 120 }, "Styles"));
        _world.Notify(_world.StylesFile);
        _world.Advance(150);
        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { Primary = 120, BoxX = 50 }, "MainScreen"));
        _world.Notify(_world.ScreenFile);

        _world.Advance(150);
        _world.Update();
        _world.Reloads.ShouldBe(0, "the last file is only 150 ms old");
        _world.Red("Box").ShouldBe(20, "nothing is applied half way through the burst");

        _world.Advance(60);
        _world.Update();

        _world.Reloads.ShouldBe(1);
        _world.Red("Box").ShouldBe(120);
        _world.Find("Box")!.X.ShouldBe(50f);
    }

    [Fact]
    public void SeveralFilesFarApart_ReloadSeparatelyAndEachShowsItsChange()
    {
        _world.Boot(Reader);

        _world.EditInPlace(_world.StylesFile, _world.ElementText(Reader with { Primary = 120 }, "Styles"));
        _world.NotifyAndSettle(_world.StylesFile);
        _world.Red("Box").ShouldBe(120);
        _world.Find("Box")!.X.ShouldBe(5f);

        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { Primary = 120, BoxX = 50 }, "MainScreen"));
        _world.NotifyAndSettle(_world.ScreenFile);

        _world.Reloads.ShouldBe(2);
        _world.Red("Box").ShouldBe(120, "the second reload keeps the first edit");
        _world.Find("Box")!.X.ShouldBe(50f);
    }

    [Fact]
    public void FileNotifiedBeforeTheDebounce_IsNotReloadedUntilTheDebounceHasPassed()
    {
        _world.Boot(Reader);

        _world.Notify(_world.ScreenFile);
        _world.Advance(199);
        _world.Update();

        _world.Reloads.ShouldBe(0);
    }

    [Fact]
    public void AtomicSave_TempFileEventIsIgnoredAndTheFinalNameReloads()
    {
        _world.Boot(Reader);
        string text = _world.ElementText(Reader with { BoxX = 50 }, "MainScreen");

        _world.EditAtomic(_world.ScreenFile, text);
        _world.NotifyAndSettle(_world.ScreenFile + ".tmp");
        _world.Reloads.ShouldBe(0, "a .tmp name is not a watched extension");

        _world.NotifyAndSettle(_world.ScreenFile);
        _world.Reloads.ShouldBe(1);
        _world.Find("Box")!.X.ShouldBe(50f);
    }

    [Fact]
    public void ProjectFileListsANewElement_TheNewInstanceAppearsWithItsFileValues()
    {
        _world.Boot(Reader);
        SweepSpec withGadget = Reader with { WithGadget = true };

        _world.SaveAll(withGadget);
        _world.NotifyAndSettle(_world.Gumx);

        GraphicalUiElement? gadget = _world.Find("Gadget1");
        gadget.ShouldNotBeNull();
        ((ColoredRectangleRuntime)gadget!.GetGraphicalUiElementByName("Fill")!).Red.ShouldBe(99);
    }

    [Fact]
    public void NewElementFileAndScreenEditedButNoProjectFileWrite_DoesNotThrowAndKeepsTheRest()
    {
        _world.Boot(Reader);
        SweepSpec withGadget = Reader with { WithGadget = true, BoxX = 50 };
        _world.EditInPlace(_world.GadgetFile, _world.ElementText(withGadget, "Widgets/Gadget"));
        _world.EditInPlace(_world.ScreenFile, _world.ElementText(withGadget, "MainScreen"));

        Should.NotThrow(() => _world.NotifyAndSettle(_world.GadgetFile, _world.ScreenFile));

        _world.Find("Box")!.X.ShouldBe(50f);
        _world.Find("Widget1").ShouldNotBeNull();
    }

    [Fact]
    public void GitCheckout_ManyFilesRewrittenOneDeletedOneAdded_TreeMatchesTheCheckedOutProject()
    {
        _world.Boot(Reader with { Extras = new[] { "Extra1" } });
        SweepSpec checkedOut = Reader with
        {
            Primary = 120, BoxX = 50, WidgetRed = 130, BaseX = 70, WithGadget = true, WithWidget = false,
            LabelText = "Branch"
        };

        _world.SaveAll(checkedOut);
        File.Delete(_world.WidgetFile);
        _world.NotifyAndSettle(
            _world.Gumx, _world.ScreenFile, _world.StylesFile, _world.BaseFile, _world.GadgetFile, _world.BehaviorFile);

        _world.Reloads.ShouldBe(1);
        _world.Find("Widget1").ShouldBeNull();
        _world.Find("Extra1").ShouldBeNull();
        _world.Find("Gadget1").ShouldNotBeNull();
        _world.Red("Box").ShouldBe(120);
        _world.Find("Box")!.X.ShouldBe(50f);
        ((TextRuntime)_world.Find("Label")!).Text.ShouldBe("Branch");
        _world.Screen.Children.Count.ShouldBe(3);
    }

    [Fact]
    public void ReloadingTwiceWithoutAnyEdit_ChangesNothing()
    {
        _world.Boot(Reader with { Extras = new[] { "Extra1" } });

        _world.NotifyAndSettle(_world.ScreenFile);
        _world.NotifyAndSettle(_world.ScreenFile);

        _world.Reloads.ShouldBe(2);
        _world.Screen.Children.Count.ShouldBe(4);
        _world.Red("Box").ShouldBe(20);
        _world.WidgetFillRed().ShouldBe(30);
        _world.Find("Widget1")!.X.ShouldBe(7f);
    }

    [Fact]
    public void RuntimeAddedChild_SurvivesAFileEditReload()
    {
        _world.Boot(Reader);
        ContainerRuntime added = new ContainerRuntime { Name = "FromCode" };
        added.Parent = _world.Screen;

        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { BoxX = 50 }, "MainScreen"));
        _world.NotifyAndSettle(_world.ScreenFile);

        _world.Screen.Children.ShouldContain(added);
        added.Parent.ShouldBe(_world.Screen);
    }

    [Fact]
    public void HalfWrittenFileThenTheCompleteWrite_TheNextReloadAppliesTheEdit()
    {
        _world.Boot(Reader);
        string good = _world.ElementText(Reader with { BoxX = 50 }, "MainScreen");
        _world.EditInPlace(_world.ScreenFile, good.Substring(0, good.Length / 2));
        try { _world.NotifyAndSettle(_world.ScreenFile); } catch (Exception) { /* a reload that throws on a half-written file is a separate bug (#6878 red test) */ }

        _world.EditInPlace(_world.ScreenFile, good);
        _world.NotifyAndSettle(_world.ScreenFile);

        _world.Find("Box")!.X.ShouldBe(50f);
        _world.Red("Box").ShouldBe(20);
    }
}
