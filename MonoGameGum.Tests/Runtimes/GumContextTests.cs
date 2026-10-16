using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using Moq;
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

    private readonly IGumService? _originalDefault = IGumService.Default;

    // A live context owned by a stand-in service, so IGumService.Default can point at it.
    private GumContext CreateLiveContext()
    {
        GumContext context = new GumContext();
        context.ServiceTypeName = "TestService";
        Mock<IGumService> service = new Mock<IGumService>();
        service.SetupGet(item => item.Context).Returns(context);
        context.Service = service.Object;
        context.Register();
        _contexts.Add(context);
        return context;
    }

    // Assigning the default is how a service becomes current.
    private static void MakeDefault(GumContext context)
    {
        IGumService.Default = context.Service;
    }

    public override void Dispose()
    {
        foreach (GumContext context in _contexts)
        {
            context.Release();
        }
        IGumService.Default = _originalDefault;
        ElementSaveExtensions.Reset();
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = _originalCreateComponent;
        ElementSaveExtensions.CustomEvaluateExpression = _originalEvaluate;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = _originalEvaluateAllBranches;
        FileManager.RelativeDirectory = _originalRelativeDirectory;
        base.Dispose();
    }

    #region Current is the context of IGumService.Default

    [Fact]
    public void Current_ShouldBeNull_WhenNoContextIsLive()
    {
        IGumService.Default = null;

        GumContext.Current.ShouldBeNull();
    }

    [Fact]
    public void Current_ShouldBeTheDefaultsContext()
    {
        CreateLiveContext();
        GumContext second = CreateLiveContext();

        MakeDefault(second);

        GumContext.Current.ShouldBeSameAs(second);
    }

    [Fact]
    public void Current_ShouldFollowEachAssignmentOfTheDefault_WithoutRestoringAnything()
    {
        GumContext first = CreateLiveContext();
        GumContext second = CreateLiveContext();

        MakeDefault(second);
        MakeDefault(first);
        MakeDefault(second);

        GumContext.Current.ShouldBeSameAs(second);
    }

    [Fact]
    public void Current_ShouldBeTheOnlyLiveContext_WhenNoDefaultIsAssigned()
    {
        GumContext only = CreateLiveContext();
        IGumService.Default = null;

        GumContext.Current.ShouldBeSameAs(only);
    }

    [Fact]
    public void Current_ShouldThrow_WhenTwoContextsAreLiveAndNoDefaultIsAssigned()
    {
        GumContext first = CreateLiveContext();
        CreateLiveContext();
        IGumService.Default = null;

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => GumContext.Current);

        exception.Message.ShouldContain("IGumService.Default");
    }

    [Fact]
    public void Current_ShouldNotThrow_WhenTwoContextsAreLiveAndADefaultIsAssigned()
    {
        CreateLiveContext();
        GumContext second = CreateLiveContext();
        MakeDefault(second);

        Should.NotThrow(() => GumContext.Current);
    }

    [Fact]
    public void Current_ShouldBeTheRemainingContext_WhenOneOfTwoIsReleasedAndNoDefaultIsAssigned()
    {
        GumContext first = CreateLiveContext();
        GumContext second = CreateLiveContext();
        first.Release();
        IGumService.Default = null;

        GumContext.Current.ShouldBeSameAs(second);
    }

    [Fact]
    public void Current_ShouldBeNull_WhenTheDefaultHasNoContext()
    {
        CreateLiveContext();
        CreateLiveContext();
        IGumService.Default = new Mock<IGumService>().Object;

        GumContext.Current.ShouldBeNull();
    }

    #endregion

    // Pins the threading model: the active context is one plain static (all UI work runs on one thread),
    // so the default, and the current context, are visible from any thread.
    [Fact]
    public async Task Current_ShouldBeVisibleToWorkRunOnAnotherThread()
    {
        CreateLiveContext();
        GumContext second = CreateLiveContext();

        MakeDefault(second);
        GumContext? seenOnAnotherThread = await Task.Run(() => GumContext.Current);

        seenOnAnotherThread.ShouldBeSameAs(second);
    }

    // With a single-threaded synchronization context, an await resumes on that thread and the default is unchanged.
    [Fact]
    public void Current_ShouldStayTheSameAcrossAwaitOnTheSameSynchronizationContext()
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
                MakeDefault(second);
                await Task.Delay(10);
                afterAwait = GumContext.Current;
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

        MakeDefault(first);
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
        MakeDefault(second);
        {
            secondFinder = ObjectFinder.Self;
            secondFinder.GumProjectSave = new GumProjectSave();
        }

        ObjectFinder thirdFinder;
        MakeDefault(third);
        {
            thirdFinder = ObjectFinder.Self;
        }

        secondFinder.ShouldNotBeSameAs(processWide);
        thirdFinder.ShouldNotBeSameAs(secondFinder);
        thirdFinder.GumProjectSave.ShouldBeNull();
        processWide.GumProjectSave.ShouldBeNull();
    }

    [Fact]
    public void FileManagerRelativeDirectory_ShouldBeOwnedByEachNonAdoptingContext()
    {
        GumContext first = CreateLiveContext();
        GumContext second = CreateLiveContext();

        MakeDefault(first);
        {
            FileManager.RelativeDirectory = "/projectA/";
        }
        MakeDefault(second);
        {
            FileManager.RelativeDirectory = "/projectB/";
        }

        string underFirst;
        MakeDefault(first);
        {
            underFirst = FileManager.RelativeDirectory;
        }
        string underSecond;
        MakeDefault(second);
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

        MakeDefault(first);
        {
            ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromFirst" });
        }

        MakeDefault(first);
        {
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldBe("FromFirst");
        }
        MakeDefault(second);
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

        MakeDefault(first);
        {
            ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromFirst" });
        }
        MakeDefault(second);
        {
            ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromSecond" });
        }

        MakeDefault(first);
        {
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldBe("FromFirst");
        }
        MakeDefault(second);
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

        MakeDefault(first);
        {
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldBe("FromBase");
        }
        MakeDefault(second);
        {
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldBe("FromBase");
        }
    }

    [Fact]
    public void RegisterGueInstantiation_ShouldPreferTheServicesOwnLayer_OverTheBaseLayer()
    {
        GumContext first = CreateLiveContext();
        ComponentSave component = new ComponentSave { Name = "Widget" };
        MakeDefault(first);
        ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "Own" });

        // Registered afterward into the shared base layer, like an optional-package hook.
        GumContext.RegisteringShared = true;
        try
        {
            ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "Base" });
        }
        finally
        {
            GumContext.RegisteringShared = false;
        }

        ElementSaveExtensions.CreateGueForElement(component).Name.ShouldBe("Own", "the service's own layer is checked before the base");
    }

    [Fact]
    public void RegisterGueInstantiationType_ShouldNotOverwrite_WhenAnyLayerAlreadyRegisteredTheName()
    {
        GumContext first = CreateLiveContext();
        ComponentSave component = new ComponentSave { Name = "Widget" };
        ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromBase" });

        MakeDefault(first);
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
        MakeDefault(first);
        {
            ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromFirst" });
        }
        MakeDefault(second);
        {
            ElementSaveExtensions.RegisterGueInstantiation("Widget", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromSecond" });
        }

        MakeDefault(first);
        {
            ElementSaveExtensions.ClearRegistrations();
        }

        MakeDefault(first);
        {
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldNotBe("FromFirst");
            ElementSaveExtensions.CreateGueForElement(new ComponentSave { Name = "Shared" }).Name.ShouldBe("FromBase");
        }
        MakeDefault(second);
        {
            ElementSaveExtensions.CreateGueForElement(component).Name.ShouldBe("FromSecond");
        }
    }

    [Fact]
    public void ClearRegistrations_ShouldAlsoClearBaseLayer_WhenNoOtherContextIsLive()
    {
        GumContext only = CreateLiveContext();
        ElementSaveExtensions.RegisterGueInstantiation("Shared", () => new GraphicalUiElement(new InvisibleRenderable()) { Name = "FromBase" });

        MakeDefault(only);
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

        MakeDefault(first);
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
