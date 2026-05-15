using Gum.GueDeriving;
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
        Action act = () => RenderableRegistry.RegisterDefaultFactory<ICapabilityA>(null!);

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
        Action act = () => RenderableRegistry.RegisterFactory<ICapabilityA>(null!);

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
