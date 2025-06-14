using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class GraphicalUiElementTests
{
    #region Animation
    static (ComponentSave element, AnimationRuntime animation) CreateElementAndAnimation()
    {
        ComponentSave element = new();
        element.Name = "Animated component";
        element.States.Add(new StateSave { Name = "Default" });

        var category = new StateSaveCategory { Name = "Category1" };
        element.Categories.Add(category);

        var state1 = new StateSave { Name = "State1" };
        state1.Variables.Add(new() { Name = "X", Value = 0f });
        category.States.Add(state1);

        var state2 = new StateSave { Name = "State2" };
        state2.Variables.Add(new() { Name = "X", Value = 100f });
        category.States.Add(state2);

        var key1 = new KeyframeRuntime
        {
            InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear,
            Time = 0,
            StateName = "Category1/State1"
        };

        var key2 = new KeyframeRuntime
        {
            Time = 1,
            StateName = "Category1/State2"
        };

        var animation = new AnimationRuntime { Name = "Anim1" };
        animation.Keyframes.Add(key1);
        animation.Keyframes.Add(key2);
        animation.RefreshCumulativeStates(element);

        return (element, animation);
    }

    [Fact]
    public void UpdateAnimation_ShouldApplyAnimation()
    {
        var (element, animation) = CreateElementAndAnimation();
        var gue = new GraphicalUiElement(new InvisibleRenderable()) { ElementSave = element };

        gue.ApplyAnimation(animation, 0.5);

        gue.X.ShouldBe(50f);
    }

    [Fact]
    public void UpdateAnimation_ByIndex_ShouldApplyAnimation()
    {
        var (element, animation) = CreateElementAndAnimation();
        var gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            ElementSave = element,
            Animations = new() { animation }
        };

        gue.ApplyAnimation(0, 1.0);

        gue.X.ShouldBe(100f);
    }

    [Fact]
    public void ApplyAnimation_ByName_ShouldApplyAnimation()
    {
        var (element, animation) = CreateElementAndAnimation();
        var gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            ElementSave = element,
            Animations = new() { animation }
        };

        gue.ApplyAnimation("Anim1", 1.0);

        gue.X.ShouldBe(100f);
    }

    [Fact]
    public void GetAnimation_ShouldReturnNull_IfIndexInvalid()
    {
        var gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            Animations = new()
        };

        gue.GetAnimation(1).ShouldBeNull();
    }

    [Fact]
    public void GetAnimation_ShouldReturnNull_IfNameInvalid()
    {
        var gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            Animations = new()
        };

        gue.GetAnimation("Missing").ShouldBeNull();
    }

    [Fact]
    public void PlayAndStopAnimation_ShouldControlAnimation()
    {
        var (element, animation) = CreateElementAndAnimation();
        var gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            ElementSave = element
        };

        gue.PlayAnimation(animation);
        gue.AnimateSelf(0.5);
        gue.X.ShouldBe(50f);

        gue.StopAnimation();
        gue.AnimateSelf(0.5);
        gue.X.ShouldBe(50f);
    }

    [Fact]
    public void ApplyAnimation_ShouldThrow_IfNull()
    {
        var gue = new GraphicalUiElement(new InvisibleRenderable());
        bool didThrow = false;
        try
        {
            gue.ApplyAnimation(animation: null!, timeInSeconds: 0);
        }
        catch (Exception)
        {
            didThrow = true;
        }
        didThrow.ShouldBeTrue();
    }

    [Fact]
    public void PlayAnimation_ShouldThrow_IfNull()
    {
        var gue = new GraphicalUiElement(new InvisibleRenderable());
        bool didThrow = false;
        try
        {
            gue.PlayAnimation(animation: null!);
        }
        catch (Exception)
        {
            didThrow = true;
        }
        didThrow.ShouldBeTrue();
    }
    #endregion
    
    [Fact]
    public void FillListWithChildrenByType_ShouldFillRecursively()
    {
        ContainerRuntime sut = new();

        sut.Children.Add(new SpriteRuntime());
        sut.Children.Add(new TextRuntime());
        ContainerRuntime childContainer = new();
        childContainer.Children.Add(new SpriteRuntime());
        sut.Children.Add(childContainer);

        var list = sut.FillListWithChildrenByTypeRecursively<SpriteRuntime>();

        list.Count.ShouldBe(2);
        list[0].ShouldBeOfType<SpriteRuntime>();
        list[1].ShouldBeOfType<SpriteRuntime>();
    }
}
