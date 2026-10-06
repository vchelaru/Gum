using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// AnimateSelf reaches the elements an element holds, whether they are its children or, when it has
/// no renderable of its own (such as an old-style screen), only held through ElementGueContainingThis
/// (#5834).
/// </summary>
public class AnimateSelfContainedElementsTests : BaseTestClass
{
    // A looping chain whose single frame outlasts the test, so AnimationChainTime shows how many
    // seconds the sprite advanced.
    private static AnimationChainList CreateChains()
    {
        AnimationChain chain = new() { Name = "Chain" };
        chain.Add(new AnimationFrame { FrameLength = 10f });
        AnimationChainList chains = new();
        chains.Add(chain);
        return chains;
    }

    private static void MakeAnimated(SpriteRuntime sprite)
    {
        sprite.AnimationChains = CreateChains();
        sprite.Animate = true;
    }

    [Fact]
    public void AnimateSelf_ShouldAdvanceEachHeldElementOnce_WhenElementHasNoContainedObject()
    {
        GraphicalUiElement screen = new();
        SpriteRuntime heldSprite = new();
        MakeAnimated(heldSprite);
        heldSprite.ElementGueContainingThis = screen;
        // A held instance attached to another held instance: reached through its parent's
        // Children, so it must not advance a second time.
        ContainerRuntime heldContainer = new();
        heldContainer.ElementGueContainingThis = screen;
        SpriteRuntime nestedSprite = new();
        MakeAnimated(nestedSprite);
        nestedSprite.Parent = heldContainer;
        nestedSprite.ElementGueContainingThis = screen;

        screen.AnimateSelf(1);

        heldSprite.AnimationChainTime.ShouldBe(1);
        nestedSprite.AnimationChainTime.ShouldBe(1);
    }

    [Fact]
    public void AnimateSelf_ShouldAdvanceInstance_WhenScreenIsBuiltFromScreenSave()
    {
        GumProjectSave gumProject = new();
        StandardElementSave spriteStandard = new() { Name = "Sprite" };
        spriteStandard.States.Add(new StateSave { Name = "Default", ParentContainer = spriteStandard });
        gumProject.StandardElements.Add(spriteStandard);
        ScreenSave screenSave = new() { Name = "TestScreen" };
        screenSave.States.Add(new StateSave { Name = "Default", ParentContainer = screenSave });
        screenSave.Instances.Add(new InstanceSave
        {
            Name = "SpriteInstance",
            BaseType = "Sprite",
            ParentContainer = screenSave
        });
        gumProject.Screens.Add(screenSave);
        ObjectFinder.Self.GumProjectSave = gumProject;

        GraphicalUiElement screen = screenSave.ToGraphicalUiElement();
        // This test registers no runtime types, so the instance is a GraphicalUiElement wrapping a Sprite.
        Sprite sprite = (Sprite)screen.GetGraphicalUiElementByName("SpriteInstance")!.RenderableComponent;
        sprite.AnimationChains = CreateChains();
        sprite.Animate = true;

        screen.AnimateSelf(1);

        sprite.TimeIntoAnimation.ShouldBe(1);
    }
}
