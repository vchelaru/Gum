using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class RenderableRegistryTests : BaseTestClass
{
    // RenderableRegistry.Reset() runs in BaseTestClass.Dispose so every test starts
    // with an empty registry without needing a per-class override here.

    [Fact]
    public void ClearFactory_ShouldBeNoOp_WhenNoActiveFactoryRegistered()
    {
        Action act = () => RenderableRegistry.ClearFactory<ICapabilityA>();

        act.ShouldNotThrow();
    }

    [Fact]
    public void ClearFactory_ShouldFallBackToDefault_WhenDefaultIsRegistered()
    {
        RenderableRegistry.RegisterDefaultFactory<ICapabilityA>(() => new CapabilityAImplementation("default"));
        RenderableRegistry.RegisterFactory<ICapabilityA>(() => new CapabilityAImplementation("override"));

        RenderableRegistry.ClearFactory<ICapabilityA>();

        ICapabilityA? created = RenderableRegistry.Create<ICapabilityA>();
        created.ShouldNotBeNull();
        created.Label.ShouldBe("default");
    }

    [Fact]
    public void ClearFactory_ShouldReturnNull_WhenNoDefaultRegistered()
    {
        RenderableRegistry.RegisterFactory<ICapabilityA>(() => new CapabilityAImplementation("active"));

        RenderableRegistry.ClearFactory<ICapabilityA>();

        RenderableRegistry.Create<ICapabilityA>().ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldInvokeActiveFactoryOverDefault_WhenBothRegistered()
    {
        RenderableRegistry.RegisterDefaultFactory<ICapabilityA>(() => new CapabilityAImplementation("default"));
        RenderableRegistry.RegisterFactory<ICapabilityA>(() => new CapabilityAImplementation("active"));

        ICapabilityA? created = RenderableRegistry.Create<ICapabilityA>();

        created.ShouldNotBeNull();
        created.Label.ShouldBe("active");
    }

    [Fact]
    public void Create_ShouldReturnInstanceFromDefaultFactory_WhenOnlyDefaultRegistered()
    {
        RenderableRegistry.RegisterDefaultFactory<ICapabilityA>(() => new CapabilityAImplementation("default"));

        ICapabilityA? created = RenderableRegistry.Create<ICapabilityA>();

        created.ShouldNotBeNull();
        created.Label.ShouldBe("default");
    }

    [Fact]
    public void Create_ShouldReturnInstanceFromFactory_WhenOnlyActiveRegistered()
    {
        RenderableRegistry.RegisterFactory<ICapabilityA>(() => new CapabilityAImplementation("active"));

        ICapabilityA? created = RenderableRegistry.Create<ICapabilityA>();

        created.ShouldNotBeNull();
        created.Label.ShouldBe("active");
    }

    [Fact]
    public void Create_ShouldReturnNull_WhenNoFactoryRegistered()
    {
        RenderableRegistry.Create<ICapabilityA>().ShouldBeNull();
    }

    [Fact]
    public void GetFactory_ShouldKeyByType_NotShareBetweenCapabilities()
    {
        RenderableRegistry.RegisterFactory<ICapabilityA>(() => new CapabilityAImplementation("a"));

        RenderableRegistry.GetFactory<ICapabilityA>().ShouldNotBeNull();
        RenderableRegistry.GetFactory<ICapabilityB>().ShouldBeNull();
    }

    [Fact]
    public void GetFactory_ShouldReturnNull_WhenNothingRegistered()
    {
        RenderableRegistry.GetFactory<ICapabilityA>().ShouldBeNull();
    }

    [Fact]
    public void RegisterDefaultFactory_ShouldReplacePreviousDefault()
    {
        RenderableRegistry.RegisterDefaultFactory<ICapabilityA>(() => new CapabilityAImplementation("first"));
        RenderableRegistry.RegisterDefaultFactory<ICapabilityA>(() => new CapabilityAImplementation("second"));

        ICapabilityA? created = RenderableRegistry.Create<ICapabilityA>();

        created.ShouldNotBeNull();
        created.Label.ShouldBe("second");
    }

    [Fact]
    public void RegisterDefaultFactory_ShouldThrow_WhenFactoryIsNull()
    {
        Action act = () => RenderableRegistry.RegisterDefaultFactory<ICapabilityA>((Func<ICapabilityA>)null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void RegisterFactory_ShouldReplacePreviousFactory()
    {
        RenderableRegistry.RegisterFactory<ICapabilityA>(() => new CapabilityAImplementation("first"));
        RenderableRegistry.RegisterFactory<ICapabilityA>(() => new CapabilityAImplementation("second"));

        ICapabilityA? created = RenderableRegistry.Create<ICapabilityA>();

        created.ShouldNotBeNull();
        created.Label.ShouldBe("second");
    }

    [Fact]
    public void RegisterFactory_ShouldThrow_WhenFactoryIsNull()
    {
        Action act = () => RenderableRegistry.RegisterFactory<ICapabilityA>((Func<ICapabilityA>)null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Reset_ShouldClearActiveAndDefaultFactories()
    {
        RenderableRegistry.RegisterDefaultFactory<ICapabilityA>(() => new CapabilityAImplementation("default"));
        RenderableRegistry.RegisterFactory<ICapabilityA>(() => new CapabilityAImplementation("active"));
        RenderableRegistry.RegisterFactory<ICapabilityB>(() => new CapabilityBImplementation());

        RenderableRegistry.Reset();

        RenderableRegistry.GetFactory<ICapabilityA>().ShouldBeNull();
        RenderableRegistry.GetFactory<ICapabilityB>().ShouldBeNull();
    }

    // -----------------------------------------------------------------------------------------
    // Context-bearing factory tests. The Circle / Apos.Shapes collapse needs the registered
    // factory to receive the calling GraphicalUiElement so the optional assembly can wire its
    // own internal hooks (OnPreRender) onto the renderable. Same registry, separate bucket.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ClearFactory_ShouldClearBothContextFreeAndContextBearingActives()
    {
        RenderableRegistry.RegisterFactory<ICapabilityA>(() => new CapabilityAImplementation("context-free"));
        RenderableRegistry.RegisterFactory<ICapabilityA>(_ => new CapabilityAImplementation("context-bearing"));

        RenderableRegistry.ClearFactory<ICapabilityA>();

        RenderableRegistry.GetFactory<ICapabilityA>().ShouldBeNull();
        RenderableRegistry.GetContextFactory<ICapabilityA>().ShouldBeNull();
    }

    [Fact]
    public void Create_WithContext_ShouldFallBackToContextFreeActive_WhenNoContextBearingRegistered()
    {
        RenderableRegistry.RegisterFactory<ICapabilityA>(() => new CapabilityAImplementation("context-free"));

        ContainerRuntime context = new();
        ICapabilityA? created = RenderableRegistry.Create<ICapabilityA>(context);

        created.ShouldNotBeNull();
        created.Label.ShouldBe("context-free");
    }

    [Fact]
    public void Create_WithContext_ShouldFallBackToDefaultContextBearing_WhenNoActiveRegistered()
    {
        RenderableRegistry.RegisterDefaultFactory<ICapabilityA>(gue => new CapabilityAImplementation("default-context-bearing"));

        ContainerRuntime context = new();
        ICapabilityA? created = RenderableRegistry.Create<ICapabilityA>(context);

        created.ShouldNotBeNull();
        created.Label.ShouldBe("default-context-bearing");
    }

    [Fact]
    public void Create_WithContext_ShouldInvokeContextBearingOverContextFree_WhenBothActivesRegistered()
    {
        RenderableRegistry.RegisterFactory<ICapabilityA>(() => new CapabilityAImplementation("context-free"));
        RenderableRegistry.RegisterFactory<ICapabilityA>(_ => new CapabilityAImplementation("context-bearing"));

        ContainerRuntime context = new();
        ICapabilityA? created = RenderableRegistry.Create<ICapabilityA>(context);

        created.ShouldNotBeNull();
        created.Label.ShouldBe("context-bearing");
    }

    [Fact]
    public void Create_WithContext_ShouldPassContextToFactory()
    {
        GraphicalUiElement? observed = null;
        RenderableRegistry.RegisterFactory<ICapabilityA>(gue =>
        {
            observed = gue;
            return new CapabilityAImplementation("ok");
        });

        ContainerRuntime context = new();
        RenderableRegistry.Create<ICapabilityA>(context);

        observed.ShouldBeSameAs(context);
    }

    [Fact]
    public void Create_WithContext_ShouldReturnNull_WhenNothingRegistered()
    {
        ContainerRuntime context = new();
        RenderableRegistry.Create<ICapabilityA>(context).ShouldBeNull();
    }

    [Fact]
    public void Create_WithoutContext_ShouldNotInvokeContextBearingFactory()
    {
        // Context-free Create<T>() must NOT auto-invoke a context-bearing factory: there is no
        // safe default GraphicalUiElement to pass. Callers that registered context-bearing must
        // use Create<T>(context). This keeps the two registration styles cleanly distinguished.
        RenderableRegistry.RegisterFactory<ICapabilityA>(_ => new CapabilityAImplementation("context-bearing"));

        RenderableRegistry.Create<ICapabilityA>().ShouldBeNull();
    }

    [Fact]
    public void GetContextFactory_ShouldReturnNull_WhenNothingRegistered()
    {
        RenderableRegistry.GetContextFactory<ICapabilityA>().ShouldBeNull();
    }

    [Fact]
    public void RegisterFactory_ContextBearing_ShouldNotClearContextFreeActive()
    {
        RenderableRegistry.RegisterFactory<ICapabilityA>(() => new CapabilityAImplementation("context-free"));
        RenderableRegistry.RegisterFactory<ICapabilityA>(_ => new CapabilityAImplementation("context-bearing"));

        // Context-free bucket is preserved — Create<T>() still finds it.
        ICapabilityA? createdWithoutContext = RenderableRegistry.Create<ICapabilityA>();
        createdWithoutContext.ShouldNotBeNull();
        createdWithoutContext.Label.ShouldBe("context-free");
    }

    [Fact]
    public void RegisterFactory_ContextBearing_ShouldReplacePreviousContextBearing()
    {
        RenderableRegistry.RegisterFactory<ICapabilityA>(_ => new CapabilityAImplementation("first"));
        RenderableRegistry.RegisterFactory<ICapabilityA>(_ => new CapabilityAImplementation("second"));

        ContainerRuntime context = new();
        ICapabilityA? created = RenderableRegistry.Create<ICapabilityA>(context);

        created.ShouldNotBeNull();
        created.Label.ShouldBe("second");
    }

    [Fact]
    public void RegisterFactory_ContextBearing_ShouldThrow_WhenFactoryIsNull()
    {
        Action act = () => RenderableRegistry.RegisterFactory<ICapabilityA>((Func<GraphicalUiElement, ICapabilityA>)null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void RegisterDefaultFactory_ContextBearing_ShouldThrow_WhenFactoryIsNull()
    {
        Action act = () => RenderableRegistry.RegisterDefaultFactory<ICapabilityA>((Func<GraphicalUiElement, ICapabilityA>)null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Reset_ShouldClearContextBearingFactories()
    {
        RenderableRegistry.RegisterFactory<ICapabilityA>(_ => new CapabilityAImplementation("active"));
        RenderableRegistry.RegisterDefaultFactory<ICapabilityA>(_ => new CapabilityAImplementation("default"));

        RenderableRegistry.Reset();

        RenderableRegistry.GetContextFactory<ICapabilityA>().ShouldBeNull();
    }

    // Sentinel capability contracts used purely for these tests; they intentionally do not
    // derive from any real renderable interface — RenderableRegistry keys by generic type only.
    private interface ICapabilityA
    {
        string Label { get; }
    }

    private interface ICapabilityB
    {
    }

    private sealed class CapabilityAImplementation : ICapabilityA
    {
        public CapabilityAImplementation(string label)
        {
            Label = label;
        }

        public string Label { get; }
    }

    private sealed class CapabilityBImplementation : ICapabilityB
    {
    }
}
