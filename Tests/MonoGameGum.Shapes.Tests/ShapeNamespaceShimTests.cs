using Shouldly;
using System;
using System.Reflection;

namespace MonoGameGum.Shapes.Tests;

// The Apos.Shapes types moved from MonoGameAndGum.* to Gum.Renderables / Gum.Content. The old
// names derive from or forward to the real types and stay [Obsolete] until at least November 2027.
#pragma warning disable CS0618 // The shims are the subject under test.
public class ShapeNamespaceShimTests
{
    [Fact]
    public void LegacyShapeRenderer_DerivesFromGumRenderer_AndSharesSelf()
    {
        typeof(MonoGameAndGum.Renderables.ShapeRenderer).BaseType.ShouldBe(typeof(Gum.Renderables.ShapeRenderer));

        MonoGameAndGum.Renderables.ShapeRenderer.Self.ShouldBeSameAs(Gum.Renderables.ShapeRenderer.Self);
    }

    [Theory]
    [InlineData(typeof(MonoGameAndGum.Renderables.Circle), typeof(Gum.Renderables.Circle))]
    [InlineData(typeof(MonoGameAndGum.Renderables.RoundedRectangle), typeof(Gum.Renderables.RoundedRectangle))]
    [InlineData(typeof(MonoGameAndGum.Renderables.RenderableShapeBase), typeof(Gum.Renderables.RenderableShapeBase))]
    public void LegacyRenderable_DerivesFromGumRenderable_AndIsObsolete(Type legacyType, Type gumType)
    {
        legacyType.BaseType.ShouldBe(gumType);
        legacyType.GetCustomAttribute<ObsoleteAttribute>(inherit: false).ShouldNotBeNull();
    }

    [Fact]
    public void LegacyCircle_CanBeUsedWhereGumCircleIsExpected()
    {
        Gum.Renderables.Circle circle = new MonoGameAndGum.Renderables.Circle { IsFilled = true };

        circle.IsFilled.ShouldBeTrue();
    }

    [Fact]
    public void LegacyShapeSvgLoader_ForwardsToGumLoader()
    {
        MonoGameAndGum.Content.ShapeSvgLoader.Load("").ShouldBeNull();
        Gum.Content.ShapeSvgLoader.Load("").ShouldBeNull();
    }
}
#pragma warning restore CS0618
