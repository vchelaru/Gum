using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.StateAnimation.SaveClasses;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #6878 sweep, part A, "what changed": each kind of edit reaches the running objects through the
/// real manager flow (notify, debounce, Update) when only the changed file is written, by a text
/// editor in place or atomically (temp file then replace).
/// </summary>
public class HotReloadSweepAWhatTests : BaseTestClass
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
    public void Boot_FromDisk_ShowsTheSavedValues()
    {
        _world.Boot(Reader);

        _world.Red("Box").ShouldBe(20, "the Box reads the Primary swatch");
        _world.Find("Box")!.X.ShouldBe(5f);
        _world.Find("Widget1")!.X.ShouldBe(7f, "inherited from the base component");
        _world.WidgetFillRed().ShouldBe(30);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StylesVariable_OnlyTheStylesFileEdited_ReaderShowsTheNewValue(bool atomic)
    {
        _world.Boot(Reader);

        _world.Edit(atomic, _world.StylesFile, _world.ElementText(Reader with { Primary = 120 }, "Styles"));
        _world.NotifyAndSettle(_world.StylesFile);

        _world.Reloads.ShouldBe(1);
        _world.Red("Box").ShouldBe(120);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReferenceTarget_OnlyTheScreenFileRetargeted_ReaderShowsTheNewTarget(bool atomic)
    {
        _world.Boot(Reader);

        SweepSpec retargeted = Reader with { StyleTarget = "Accent" };
        _world.Edit(atomic, _world.ScreenFile, _world.ElementText(retargeted, "MainScreen"));
        _world.NotifyAndSettle(_world.ScreenFile);

        _world.Red("Box").ShouldBe(220);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlainValue_ScreenFileEdited_ValuesAndTextFlow(bool atomic)
    {
        _world.Boot(Reader);

        SweepSpec edited = new SweepSpec { BoxX = 55, BoxRed = 77, LabelText = "Changed" };
        _world.Edit(atomic, _world.ScreenFile, _world.ElementText(edited, "MainScreen"));
        _world.NotifyAndSettle(_world.ScreenFile);

        _world.Find("Box")!.X.ShouldBe(55f);
        _world.Red("Box").ShouldBe(77);
        ((TextRuntime)_world.Find("Label")!).Text.ShouldBe("Changed");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void State_HighlightedValueEditedWhileInThatState_ShowsTheNewStateValue(bool atomic)
    {
        _world.Boot(Reader);
        _world.Find("Widget1")!.ApplyState("Look", "Highlighted");
        _world.WidgetFillRed().ShouldBe(40, "sanity");

        _world.Edit(atomic, _world.WidgetFile, _world.ElementText(Reader with { WidgetHighlighted = 140 }, "Widgets/Widget"));
        _world.NotifyAndSettle(_world.WidgetFile);

        _world.WidgetFillRed().ShouldBe(140);
    }

    [Fact]
    public void State_HighlightedValueEdited_AppliedAfterTheReload_UsesTheNewValue()
    {
        _world.Boot(Reader);

        _world.EditInPlace(_world.WidgetFile, _world.ElementText(Reader with { WidgetHighlighted = 140 }, "Widgets/Widget"));
        _world.NotifyAndSettle(_world.WidgetFile);
        _world.Find("Widget1")!.ApplyState("Look", "Highlighted");

        _world.WidgetFillRed().ShouldBe(140);
    }

    [Fact]
    public void State_DefaultValueEditedWhileNotInAState_ShowsTheNewDefault()
    {
        _world.Boot(Reader);

        _world.EditInPlace(_world.WidgetFile, _world.ElementText(Reader with { WidgetRed = 130 }, "Widgets/Widget"));
        _world.NotifyAndSettle(_world.WidgetFile);

        _world.WidgetFillRed().ShouldBe(130);
    }

    [Fact]
    public void State_AddedByTheEdit_CanBeAppliedAfterTheReload()
    {
        SweepSpec without = Reader with { WidgetHasHighlighted = false };
        _world.Boot(without);

        _world.EditInPlace(_world.WidgetFile, _world.ElementText(Reader with { WidgetHighlighted = 66 }, "Widgets/Widget"));
        _world.NotifyAndSettle(_world.WidgetFile);
        _world.Find("Widget1")!.ApplyState("Look", "Highlighted");

        _world.WidgetFillRed().ShouldBe(66);
    }

    [Fact]
    public void State_RemovedByTheEditWhileApplied_FallsBackToTheNewDefaultWithoutThrowing()
    {
        _world.Boot(Reader);
        _world.Find("Widget1")!.ApplyState("Look", "Highlighted");

        _world.EditInPlace(_world.WidgetFile, _world.ElementText(Reader with { WidgetHasHighlighted = false, WidgetRed = 31 }, "Widgets/Widget"));
        _world.NotifyAndSettle(_world.WidgetFile);

        _world.Reloads.ShouldBe(1);
        _world.WidgetFillRed().ShouldBe(31);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Instance_AddedInTheScreenFile_AppearsOnce(bool atomic)
    {
        _world.Boot(Reader);

        _world.Edit(atomic, _world.ScreenFile, _world.ElementText(Reader with { Extras = new[] { "Extra1" } }, "MainScreen"));
        _world.NotifyAndSettle(_world.ScreenFile);

        _world.Screen.Children.Count(c => c.Name == "Extra1").ShouldBe(1);
        _world.Screen.Children.Count.ShouldBe(4);
    }

    [Fact]
    public void Instance_AddedThenReloadedAgainWithTheSameFile_IsNotDuplicated()
    {
        _world.Boot(Reader);
        SweepSpec added = Reader with { Extras = new[] { "Extra1" } };

        _world.EditInPlace(_world.ScreenFile, _world.ElementText(added, "MainScreen"));
        _world.NotifyAndSettle(_world.ScreenFile);
        _world.NotifyAndSettle(_world.ScreenFile);

        _world.Reloads.ShouldBe(2);
        _world.Screen.Children.Count.ShouldBe(4);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Instance_RemovedInTheScreenFile_LeavesTheTree(bool atomic)
    {
        _world.Boot(Reader with { Extras = new[] { "Extra1" } });
        _world.Screen.Children.Count.ShouldBe(4, "sanity");

        _world.Edit(atomic, _world.ScreenFile, _world.ElementText(Reader, "MainScreen"));
        _world.NotifyAndSettle(_world.ScreenFile);

        _world.Find("Extra1").ShouldBeNull();
        _world.Screen.Children.Count.ShouldBe(3);
    }

    [Fact]
    public void Instance_RetypedInTheScreenFile_IsRebuiltAsTheNewType()
    {
        _world.Boot(Reader);
        _world.Find("Box").ShouldBeOfType<ColoredRectangleRuntime>();

        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { BoxType = "Container" }, "MainScreen"));
        _world.NotifyAndSettle(_world.ScreenFile);

        _world.Find("Box").ShouldBeOfType<ContainerRuntime>();
        _world.Screen.Children.Count(c => c.Name == "Box").ShouldBe(1);
        _world.Find("Box")!.X.ShouldBe(5f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BaseTypeDefault_OnlyTheBaseFileEdited_ReachesTheDerivedInstance(bool atomic)
    {
        _world.Boot(Reader);

        _world.Edit(atomic, _world.BaseFile, _world.ElementText(Reader with { BaseX = 70 }, "Widgets/Base"));
        _world.NotifyAndSettle(_world.BaseFile);

        _world.Find("Widget1")!.X.ShouldBe(70f);
    }

    [Fact]
    public void Behavior_NewBehaviorFileAndProjectListing_ReloadsWithoutDisturbingTheTree()
    {
        _world.Boot(Reader);
        SweepSpec withBehavior = Reader with { WithExtraBehavior = true };

        _world.SaveAll(withBehavior);
        _world.NotifyAndSettle(_world.BehaviorFile, _world.Gumx);

        _world.Reloads.ShouldBe(1);
        ObjectFinder.Self.GumProjectSave!.Behaviors.Any(b => b.Name == "ExtraBehavior").ShouldBeTrue();
        _world.Red("Box").ShouldBe(20);
    }

    [Fact]
    public void Behavior_OnlyTheBehaviorFileEdited_TriggersAReload()
    {
        _world.Boot(Reader with { WithExtraBehavior = true });

        _world.EditInPlace(_world.BehaviorFile, File.ReadAllText(_world.BehaviorFile));
        _world.NotifyAndSettle(_world.BehaviorFile);

        _world.Reloads.ShouldBe(1);
    }

    [Fact]
    public void Font_FntAndItsPagesChanged_AreCopiedToBinAndEvictedFromTheCache()
    {
        string bin = Path.Combine(_world.Dir, "bin") + "/";
        Directory.CreateDirectory(bin);
        FileManager.RelativeDirectory = bin;
        _world.Boot(Reader);
        string cache = Path.Combine(_world.Dir, "FontCache");
        Directory.CreateDirectory(cache);
        File.WriteAllText(Path.Combine(cache, "Arial.fnt"), "info");
        File.WriteAllText(Path.Combine(cache, "Arial_0.png"), "png");
        File.WriteAllText(Path.Combine(cache, "Other_0.png"), "png");

        _world.NotifyAndSettle(Path.Combine(cache, "Arial.fnt"));

        File.Exists(Path.Combine(bin, "FontCache", "Arial.fnt")).ShouldBeTrue();
        File.Exists(Path.Combine(bin, "FontCache", "Arial_0.png")).ShouldBeTrue();
        File.Exists(Path.Combine(bin, "FontCache", "Other_0.png")).ShouldBeFalse("only pages of the changed font are copied");
        _world.Disposed.Count.ShouldBe(2);
        _world.Disposed.ShouldContain(p => p.Replace(Path.DirectorySeparatorChar, '/').EndsWith("FontCache/Arial.fnt"));
        _world.Disposed.ShouldContain(p => p.Replace(Path.DirectorySeparatorChar, '/').EndsWith("FontCache/Arial_0.png"));
        _world.Reloads.ShouldBe(1);
    }

    [Fact]
    public void Font_ChangedTwiceInOneBurst_IsCopiedAndEvictedOnceMoreThanOnceHarmlessly()
    {
        string bin = Path.Combine(_world.Dir, "bin") + "/";
        Directory.CreateDirectory(bin);
        FileManager.RelativeDirectory = bin;
        _world.Boot(Reader);
        string cache = Path.Combine(_world.Dir, "FontCache");
        Directory.CreateDirectory(cache);
        File.WriteAllText(Path.Combine(cache, "Arial.fnt"), "info");

        _world.Notify(Path.Combine(cache, "Arial.fnt"));
        _world.Advance(50);
        _world.Notify(Path.Combine(cache, "Arial.fnt"));
        _world.Advance(300);
        _world.Update();

        _world.Reloads.ShouldBe(1);
        File.Exists(Path.Combine(bin, "FontCache", "Arial.fnt")).ShouldBeTrue();
    }

    [Fact]
    public void Font_FntChangedButNoSourceFontCacheFolder_ReloadsWithoutThrowing()
    {
        _world.Boot(Reader);

        _world.NotifyAndSettle(Path.Combine(_world.Dir, "FontCache", "Missing.fnt"));

        _world.Reloads.ShouldBe(1);
        _world.Disposed.ShouldBeEmpty();
    }

    [Fact]
    public void Texture_PngChanged_DoesNotTriggerAReload()
    {
        _world.Boot(Reader);

        _world.NotifyAndSettle(Path.Combine(_world.Dir, "Content", "Hero.png"));

        _world.Reloads.ShouldBe(0);
    }

    [Fact]
    public void Animation_GanxEditedBetweenReloads_TheSecondReloadHoldsTheNewAnimation()
    {
        _world.Boot(Reader);
        string ganx = Path.Combine(_world.Dir, "Screens", "MainScreenAnimations.ganx");
        WriteAnimation(ganx, "Fade");
        _world.NotifyAndSettle(ganx);
        ObjectFinder.Self.GumProjectSave!.ElementAnimations.Single().Animations.Single().Name.ShouldBe("Fade");

        WriteAnimation(ganx, "Slide");
        _world.NotifyAndSettle(ganx);

        ObjectFinder.Self.GumProjectSave!.ElementAnimations.Single().Animations.Single().Name.ShouldBe("Slide");
    }

    [Fact]
    public void Animation_GanxDeleted_NextReloadDropsTheAnimation()
    {
        _world.Boot(Reader);
        string ganx = Path.Combine(_world.Dir, "Screens", "MainScreenAnimations.ganx");
        WriteAnimation(ganx, "Fade");
        _world.NotifyAndSettle(ganx);
        ObjectFinder.Self.GumProjectSave!.ElementAnimations.Count.ShouldBe(1, "sanity");

        File.Delete(ganx);
        _world.NotifyAndSettle(_world.ScreenFile);

        ObjectFinder.Self.GumProjectSave!.ElementAnimations.ShouldBeEmpty();
    }

    private static void WriteAnimation(string path, string name)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        ElementAnimationsSave save = new ElementAnimationsSave { ElementName = "MainScreen" };
        save.Animations.Add(new AnimationSave { Name = name });
        XmlSerializer serializer = FileManager.GetXmlSerializer(typeof(ElementAnimationsSave));
        using FileStream stream = File.Create(path);
        serializer.Serialize(stream, save);
    }
}
