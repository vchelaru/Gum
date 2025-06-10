using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.StateAnimation.Runtime;
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
}

