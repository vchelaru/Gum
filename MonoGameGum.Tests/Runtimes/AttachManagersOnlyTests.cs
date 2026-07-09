using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class AttachManagersOnlyTests : BaseTestClass
{
    [Fact]
    public void AttachManagersOnly_ResolvesEffectiveManagers_ButDoesNotRegisterRenderableForDrawing()
    {
        // AddRenderableToManagers is the hook that actually puts a renderable on a Layer
        // (see CustomSetPropertyOnRenderable.AddRenderableToManagers -> SpriteManager.Add ->
        // Layer.Add). Recording its calls lets us prove AttachManagersOnly never reaches it,
        // while AddToManagers still does.
        Action<IRenderableIpso, ISystemManagers, Layer>? originalHook = GraphicalUiElement.AddRenderableToManagers;
        List<object> registeredRenderables = new();
        GraphicalUiElement.AddRenderableToManagers = (renderable, managers, layer) => registeredRenderables.Add(renderable);

        try
        {
            SpriteRuntime attachOnlySprite = new();
            attachOnlySprite.AttachManagersOnly(SystemManagers.Default);

            SpriteRuntime addedSprite = new();
            addedSprite.AddToManagers(SystemManagers.Default);

            attachOnlySprite.EffectiveManagers.ShouldBe(SystemManagers.Default);

            registeredRenderables.ShouldNotContain(attachOnlySprite.RenderableComponent,
                "because AttachManagersOnly must not register the renderable, or Renderer.Draw would double-draw it " +
                "in hosts (like FlatRedBall2) that draw this element themselves through their own render pass.");
            registeredRenderables.ShouldContain(addedSprite.RenderableComponent,
                "because AddToManagers should still register the renderable, confirming the assertion above is a " +
                "real difference and not an artifact of the test environment.");
        }
        finally
        {
            GraphicalUiElement.AddRenderableToManagers = originalHook;
        }
    }

    [Fact]
    public void AttachManagersOnly_OnAContainer_LetsChildrenResolveEffectiveManagers()
    {
        ContainerRuntime container = new();
        SpriteRuntime child = new();
        child.Parent = container;

        container.AttachManagersOnly(SystemManagers.Default);

        child.EffectiveManagers.ShouldBe(SystemManagers.Default);
    }
}
