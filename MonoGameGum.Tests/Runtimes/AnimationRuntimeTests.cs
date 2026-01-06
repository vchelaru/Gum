using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class AnimationRuntimeTests : BaseTestClass
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
    public void RefreshCumulativeStates_ShouldInterpolateValuesPoperly()
    {
        ComponentSave element = new();
        element.States.Add(new StateSave()); // give it a default state
        element.Name = "Animated component";

        StateSaveCategory alphaCategory = new();
        element.Categories.Add(alphaCategory);
        alphaCategory.Name = "AlphaCategory";

        StateSave alphaState1 = new();
        alphaCategory.States.Add(alphaState1);
        alphaState1.Name = "AlphaState1";
        alphaState1.Variables.Add(new VariableSave()
        {
            Name = "Alpha",
            Value = 0f
        });
        
        StateSave alphaState2 = new();
        alphaCategory.States.Add(alphaState2);
        alphaState2.Name = "AlphaState2";
        alphaState2.Variables.Add(new VariableSave()
        {
            Name = "Alpha",
            Value = 1f
        });

        StateSaveCategory positionCategory = new();
        element.Categories.Add(positionCategory);
        positionCategory.Name = "PositionCategory";

        StateSave positionState1 = new();
        positionCategory.States.Add(positionState1);
        positionState1.Name = "PositionState1";
        positionState1.Variables.Add(new VariableSave()
        {
            Name = "X",
            Value = 0f
        });

        StateSave positionState2 = new();
        positionCategory.States.Add(positionState2);
        positionState2.Name = "PositionState2";
        positionState2.Variables.Add(new VariableSave()
        {
            Name = "X",
            Value = 100f
        });

        KeyframeRuntime alphaKeyframe1 = new();
        alphaKeyframe1.InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear;
        alphaKeyframe1.Time = 0;
        alphaKeyframe1.StateName = "AlphaCategory/AlphaState1";

        KeyframeRuntime alphaKeyframe2 = new();
        alphaKeyframe2.Time = 1;
        alphaKeyframe2.StateName = "AlphaCategory/AlphaState2";

        KeyframeRuntime positionKeyframe1 = new();
        positionKeyframe1.InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear;
        positionKeyframe1.Time = 0.5f;
        positionKeyframe1.StateName = "PositionCategory/PositionState1";

        KeyframeRuntime positionKeyframe2 = new();
        positionKeyframe2.Time = 1.5f;
        positionKeyframe2.StateName = "PositionCategory/PositionState2";

        AnimationRuntime animation = new();
        animation.Keyframes.Add(alphaKeyframe1);
        animation.Keyframes.Add(positionKeyframe1);
        animation.Keyframes.Add(alphaKeyframe2);
        animation.Keyframes.Add(positionKeyframe2);

        animation.RefreshCumulativeStates(element);

        StateSave at0_5 = animation.GetStateToSet(0.5f, element, defaultIfNull: true);

        at0_5.Variables.First(item => item.Name == "Alpha").Value.ShouldBe(0.5f);
        at0_5.Variables.First(item => item.Name == "X").Value.ShouldBe(0f);

        StateSave at1_0 = animation.GetStateToSet(1.0f, element, defaultIfNull: true);

        at1_0.Variables.First(item => item.Name == "Alpha").Value.ShouldBe(1f);
        at1_0.Variables.First(item => item.Name == "X").Value.ShouldBe(50f);
    }

    [Fact]
    public void RefreshCumulativeStates_ShouldSetAllValues_BeforeKeyframe()
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
            Value = 44f
        });

        KeyframeRuntime keyframe = new();
        keyframe.StateName = "Category1/State1";
        keyframe.Time = 1;

        AnimationRuntime animation = new();
        animation.Keyframes.Add(keyframe);

        animation.RefreshCumulativeStates(element);

        StateSave at0 = animation.GetStateToSet(0f, element, defaultIfNull: true);
        at0.Variables.Count.ShouldBe(1);
        at0.Variables[0].Name.ShouldBe("X");
        at0.Variables[0].Value.ShouldBe(44f);
    }

    [Fact]
    public void GetStateToSet_ShouldNotThrowException_IfRefreshCmulativeStatesIsntCalled()
    {
        ComponentSave element = new();
        element.States.Add(new StateSave()); // give it a default state
        element.Name = "Animated component";

        AnimationRuntime animation = CreateXSettingAnimationRuntime(element);

        // Not calling RefreshCumulativeStates here, so we get an exception
        //animation.RefreshCumulativeStates(element);

        bool didThrow = false;
        try
        {
            animation.GetStateToSet(0.5,
                // Intentionally null, so we get an exception
                element: null!,
                defaultIfNull: true);
        }
        catch
        {
            didThrow = true;
        }

        didThrow.ShouldBeFalse("Because as of November 2025 this is no longer required");
    }


    [Fact]
    public void GetStateToSet_ShouldRespectVariableSpecificInterpolationType()
    {
        ComponentSave element = new();
        element.States.Add(new StateSave()); // give it a default state
        element.Name = "Animated component";

        StateSaveCategory xCategory = new StateSaveCategory();
        element.Categories.Add(xCategory);
        xCategory.Name = "XCategory";

        var xState1 = new StateSave();
        xCategory.States.Add(xState1);
        xState1.Name = "XState1";
        xState1.Variables.Add(new VariableSave()
        {
            Name = "X",
            Value = 0f
        });

        var xState2 = new StateSave();
        xCategory.States.Add(xState2);
        xState2.Name = "XState2";
        xState2.Variables.Add(new VariableSave()
        {
            Name = "X",
            Value = 100f
        });

        StateSaveCategory yCategory = new StateSaveCategory();
        element.Categories.Add(yCategory);
        yCategory.Name = "YCategory";

        var yState1 = new StateSave();
        yCategory.States.Add(yState1);
        yState1.Name = "YState1";
        yState1.Variables.Add(new VariableSave()
        {
            Name = "Y",
            Value = 0f
        });

        var yState2 = new StateSave();
        yCategory.States.Add(yState2);
        yState2.Name = "YState2";
        yState2.Variables.Add(new VariableSave()
        {
            Name = "Y",
            Value = 100f
        });


        KeyframeRuntime keyframeX0 = new();
        keyframeX0.InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Elastic;
        keyframeX0.Easing = FlatRedBall.Glue.StateInterpolation.Easing.Out;
        keyframeX0.Time = 0;
        keyframeX0.StateName = "XCategory/XState1";

        KeyframeRuntime keyframeX1 = new();
        keyframeX1.Time = 1;
        keyframeX1.StateName = "XCategory/XState2";

        KeyframeRuntime keyframeY0 = new();
        keyframeY0.InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear;
        keyframeY0.Time = .5f;
        keyframeY0.StateName = "YCategory/YState1";

        AnimationRuntime noY = new ();
        noY.Keyframes.Add(keyframeX0);
        noY.Keyframes.Add(keyframeX1);
        noY.RefreshCumulativeStates(element);

        AnimationRuntime withY = new ();
        withY.Keyframes.Add(FileManager.CloneSaveObject<KeyframeRuntime>(keyframeX0));
        withY.Keyframes.Add(FileManager.CloneSaveObject<KeyframeRuntime>(keyframeY0));
        withY.Keyframes.Add(FileManager.CloneSaveObject<KeyframeRuntime>(keyframeX1));
        withY.RefreshCumulativeStates(element);

        for (float t = .1f; t <= 1; t+= .1f)
        {
            var stateNoY = noY.GetStateToSet(t, element, defaultIfNull: true);
            var stateWithY = withY.GetStateToSet(t, element, defaultIfNull: true);
            var xNoY = (float)stateNoY.Variables.First(item => item.Name == "X").Value!;
            var xWithY = (float)stateWithY.Variables.First(item => item.Name == "X").Value!;
            xNoY.ShouldBe(xWithY, $"At time {t}, expected X values to match");
        }
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

    [Fact]
    public void PlayAnimation_ShouldApplyState_InUpdate()
    {

        ContainerRuntime runtime = new();
        runtime.AddToRoot();

        var animation = CreateXSettingAnimationRuntime(gue:runtime);
        animation.Name = "Animation1";
        runtime.Animations = new List<AnimationRuntime>
        {
            animation
        };
        runtime.PlayAnimation("Animation1");

        GumService.Default.Update(
            new Microsoft.Xna.Framework.GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));
        GumService.Default.Update(
            new Microsoft.Xna.Framework.GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));

        runtime.X.ShouldBe(100);
    }

    #region Utilities

    private static AnimationRuntime CreateXSettingAnimationRuntime(ComponentSave? element = null, GraphicalUiElement? gue = null)
    {
        StateSaveCategory category = new();
        element?.Categories.Add(category);
        category.Name = "Category1";
        if(gue != null)
        {
            gue.Categories[category.Name] = category;
        }

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
        return animation;
    }

    #endregion
}

