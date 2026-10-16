using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Prototype coverage for #6066: per-service state owned by GumContext. No graphics device needed, so
// contexts are registered by hand instead of through GumService.Initialize.
public class GumContextTests : BaseTestClass
{
    private readonly List<GumContext> _contexts = new List<GumContext>();
    private readonly string _originalRelativeDirectory = FileManager.RelativeDirectory;

    // ClearRegistrations also nulls these process-wide hooks that other tests rely on.
    private readonly Func<string, ISystemManagers, IRenderable?>? _originalCreateComponent =
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc;
    private readonly Func<StateSave, string, string?, GraphicalUiElement?, object?>? _originalEvaluate =
        ElementSaveExtensions.CustomEvaluateExpression;
    private readonly Func<StateSave, string, string?, GraphicalUiElement?, IEnumerable<object>>? _originalEvaluateAllBranches =
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches;

    private GumContext CreateLiveContext()
    {
        GumContext context = new GumContext();
        context.Register();
        _contexts.Add(context);
        return context;
    }

    public override void Dispose()
    {
        foreach (GumContext context in _contexts)
        {
            context.Release();
        }
        ElementSaveExtensions.Reset();
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = _originalCreateComponent;
        ElementSaveExtensions.CustomEvaluateExpression = _originalEvaluate;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = _originalEvaluateAllBranches;
        FileManager.RelativeDirectory = _originalRelativeDirectory;
        base.Dispose();
    }

    #region Current and Activate

    [Fact]
    public void Current_ShouldBeNull_WhenNoContextIsLive()
    {
        GumContext.Current.ShouldBeNull();
    }

    [Fact]
    public void Current_ShouldBeFirstLiveContext_WhenNothingIsActivated()
    {
        GumContext first = CreateLiveContext();
        CreateLiveContext();

        GumContext.Current.ShouldBeSameAs(first);
    }

    [Fact]
    public void Current_ShouldBeNextLiveContext_WhenFirstIsReleased()
    {
        GumContext first = CreateLiveContext();
        GumContext second = CreateLiveContext();

        first.Release();

        GumContext.Current.ShouldBeSameAs(second);
    }

    [Fact]
    public void Activate_ShouldMakeContextCurrent_UntilDisposed()
    {
        GumContext first = CreateLiveContext();
        GumContext second = CreateLiveContext();

        using (second.Activate())
        {
            GumContext.Current.ShouldBeSameAs(second);
        }

        GumContext.Current.ShouldBeSameAs(first);
    }

    [Fact]
    public void Activate_ShouldRestorePreviousContext_WhenNested()
    {
        GumContext first = CreateLiveContext();
        GumContext second = CreateLiveContext();

        using (second.Activate())
        {
            using (first.Activate())
            {
                GumContext.Current.ShouldBeSameAs(first);
            }

            GumContext.Current.ShouldBeSameAs(second);
        }
    }

    // Pins the threading model: the active context is one plain static (all UI work runs on one thread),
    // so an open scope is visible from any thread.
    [Fact]
    public async Task Activate_ShouldBeVisibleToWorkRunOnAnotherThread()
    {
        CreateLiveContext();
        GumContext second = CreateLiveContext();

        GumContext? seenOnAnotherThread;
        using (second.Activate())
        {
            seenOnAnotherThread = await Task.Run(() => GumContext.Current);
        }

        seenOnAnotherThread.ShouldBeSameAs(second);
    }

    // With a single-threaded synchronization context, an await resumes on that thread and the scope is intact.
    [Fact]
    public void Activate_ShouldPersistAcrossAwaitOnTheSameSynchronizationContext()
    {
        CreateLiveContext();
        GumContext second = CreateLiveContext();
        GumContext? afterAwait = null;
        SynchronizationContext? original = SynchronizationContext.Current;
        SingleThreadContext single = new SingleThreadContext();
        SynchronizationContext.SetSynchronizationContext(single);
        try
        {
            async Task Work()
            {
                using (second.Activate())
                {
                    await Task.Delay(10);
                    afterAwait = GumContext.Current;
                }
            }

            Task task = Work();
            while (!task.IsCompleted)
            {
                single.RunPending();
                Thread.Sleep(1);
            }
            task.GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }

        afterAwait.ShouldBeSameAs(second);
    }

    // Runs posted continuations on whichever thread calls RunPending, like a game loop.
    private sealed class SingleThreadContext : SynchronizationContext
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<(SendOrPostCallback, object?)> _queue = new();

        public override void Post(SendOrPostCallback d, object? state) => _queue.Enqueue((d, state));

        public void RunPending()
        {
            while (_queue.TryDequeue(out (SendOrPostCallback Callback, object? State) item))
            {
                item.Callback(item.State);
            }
        }
    }

    [Fact]
    public void Activate_ShouldDoNothing_WhenContextIsNull()
    {
        GumContext first = CreateLiveContext();

        using (GumContext.Activate(null))
        {
            GumContext.Current.ShouldBeSameAs(first);
        }
    }

    #endregion

    #region Adopting the process statics

    [Fact]
    public void AdoptsProcessStatics_ShouldBeTrueOnlyForFirstLiveContext()
    {
        GumContext first = CreateLiveContext();
        GumContext second = CreateLiveContext();

        first.AdoptsProcessStatics.ShouldBeTrue();
        second.AdoptsProcessStatics.ShouldBeFalse();
    }

    [Fact]
    public void AdoptsProcessStatics_ShouldBeTrueAgain_WhenEveryContextWasReleased()
    {
        GumContext first = CreateLiveContext();
        first.Release();

        GumContext next = CreateLiveContext();

        next.AdoptsProcessStatics.ShouldBeTrue();
    }

    [Fact]
    public void ObjectFinderSelf_ShouldBeProcessWideInstance_ForTheAdoptingContext()
    {
        ObjectFinder processWide = ObjectFinder.Self;
        GumContext first = CreateLiveContext();

        using (first.Activate())
        {
            ObjectFinder.Self.ShouldBeSameAs(processWide);
        }
    }

    [Fact]
    public void ObjectFinderSelf_ShouldBeOwnedByEachNonAdoptingContext()
    {
        ObjectFinder processWide = ObjectFinder.Self;
        CreateLiveContext();
        GumContext second = CreateLiveContext();
        GumContext third = CreateLiveContext();

        ObjectFinder secondFinder;
        using (second.Activate())
        {
            secondFinder = ObjectFinder.Self;
            secondFinder.GumProjectSave = new GumProjectSave();
        }

        ObjectFinder thirdFinder;
        using (third.Activate())
        {
            thirdFinder = ObjectFinder.Self;
        }

        secondFinder.ShouldNotBeSameAs(processWide);
        thirdFinder.ShouldNotBeSameAs(secondFinder);
        thirdFinder.GumProjectSave.ShouldBeNull();
        processWide.GumProjectSave.ShouldBeNull();
    }

    [Fact]
    public void LoaderManagerSelf_ShouldBeOwnedByEachNonAdoptingContext()
    {
        LoaderManager processWide = LoaderManager.Self;
        GumContext first = CreateLiveContext();
        GumContext second = CreateLiveContext();

        LoaderManager firstManager;
        using (first.Activate())
        {
            firstManager = LoaderManager.Self;
        }

        LoaderManager secondManager;
        using (second.Activate())
        {
            secondManager = LoaderManager.Self;
        }

        firstManager.ShouldBeSameAs(processWide);
        secondManager.ShouldNotBeSameAs(processWide);
        LoaderManager.Of(second).ShouldBeSameAs(secondManager);
    }

    [Fact]
    public void LoaderManagerDisposeAndClear_ShouldNotClearAnotherContextsCache()
    {
        GumContext first = CreateLiveContext();
        GumContext second = CreateLiveContext();
        TestDisposable firstItem = new TestDisposable();
        TestDisposable secondItem = new TestDisposable();
        LoaderManager.Of(first).AddDisposable("item", firstItem);
        LoaderManager.Of(second).AddDisposable("item", secondItem);

        LoaderManager.Of(second).DisposeAndClear();

        secondItem.IsDisposed.ShouldBeTrue();
        firstItem.IsDisposed.ShouldBeFalse();
        LoaderManager.Of(first).GetDisposable("item").ShouldBeSameAs(firstItem);

        LoaderManager.Of(first).DisposeAndClear();
    }

    [Fact]
    public void FileManagerRelativeDirectory_ShouldBeOwnedByEachNonAdoptingContext()
    {
        GumContext first = CreateLiveContext();
        GumContext second = CreateLiveContext();

        using (first.Activate())
        {
            FileManager.RelativeDirectory = "/projectA/";
        }
        using (second.Activate())
        {
            FileManager.RelativeDirectory = "/projectB/";
        }

        string underFirst;
        using (first.Activate())
        {
            underFirst = FileManager.RelativeDirectory;
        }
        string underSecond;
        using (second.Activate())
        {
            underSecond = FileManager.RelativeDirectory;
        }

        underFirst.ShouldBe(new FilePath("/projectA/").FullPath, StringCompareShould.IgnoreCase);
        underSecond.ShouldBe(new FilePath("/projectB/").FullPath, StringCompareShould.IgnoreCase);
    }

    [Fact]
    public void FileManagerRelativeDirectory_ShouldUseProcessStatic_WhenNoContextIsLive()
    {
        FileManager.RelativeDirectory = "/noContext/";

        FileManager.RelativeDirectory.ShouldBe(new FilePath("/noContext/").FullPath, StringCompareShould.IgnoreCase);
    }

    #endregion

    #region Element type table

    [Fact]
    public void RegisterGueInstantiation_ShouldNotBeVisibleToAnotherContext_WhenRegisteredInOneScope()
    {
        GumContext first = CreateLiveContext();
        GumContext second = CreateLiveContext();
        ComponentSave component = new ComponentSave { Name = "Widget" };

        using (first.Activate())
        {
            ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromFirst" });
        }

        using (first.Activate())
        {
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldBe("FromFirst");
        }
        using (second.Activate())
        {
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldNotBe("FromFirst");
        }
    }

    [Fact]
    public void RegisterGueInstantiation_ShouldLetEachContextRegisterItsOwnTypeForTheSameName()
    {
        GumContext first = CreateLiveContext();
        GumContext second = CreateLiveContext();
        ComponentSave component = new ComponentSave { Name = "Widget" };

        using (first.Activate())
        {
            ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromFirst" });
        }
        using (second.Activate())
        {
            ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromSecond" });
        }

        using (first.Activate())
        {
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldBe("FromFirst");
        }
        using (second.Activate())
        {
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldBe("FromSecond");
        }
    }

    [Fact]
    public void RegisterGueInstantiation_ShouldBeVisibleToEveryContext_WhenRegisteredOutsideAnyScope()
    {
        // The process-wide base layer: what [ModuleInitializer]s and startup code write.
        ElementSaveExtensions.RegisterGueInstantiation("Shared", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromBase" });
        GumContext first = CreateLiveContext();
        GumContext second = CreateLiveContext();
        ComponentSave component = new ComponentSave { Name = "Shared" };

        using (first.Activate())
        {
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldBe("FromBase");
        }
        using (second.Activate())
        {
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldBe("FromBase");
        }
    }

    [Fact]
    public void RegisterGueInstantiation_ShouldPreferNewestRegistration_AcrossBaseAndContextLayers()
    {
        GumContext first = CreateLiveContext();
        ComponentSave component = new ComponentSave { Name = "Widget" };

        using (first.Activate())
        {
            ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "Older" });
        }
        // Registered later, with no service active, like user startup code that runs after Initialize.
        ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "Newer" });

        using (first.Activate())
        {
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldBe("Newer");
        }
    }

    [Fact]
    public void RegisterGueInstantiationType_ShouldNotOverwrite_WhenAnyLayerAlreadyRegisteredTheName()
    {
        GumContext first = CreateLiveContext();
        ComponentSave component = new ComponentSave { Name = "Widget" };
        ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromBase" });

        using (first.Activate())
        {
            ElementSaveExtensions.RegisterGueInstantiationType("Widget", typeof(GraphicalUiElement), overwriteIfAlreadyExists: false);
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldBe("FromBase");
        }
    }

    [Fact]
    public void ClearRegistrations_ShouldOnlyClearTheCurrentContext_WhenAnotherContextIsLive()
    {
        GumContext first = CreateLiveContext();
        GumContext second = CreateLiveContext();
        ComponentSave component = new ComponentSave { Name = "Widget" };
        ElementSaveExtensions.RegisterGueInstantiation("Shared", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromBase" });
        using (first.Activate())
        {
            ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromFirst" });
        }
        using (second.Activate())
        {
            ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromSecond" });
        }

        using (first.Activate())
        {
            ElementSaveExtensions.ClearRegistrations();
        }

        using (first.Activate())
        {
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldNotBe("FromFirst");
            ElementSaveExtensions.CreateGueForElement(new ComponentSave { Name = "Shared" }).Name.ShouldBe("FromBase");
        }
        using (second.Activate())
        {
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldBe("FromSecond");
        }
    }

    [Fact]
    public void ClearRegistrations_ShouldAlsoClearBaseLayer_WhenNoOtherContextIsLive()
    {
        GumContext only = CreateLiveContext();
        ElementSaveExtensions.RegisterGueInstantiation("Shared", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromBase" });

        using (only.Activate())
        {
            ElementSaveExtensions.ClearRegistrations();
            ElementSaveExtensions.CreateGueForElement(new ComponentSave { Name = "Shared" }).Name.ShouldNotBe("FromBase");
        }
    }

    [Fact]
    public void ClearRegistrations_ShouldKeepCustomCreateGraphicalComponentFunc_WhenAnotherContextIsLive()
    {
        GumContext first = CreateLiveContext();
        CreateLiveContext();
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = (_, _) => null;

        using (first.Activate())
        {
            ElementSaveExtensions.ClearRegistrations();
        }

        ElementSaveExtensions.CustomCreateGraphicalComponentFunc.ShouldNotBeNull();
    }

    #endregion

    #region Owner of an element

    [Fact]
    public void OwnerOf_ShouldBeNull_WhenElementWasNotBuiltByAService()
    {
        GraphicalUiElement element = new GraphicalUiElement(new InvisibleRenderable());

        GumContext.OwnerOf(element).ShouldBeNull();
    }

    [Fact]
    public void OwnerOf_ShouldBeBuildingContext_ForStampedElementAndItsDescendants()
    {
        GumContext first = CreateLiveContext();
        GraphicalUiElement root = new GraphicalUiElement(new InvisibleRenderable());
        GraphicalUiElement child = new GraphicalUiElement(new InvisibleRenderable());
        child.Parent = root;

        GumContext.StampBuiltBy(root, first);

        GumContext.OwnerOf(root).ShouldBeSameAs(first);
        GumContext.OwnerOf(child).ShouldBeSameAs(first);
    }

    #endregion

    private sealed class TestDisposable : System.IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }
}
