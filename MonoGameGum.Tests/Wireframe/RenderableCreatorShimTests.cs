using Shouldly;
using System;
using System.Reflection;
using Xunit;

namespace MonoGameGum.Tests.Wireframe;

// RenderableCreator moved from MonoGameGum.Renderables to Gum.Renderables. The old name forwards
// to the real one and stays [Obsolete] until at least November 2027.
public class RenderableCreatorShimTests
{
#pragma warning disable CS0618 // The shim is the subject under test.
    [Fact]
    public void LegacyRenderableCreator_ShouldBeObsolete()
    {
        typeof(MonoGameGum.Renderables.RenderableCreator)
            .GetCustomAttribute<ObsoleteAttribute>(inherit: false).ShouldNotBeNull();
    }

    [Fact]
    public void LegacyRenderableCreator_ShouldForwardToGumRenderableCreator()
    {
        object? fromGum = Gum.Renderables.RenderableCreator.HandleCreateGraphicalComponent("NotAStandardType", null!);
        object? fromLegacy = MonoGameGum.Renderables.RenderableCreator.HandleCreateGraphicalComponent("NotAStandardType", null!);

        fromGum.ShouldBeNull();
        fromLegacy.ShouldBeNull();
    }
#pragma warning restore CS0618

    [Fact]
    public void AddChildForwarder_ShouldBeObsolete()
    {
        MethodInfo? method = typeof(MonoGameGum.GraphicalUiElementExtensionMethods).GetMethod("AddChild");

        method.ShouldNotBeNull();
        method.GetCustomAttribute<ObsoleteAttribute>(inherit: false).ShouldNotBeNull();
    }
}
