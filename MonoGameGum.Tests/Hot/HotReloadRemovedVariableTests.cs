using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// A variable the saved file used to set and no longer does, such as a <c>Box1.Parent</c> line
/// deleted by detaching the instance in the Gum tool. A reload only re-applies the variables the
/// new file has, so a removed one has to be reset explicitly or the live visual keeps the old
/// value. Unlike <see cref="HotReloadStructuralDiffTests"/>, which edits one project in place,
/// each test here builds the project twice, because that is what a real reload does: the new
/// project is a separate object read from disk and the old one is gone.
/// </summary>
public class HotReloadRemovedVariableTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    /// <summary>
    /// A screen with two Container instances. <paramref name="configure"/> adds whatever the file
    /// holds in the screen's default state, so each call is one version of the saved project.
    /// </summary>
    private static (GumProjectSave project, ScreenSave screen) BuildProject(Action<StateSave>? configure)
    {
        GumProjectSave project = new GumProjectSave();

        // A real project's standard elements record a default for every variable, and that record
        // is where a variable the file no longer sets gets its value back from.
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        StateSave containerDefault = new StateSave { Name = "Default", ParentContainer = container };
        SetVariable(containerDefault, "X", 0f, "float");
        SetVariable(containerDefault, "Width", 100f, "float");
        container.States.Add(containerDefault);
        project.StandardElements.Add(container);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        project.Screens.Add(screen);

        foreach (string name in new[] { "Holder1", "Box1" })
        {
            screen.Instances.Add(new InstanceSave { Name = name, BaseType = "Container", ParentContainer = screen });
        }

        configure?.Invoke(defaultState);
        return (project, screen);
    }

    private static void SetVariable(StateSave state, string name, object? value, string type)
    {
        state.Variables.Add(new VariableSave { Name = name, Value = value, Type = type, SetsValue = true });
    }

    private static GraphicalUiElement Find(GraphicalUiElement parent, string name)
    {
        return parent.ContainedElements.Concat(parent.Children).First(c => c.Name == name);
    }

    /// <summary>Reads the new project the way a reload does, then applies it to the live tree.</summary>
    private static void Reload(GraphicalUiElement root, GumProjectSave newProject)
    {
        ObjectFinder.Self.GumProjectSave = newProject;
        GumHotReloadManager.ApplyDiff(new[] { root }, newProject, SystemManagers.Default);
    }

    [Fact]
    public void PerformReload_FromFilesOnDisk_DetachesAnInstanceTheToolSavedWithAnEmptyParent()
    {
        // The real path the preview takes: the tool saves, the reload reads the files back. Detaching
        // in the tool does not delete the Parent line, it saves it with no value.
        string sourceDirectory = Path.Combine(
            Path.GetTempPath(), "HotReloadRemovedVariableTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDirectory);

        try
        {
            string gumxPath = Path.Combine(sourceDirectory, "Proj.gumx");
            SaveToDisk(BuildProject(state => SetVariable(state, "Box1.Parent", "Holder1", "string")).project, gumxPath);

            GumProjectSave loaded = GumProjectSave.Load(gumxPath);
            loaded.Initialize();
            ObjectFinder.Self.GumProjectSave = loaded;
            GraphicalUiElement screenGue = loaded.Screens.Single().ToGraphicalUiElement();
            GraphicalUiElement holder = Find(screenGue, "Holder1");
            GraphicalUiElement box1 = Find(screenGue, "Box1");
            box1.Parent.ShouldBe(holder, "sanity: the file attaches Box1 under Holder1");

            SaveToDisk(BuildProject(state => SetVariable(state, "Box1.Parent", null, "string")).project, gumxPath);
            GumHotReloadManager manager = new GumHotReloadManager(
                Gum.GumService.ApplyProjectTextureFilter, GumAnimationLoader.LoadAnimationsFromProvider,
                path => RenderingLibrary.Content.LoaderManager.Self.Dispose(path));
            manager.Start(gumxPath);
            // Start only records the source path; release the OS watcher right away.
            manager.Stop();

            manager.PerformReload(new[] { screenGue });

            box1.Parent.ShouldBe(screenGue);
        }
        finally
        {
            try { Directory.Delete(sourceDirectory, recursive: true); } catch { /* best-effort */ }
        }
    }

    private static void SaveToDisk(GumProjectSave project, string gumxPath)
    {
        foreach (ScreenSave screen in project.Screens)
        {
            project.ScreenReferences.Add(new ElementReference { Name = screen.Name, ElementType = ElementType.Screen });
        }
        foreach (StandardElementSave standard in project.StandardElements)
        {
            project.StandardElementReferences.Add(new ElementReference { Name = standard.Name, ElementType = ElementType.Standard });
        }
        project.Save(gumxPath, saveElements: true);
    }

    [Fact]
    public void EmptyParentVariable_DetachesTheInstanceFromItsParent()
    {
        // What the tool saves when an instance is dragged back out: Parent is still there, still
        // marked as set, with no value.
        (GumProjectSave before, ScreenSave beforeScreen) =
            BuildProject(state => SetVariable(state, "Box1.Parent", "Holder1", "string"));
        ObjectFinder.Self.GumProjectSave = before;
        GraphicalUiElement screenGue = beforeScreen.ToGraphicalUiElement();
        GraphicalUiElement holder = Find(screenGue, "Holder1");
        GraphicalUiElement box1 = Find(screenGue, "Box1");
        box1.Parent.ShouldBe(holder, "sanity: the first load attaches Box1 under Holder1");

        (GumProjectSave after, _) = BuildProject(state => SetVariable(state, "Box1.Parent", null, "string"));
        Reload(screenGue, after);

        box1.Parent.ShouldBe(screenGue);
        holder.Children.ShouldNotContain(box1);
    }

    [Fact]
    public void RemovedParentVariable_ReattachesTheInstanceToTheScreen()
    {
        (GumProjectSave before, ScreenSave beforeScreen) =
            BuildProject(state => SetVariable(state, "Box1.Parent", "Holder1", "string"));
        ObjectFinder.Self.GumProjectSave = before;
        GraphicalUiElement screenGue = beforeScreen.ToGraphicalUiElement();
        GraphicalUiElement holder = Find(screenGue, "Holder1");
        GraphicalUiElement box1 = Find(screenGue, "Box1");
        box1.Parent.ShouldBe(holder, "sanity: the first load attaches Box1 under Holder1");

        // Detaching Box1 in the tool deletes its Parent line from the saved file.
        (GumProjectSave after, _) = BuildProject(configure: null);
        Reload(screenGue, after);

        box1.Parent.ShouldBe(screenGue);
        holder.Children.ShouldNotContain(box1);
    }

    [Fact]
    public void RemovedPlainVariable_ResetsToTheStandardElementsDefault()
    {
        // Not specific to Parent: any variable the file stops setting has the same problem.
        (GumProjectSave before, ScreenSave beforeScreen) =
            BuildProject(state => SetVariable(state, "Box1.Width", 321f, "float"));
        ObjectFinder.Self.GumProjectSave = before;
        GraphicalUiElement screenGue = beforeScreen.ToGraphicalUiElement();
        GraphicalUiElement box1 = Find(screenGue, "Box1");
        box1.Width.ShouldBe(321f, "sanity: the first load applies the file's width");

        (GumProjectSave after, _) = BuildProject(configure: null);
        Reload(screenGue, after);

        box1.Width.ShouldBe(100f, "the standard Container's default");
    }

    [Fact]
    public void ValuesAssignedInCode_SurviveWhileOnlyTheFilesRemovedVariableResets()
    {
        // The file sets Box1.X and then stops. Code (a game, or the preview's interaction)
        // changed other things the file never mentions. Only the variable the file removed may
        // go back to its default; nothing the file never set may be touched.
        (GumProjectSave before, ScreenSave beforeScreen) =
            BuildProject(state => SetVariable(state, "Box1.X", 40f, "float"));
        ObjectFinder.Self.GumProjectSave = before;
        GraphicalUiElement screenGue = beforeScreen.ToGraphicalUiElement();
        GraphicalUiElement holder = Find(screenGue, "Holder1");
        GraphicalUiElement box1 = Find(screenGue, "Box1");
        box1.X.ShouldBe(40f, "sanity: the first load applies the file's X");

        box1.Y = 7f;
        box1.Width = 55f;
        box1.Parent = holder;

        (GumProjectSave after, _) = BuildProject(configure: null);
        Reload(screenGue, after);

        box1.X.ShouldBe(0f, "the file stopped setting X, so it is back to the standard Container's default");
        box1.Y.ShouldBe(7f, "the file never set Y");
        box1.Width.ShouldBe(55f, "the file never set Width");
        box1.Parent.ShouldBe(holder, "the file never set Parent, so the attachment made in code stays");
    }
}
