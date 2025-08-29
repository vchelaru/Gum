using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class AnimationRuntimeTests
{
    [Fact]
    public void GetStateToSet_ShouldInterpolateKeyframes()
    {
        ComponentSave element = new ();
        element.States.Add(new StateSave()); // give it a default state
        element.Name = "Animated component";

        StateSaveCategory category = new ();
        element.Categories.Add (category);
        category.Name = "Category1";

        StateSave state = new ();
        category.States.Add(state);
        state.Name = "State1";
        state.Variables.Add(new()
        {
            Name = "X",
            Value = 0f
        });

        StateSave state2 = new();
        category.States.Add(state2);
        state2.Name = "State2";
        state2.Variables.Add(new()
        {
            Name = "X",
            Value = 100f
        });

        KeyframeRuntime keyframe1 = new();
        keyframe1.InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear;
        keyframe1.Time = 0;
        keyframe1.StateName = "Category1/State1";

        KeyframeRuntime keyframe2 = new();
        keyframe2.Time = 1;
        keyframe2.StateName = "Category1/State2";

        AnimationRuntime animation = new();
        animation.Keyframes.Add(keyframe1);
        animation.Keyframes.Add(keyframe2);
        animation.RefreshCumulativeStates(element);

        var interpolated = animation.GetStateToSet(0.5, element, defaultIfNull: true);
        interpolated.Variables.Count.ShouldBe(1);
        interpolated.Variables[0].Name.ShouldBe("X");
        interpolated.Variables[0].Value.ShouldBe(50f);
    }

    [Fact]
    public void GetStateToSet_ShouldThrowException_IfRefreshCmulativeStatesIsntCalled()
    {
        ComponentSave element = new();
        element.States.Add(new StateSave()); // give it a default state
        element.Name = "Animated component";

        StateSaveCategory category = new();
        element.Categories.Add(category);
        category.Name = "Category1";

        StateSave state = new();
        category.States.Add(state);
        state.Name = "State1";
        state.Variables.Add(new()
        {
            Name = "X",
            Value = 0f
        });

        StateSave state2 = new();
        category.States.Add(state2);
        state2.Name = "State2";
        state2.Variables.Add(new()
        {
            Name = "X",
            Value = 100f
        });

        KeyframeRuntime keyframe1 = new();
        keyframe1.InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear;
        keyframe1.Time = 0;
        keyframe1.StateName = "Category1/State1";

        KeyframeRuntime keyframe2 = new();
        keyframe2.Time = 1;
        keyframe2.StateName = "Category1/State2";

        AnimationRuntime animation = new();
        animation.Keyframes.Add(keyframe1);
        animation.Keyframes.Add(keyframe2);

        // Not calling RefreshCumulativeStates here, so we get an exception
        //animation.RefreshCumulativeStates(element);

        bool didThrow = false;
        try
        {
            animation.GetStateToSet(0.5, 
                // Intentionally null, so we get an exception
                element:null, 
                defaultIfNull: true);
        }
        catch
        {
            didThrow = true;
        }

        didThrow.ShouldBeTrue();
    }

    [Fact]
    public void ComponentAnimation_ShouldContainAnimations()
    {
        var component = new ComponentSave();
        component.Name = "AnimatedComponent";
        component.States.Add(new StateSave()
        {
            ParentContainer = component,
            Name = "Default"
        });

        var project = new GumProjectSave();
        project.ElementAnimations = new()
        {
            new Gum.StateAnimation.SaveClasses.ElementAnimationsSave
            {
                Animations = new ()
                {
                    new Gum.StateAnimation.SaveClasses.AnimationSave
                    {
                        Name = "TestAnim"
                    }
                },
                ElementName = "AnimatedComponent"
            }
        };
        ObjectFinder.Self.GumProjectSave = project;

        var runtime = component.ToGraphicalUiElement();


        runtime.Animations.ShouldNotBeNull();
        runtime.Animations.Count.ShouldBe(1);

        var animation = runtime.Animations[0];
        runtime.PlayAnimation(animation);
        runtime.PlayAnimation("TestAnim");

    }
}

