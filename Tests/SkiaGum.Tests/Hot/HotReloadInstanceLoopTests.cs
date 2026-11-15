using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Content;
using Shouldly;
using Xunit;

namespace SkiaGum.Tests.Hot;

/// <summary>
/// #7988, as reported on Skia: a screen showing two components that hold each other loads, and a reload
/// of the unchanged files from disk must stop at the loop like the load did. Each component counts its
/// builds and throws past a limit, so a regression fails the test instead of overflowing the stack.
/// </summary>
public class HotReloadInstanceLoopTests : BaseTestClass
{
    private const int MaxBuilds = 40;

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "SkiaHotReloadInstanceLoop_" + Path.GetRandomFileName());
    private readonly Action<string>? _previousReporter = ElementSaveExtensions.ReportPropertyAssignmentError;
    private readonly List<string> _errors = new List<string>();
    private int _builds;

    public HotReloadInstanceLoopTests()
    {
        Directory.CreateDirectory(_directory);
        ElementSaveExtensions.ReportPropertyAssignmentError = _errors.Add;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.ReportPropertyAssignmentError = _previousReporter;
        ElementSaveExtensions.Reset();
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { }
        base.Dispose();
    }

    private static StateSave NewDefaultState(ElementSave element)
    {
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        return state;
    }

    private void AddComponent(GumProjectSave project, string name, string instanceName, string instanceType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        NewDefaultState(component);
        component.Instances.Add(new InstanceSave { Name = instanceName, BaseType = instanceType, ParentContainer = component });
        project.Components.Add(component);
        project.ComponentReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Component });
        ElementSaveExtensions.RegisterGueInstantiation(name, () =>
        {
            _builds++;
            if (_builds > MaxBuilds)
            {
                throw new InvalidOperationException($"More than {MaxBuilds} components were built: the reload did not stop at the loop.");
            }
            return new GraphicalUiElement();
        });
    }

    private string SaveReportedProject()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        NewDefaultState(container);
        project.StandardElements.Add(container);
        project.StandardElementReferences.Add(new ElementReference { Name = "Container", ElementType = ElementType.Standard });
        AddComponent(project, "LoopA", "Inner", "LoopB");
        AddComponent(project, "LoopB", "Back", "LoopA");
        ScreenSave screen = new ScreenSave { Name = "Looped" };
        NewDefaultState(screen);
        screen.Instances.Add(new InstanceSave { Name = "First", BaseType = "LoopA", ParentContainer = screen });
        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = "Looped", ElementType = ElementType.Screen });

        string gumx = Path.Combine(_directory, "Proj.gumx");
        project.Save(gumx, saveElements: true);
        return gumx;
    }

    private static int Count(GraphicalUiElement element) =>
        1 + element.Children.Cast<GraphicalUiElement>().Sum(Count);

    [Fact]
    public void PerformReload_UnchangedProjectWithAnInstanceLoop_KeepsTheTreeAndReportsTheLoop()
    {
        string gumx = SaveReportedProject();
        GumProjectSave loaded = GumProjectSave.Load(gumx);
        loaded.Initialize();
        ObjectFinder.Self.GumProjectSave = loaded;
        GraphicalUiElement screen = loaded.Screens.Single().ToGraphicalUiElement(RenderingLibrary.SystemManagers.Default, addToManagers: false);
        int startupCount = Count(screen);
        _errors.Clear();
        _builds = 0;

        GumHotReloadManager manager = new GumHotReloadManager(
            applyProjectTextureFilter: _ => { },
            loadAnimationsFromProvider: GumAnimationLoader.LoadAnimationsFromProvider,
            disposeCachedAsset: path => LoaderManager.Self.Dispose(path));
        List<HotReloadResult> results = new List<HotReloadResult>();
        manager.ReloadFinished += results.Add;
        manager.Start(gumx);
        manager.Stop();

        manager.PerformReload(new[] { screen });
        manager.PerformReload(new[] { screen });

        Count(screen).ShouldBe(startupCount);
        startupCount.ShouldBe(3, "Looped > First (LoopA) > Inner (LoopB), Back skipped");
        results.Count.ShouldBe(2);
        foreach (HotReloadResult result in results)
        {
            HotReloadIssue issue = result.Issues.ShouldHaveSingleItem();
            issue.Kind.ShouldBe(HotReloadIssueKind.InstanceLoop);
            issue.ElementName.ShouldBe("LoopB");
            issue.InstanceName.ShouldBe("Back");
            result.MatchesRestart.ShouldBeTrue();
        }
    }
}
