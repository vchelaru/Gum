#pragma warning disable CS0618
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.StateAnimation.SaveClasses;
using GumRuntime;
using Shouldly;
using Gum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracyWorld;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// Preview accuracy sweep, round 3: saves the way the tool orders them (accumulating edits, partial
/// notifications, an old file restored, files arriving between two reloads), the state Preview applies on
/// top of the shown element, exposed variables, state animation files, a Forms scroll bar, and what the
/// OS watcher reports. The hot tree is compared with a restart of the saved folder and the reload's
/// <see cref="HotReloadResult"/> is checked against it.
/// </summary>
public partial class PreviewAccuracy3Tests : BaseTestClass
{
    private readonly List<PreviewAccuracy3World> _worlds = new List<PreviewAccuracy3World>();

    public override void Dispose()
    {
        foreach (PreviewAccuracy3World world in _worlds)
        {
            world.Dispose();
        }
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private PreviewAccuracy3World Boot(string shown = "MainScreen", PreviewSelectionMessage? selection = null, Action<GumProjectSave>? edit = null)
    {
        PreviewAccuracy3World world = new PreviewAccuracy3World { Selection = selection };
        _worlds.Add(world);
        GumProjectSave project = Baseline();
        edit?.Invoke(project);
        world.Boot(project, shown);
        return world;
    }

    private static void ShouldBeCorrect(PreviewVerdict2 verdict) =>
        (verdict.Differs || !verdict.MatchesRestart).ShouldBeFalse(verdict.Explain());

    /// <summary>The tree may differ from a restart only when the result says so (Preview then rebuilds).</summary>
    private static void ShouldNotBeStaleSilently(PreviewVerdict2 verdict) => verdict.IsFalseNegative.ShouldBeFalse(verdict.Explain());

    private static ScreenSave Main(GumProjectSave project) => MainScreen(project);

    private static string Rel(PreviewAccuracy3World world, string relative) => Path.Combine(world.Dir, relative);

    // ---- Saves as the tool orders them ----

    [Fact]
    public void Control_EveryFileTouched_NothingEdited()
    {
        ShouldBeCorrect(Boot().Touch());
    }

    [Fact]
    public void Saves_TwoEditsInARow_EachMatchesARestart()
    {
        PreviewAccuracy3World world = Boot();

        ShouldBeCorrect(world.Save(project => Std(Main(project), "Box", "Red", 99)));
        ShouldBeCorrect(world.Save(project => Std(Main(project), "Label", "Text", "Bye")));
    }

    [Fact]
    public void Saves_InstanceAddedThenRenamedInTheNextSave()
    {
        PreviewAccuracy3World world = Boot();

        ShouldBeCorrect(world.Save(project => AddInstance(Main(project), "Extra", "ColoredRectangle")));
        ShouldBeCorrect(world.Save(project => Main(project).Instances.Single(instance => instance.Name == "Extra").Name = "Renamed"));
    }

    [Fact]
    public void UndoRestoresTheOldElementFile_OnlyThatFileIsNotified()
    {
        PreviewAccuracy3World world = Boot();
        string path = Rel(world, "Screens/MainScreen.gusx");
        byte[] old = File.ReadAllBytes(path);
        ShouldBeCorrect(world.Save(project => Std(Main(project), "Box", "Red", 99)));

        File.WriteAllBytes(path, old);
        PreviewVerdict2 verdict = world.Reload(path);

        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void UndoRestoresTheOldFiles_AfterAnInstanceWasAdded()
    {
        PreviewAccuracy3World world = Boot();
        string path = Rel(world, "Screens/MainScreen.gusx");
        byte[] old = File.ReadAllBytes(path);
        ShouldBeCorrect(world.Save(project => AddInstance(Main(project), "Extra", "ColoredRectangle")));

        File.WriteAllBytes(path, old);

        ShouldBeCorrect(world.Reload(path));
    }

    [Fact]
    public void ElementEdited_OnlyTheGumxIsNotified()
    {
        PreviewVerdict2 verdict = Boot().Save(project => Std(Main(project), "Box", "Red", 99), notify: file => file.EndsWith(".gumx"));

        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void ElementEdited_OnlyTheElementFileIsNotified()
    {
        PreviewVerdict2 verdict = Boot().Save(project => Std(Main(project), "Box", "Red", 99), notify: file => file.EndsWith(".gusx"));

        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void NewComponentUsedByTheScreen_ComponentAndScreenFilesNotified_GumxNot()
    {
        PreviewVerdict2 verdict = Boot().Save(project =>
        {
            ComponentSave added = NewComponent("Widgets/Added");
            AddInstance(added, "Dot", "ColoredRectangle");
            project.Components.Add(added);
            AddInstance(Main(project), "AddedInstance", "Widgets/Added");
        }, notify: file => !file.EndsWith(".gumx"));

        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void NewComponentUsedByTheScreen_ElementFilesSavedBeforeTheGumx_TwoReloads()
    {
        PreviewAccuracy3World world = Boot();
        Dictionary<string, string> before = world.Snapshot();
        GumProjectSave project = world.LoadSaved();
        ComponentSave added = NewComponent("Widgets/Added");
        AddInstance(added, "Dot", "ColoredRectangle");
        project.Components.Add(added);
        AddInstance(Main(project), "AddedInstance", "Widgets/Added");
        SaveAllKeepingGumx(world, project, out byte[] oldGumx);
        string[] changed = world.ChangedSince(before);

        // The tool writes the element files first and the .gumx last; the watcher reports them in separate bursts.
        PreviewVerdict2 elementsOnly = world.Reload(changed.Where(file => !file.EndsWith(".gumx")).ToArray());
        PreviewVerdict2 gumx = world.Reload(changed.Where(file => file.EndsWith(".gumx")).ToArray());

        ShouldBeCorrect(gumx);
        elementsOnly.Hot.ShouldNotBeNull();
    }

    [Fact]
    public void NewComponentUsedByTheScreen_GumxSavedBeforeTheElementFiles_TwoReloads()
    {
        PreviewAccuracy3World world = Boot();
        Dictionary<string, string> before = world.Snapshot();
        GumProjectSave project = world.LoadSaved();
        ComponentSave added = NewComponent("Widgets/Added");
        AddInstance(added, "Dot", "ColoredRectangle");
        project.Components.Add(added);
        AddInstance(Main(project), "AddedInstance", "Widgets/Added");
        SaveAllKeepingGumx(world, project, out _);
        string[] changed = world.ChangedSince(before);

        world.Reload(changed.Where(file => file.EndsWith(".gumx")).ToArray());
        PreviewVerdict2 elements = world.Reload(changed.Where(file => !file.EndsWith(".gumx")).ToArray());

        ShouldBeCorrect(elements);
    }

    private static void SaveAllKeepingGumx(PreviewAccuracy3World world, GumProjectSave project, out byte[] oldGumx)
    {
        oldGumx = File.ReadAllBytes(world.Gumx);
        foreach (ElementSave element in project.AllElements)
        {
            element.Instances.RemoveAll(instance => instance.DefinedByBase);
        }
        project.Initialize();
        PreviewAccuracy2World.SaveAll(project, world.Gumx);
    }

    // ---- The state Preview applies on top of the shown element ----

    private static PreviewSelectionMessage Highlighted() =>
        new PreviewSelectionMessage("Widgets/Widget") { CategoryName = "Look", StateName = "Highlighted" };

    private static StateSave LookState(GumProjectSave project, string name) =>
        Component(project, "Widgets/Widget").Categories.Single(category => category.Name == "Look").States.Single(state => state.Name == name);

    [Fact]
    public void SelectedState_Control_NothingEdited()
    {
        ShouldBeCorrect(Boot("Widgets/Widget", Highlighted()).Touch());
    }

    [Fact]
    public void SelectedState_ItsValueEdited()
    {
        PreviewVerdict2 verdict = Boot("Widgets/Widget", Highlighted()).Save(project => Set(LookState(project, "Highlighted"), "Fill.Red", "int", 77));

        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void SelectedState_TheDefaultOfTheSameVariableEdited()
    {
        PreviewVerdict2 verdict = Boot("Widgets/Widget", Highlighted()).Save(project => Std(Component(project, "Widgets/Widget"), "Fill", "Red", 5));

        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void SelectedState_AnotherVariableIsAddedToIt()
    {
        PreviewVerdict2 verdict = Boot("Widgets/Widget", Highlighted()).Save(project => Set(LookState(project, "Highlighted"), "Fill.Green", "int", 120));

        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void SelectedState_TheBaseComponentIsEdited()
    {
        PreviewVerdict2 verdict = Boot("Widgets/Widget", Highlighted()).Save(project => Set(Component(project, "Widgets/Base"), "X", "float", 33f));

        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void SelectedState_AnotherStateOfTheCategoryEdited()
    {
        PreviewVerdict2 verdict = Boot("Widgets/Widget", Highlighted()).Save(project => Set(LookState(project, "Normal"), "Fill.Red", "int", 200));

        ShouldBeCorrect(verdict);
    }

    // ---- Exposed variables ----

    private static void Expose(GumProjectSave project, string exposedName)
    {
        ComponentSave widget = Component(project, "Widgets/Widget");
        widget.DefaultState!.Variables.RemoveAll(variable => variable.Name == "Fill.Red");
        widget.DefaultState.Variables.Add(new VariableSave { Name = "Fill.Red", Type = "int", Value = 30, SetsValue = true, ExposedAsName = exposedName });
    }

    [Fact]
    public void ExposedVariable_AddedAndSetOnTheScreenInstance()
    {
        PreviewVerdict2 verdict = Boot().Save(project =>
        {
            Expose(project, "FillRed");
            Set(Main(project), "Widget1.FillRed", "int", 150);
        });

        ShouldNotBeStaleSilently(verdict);
    }

    [Fact]
    public void ExposedVariable_RemovedWhileTheInstanceStillSetsIt()
    {
        PreviewAccuracy3World world = Boot(edit: project =>
        {
            Expose(project, "FillRed");
            Set(Main(project), "Widget1.FillRed", "int", 150);
        });

        PreviewVerdict2 verdict = world.Save(project =>
        {
            ComponentSave widget = Component(project, "Widgets/Widget");
            widget.DefaultState!.Variables.Single(variable => variable.Name == "Fill.Red").ExposedAsName = null;
        });

        ShouldNotBeStaleSilently(verdict);
    }

    [Fact]
    public void ExposedVariable_Renamed()
    {
        PreviewAccuracy3World world = Boot(edit: project =>
        {
            Expose(project, "FillRed");
            Set(Main(project), "Widget1.FillRed", "int", 150);
        });

        PreviewVerdict2 verdict = world.Save(project =>
            Component(project, "Widgets/Widget").DefaultState!.Variables.Single(variable => variable.Name == "Fill.Red").ExposedAsName = "Tint");

        ShouldNotBeStaleSilently(verdict);
    }

    // ---- State animation files (.ganx) ----

    private static void WriteAnimations(string dir, string element, Action<ElementAnimationsSave> fill)
    {
        ElementAnimationsSave save = new ElementAnimationsSave { ElementName = element };
        fill(save);
        string path = Path.Combine(dir, "Screens", element + "Animations.ganx");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using FileStream stream = File.Create(path);
        FileManager.GetXmlSerializer(typeof(ElementAnimationsSave)).Serialize(stream, save);
    }

    private static AnimationSave Intro(params float[] times)
    {
        AnimationSave animation = new AnimationSave { Name = "Intro" };
        for (int i = 0; i < times.Length; i++)
        {
            animation.States.Add(new AnimatedStateSave { StateName = "Default", Time = times[i] });
        }
        return animation;
    }

    private PreviewAccuracy3World BootWithAnimation()
    {
        PreviewAccuracy3World world = new PreviewAccuracy3World();
        _worlds.Add(world);
        world.Boot(Baseline(), assets: dir => WriteAnimations(dir, "MainScreen", save => save.Animations.Add(Intro(0f, 1f))));
        return world;
    }

    [Fact]
    public void Animation_Control_NothingEdited()
    {
        ShouldBeCorrect(BootWithAnimation().Touch());
    }

    [Fact]
    public void Animation_StateAddedToTheFile()
    {
        PreviewAccuracy3World world = BootWithAnimation();
        WriteAnimations(world.Dir, "MainScreen", save => save.Animations.Add(Intro(0f, 1f, 2f)));

        ShouldBeCorrect(world.Reload(Rel(world, "Screens/MainScreenAnimations.ganx")));
    }

    [Fact]
    public void Animation_FileEditedAndProjectSavedInTheSameBurst()
    {
        PreviewAccuracy3World world = BootWithAnimation();
        PreviewVerdict2 verdict = world.Save(project => Std(Main(project), "Box", "Red", 99),
            assets: dir => WriteAnimations(dir, "MainScreen", save => save.Animations.Add(Intro(0f, 5f))));

        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Animation_FileDeleted()
    {
        PreviewAccuracy3World world = BootWithAnimation();
        File.Delete(Rel(world, "Screens/MainScreenAnimations.ganx"));

        ShouldBeCorrect(world.Reload(Rel(world, "Screens/MainScreenAnimations.ganx")));
    }

    [Fact]
    public void Animation_FileCreatedForAScreenThatHadNone()
    {
        PreviewAccuracy3World world = Boot();
        WriteAnimations(world.Dir, "MainScreen", save => save.Animations.Add(Intro(0f)));

        ShouldBeCorrect(world.Reload(Rel(world, "Screens/MainScreenAnimations.ganx")));
    }

    [Fact]
    public void Animation_EventAddedToTheFile()
    {
        PreviewAccuracy3World world = BootWithAnimation();
        WriteAnimations(world.Dir, "MainScreen", save =>
        {
            AnimationSave animation = Intro(0f, 1f);
            animation.Events.Add(new NamedEventSave { Name = "Ping", Time = 0.5f });
            save.Animations.Add(animation);
        });

        ShouldBeCorrect(world.Reload(Rel(world, "Screens/MainScreenAnimations.ganx")));
    }

    // ---- A Forms scroll bar built from the checked-in MVVM sample ----

    private PreviewAccuracy3World BootMvvmMainMenu()
    {
        string root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root, "Samples")))
        {
            root = Path.GetDirectoryName(root)!;
        }
        PreviewAccuracy3World world = new PreviewAccuracy3World();
        _worlds.Add(world);
        PreviewAccuracy3World.CopyFolder(Path.Combine(root, "Samples", "MVVM", "Content", "GumProject"), world.Dir);
        File.Move(Path.Combine(world.Dir, "GumProject.gumx"), world.Gumx);
        world.Boot(world.LoadSaved(), "MainMenu");
        return world;
    }

    [Fact]
    public void ScrollBar_NoOpSave_OnlyTheLayoutOfInvisibleElementsDiffers()
    {
        PreviewAccuracy3World world = BootMvvmMainMenu();
        world.IgnoreLayoutOfInvisible = true;

        PreviewVerdict2 verdict = world.Touch();

        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void ScrollBar_NoOpSave_EveryLineThatDiffersIsAnInvisibleElement()
    {
        PreviewAccuracy3World world = BootMvvmMainMenu();

        PreviewVerdict2 verdict = world.Touch();

        string[] hot = verdict.Hot.Split('\n');
        string[] fresh = verdict.Fresh.Split('\n');
        hot.Length.ShouldBe(fresh.Length);
        for (int i = 0; i < hot.Length; i++)
        {
            if (hot[i] != fresh[i])
            {
                hot[i].ShouldContain("AbsoluteVisible=False", Case.Sensitive, "a differing line must be an element nobody sees: " + hot[i]);
            }
        }
        verdict.MatchesRestart.ShouldBeTrue(verdict.Explain());
    }

    [Fact]
    public void ScrollBar_ListBoxItemTextEdited_MatchesARestart()
    {
        PreviewAccuracy3World world = BootMvvmMainMenu();
        world.IgnoreLayoutOfInvisible = true;

        PreviewVerdict2 verdict = world.Save(project =>
        {
            ScreenSave screen = project.Screens.Single(candidate => candidate.Name == "MainMenu");
            InstanceSave label = screen.Instances.First(instance => instance.BaseType == "Elements/Label");
            VariableSave? text = screen.DefaultState!.Variables.FirstOrDefault(variable => variable.Name == label.Name + ".Text");
            if (text != null)
            {
                text.Value = "Edited";
            }
        });

        ShouldBeCorrect(verdict);
    }

    // ---- What the OS watcher reports ----

    private static int ReloadsDuring(string folder, TimeSpan duration, Action act)
    {
        int reloads = 0;
        GumHotReloadManager manager = new GumHotReloadManager(Gum.GumService.ApplyProjectTextureFilter, GumAnimationLoader.LoadAnimationsFromProvider, _ => { });
        manager.ReloadFinished += _ => reloads++;
        manager.Start(Path.Combine(folder, "Proj.gumx"));
        try
        {
            System.Threading.Thread.Sleep(300);
            act();
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (stopwatch.Elapsed < duration)
            {
                manager.Update(Array.Empty<Gum.Wireframe.GraphicalUiElement>());
                System.Threading.Thread.Sleep(20);
            }
        }
        finally
        {
            manager.Stop();
        }
        return reloads;
    }

    [Fact]
    public void Watcher_ReadingTheProjectFiles_RaisesNoReload()
    {
        PreviewAccuracy3World world = Boot();

        int reloads = ReloadsDuring(world.Dir, TimeSpan.FromSeconds(1.2), () =>
        {
            foreach (string file in Directory.EnumerateFiles(world.Dir, "*.*", SearchOption.AllDirectories))
            {
                File.ReadAllBytes(file);
            }
            GumProjectSave.Load(world.Gumx);
        });

        reloads.ShouldBe(0);
    }

    [Fact]
    public void Watcher_WritingAnElementFile_RaisesOneReload()
    {
        PreviewAccuracy3World world = Boot();

        int reloads = ReloadsDuring(world.Dir, TimeSpan.FromSeconds(2), () =>
        {
            string path = Rel(world, "Screens/MainScreen.gusx");
            File.WriteAllBytes(path, File.ReadAllBytes(path));
        });

        reloads.ShouldBe(1);
    }
}
