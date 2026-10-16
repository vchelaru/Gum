using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// ApplyState suspends the element and every descendant (SuspendLayout(recursive: true)) for the
// duration of the state. A variable that throws (here via a PositionChanged handler, standing in for
// a custom setter or a binding) must not leave the whole subtree suspended: every later UpdateLayout
// on those elements early-outs and they keep stale positions and sizes.
public class ApplyStateLayoutSuspensionTests : BaseTestClass
{
    private static StateSave CreateStateSettingX(float x)
    {
        StateSave state = new() { Name = "MoveRight" };
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = x });
        return state;
    }

    [Fact]
    public void ApplyState_ShouldResumeLayoutOnSubtree_WhenSettingAVariableThrows()
    {
        ContainerRuntime parent = new();
        ContainerRuntime child = new();
        parent.Children.Add(child);
        parent.PositionChanged += (_, _) => throw new InvalidOperationException("handler failed");

        Should.Throw<InvalidOperationException>(() => parent.ApplyState(CreateStateSettingX(50)));

        parent.IsLayoutSuspended.ShouldBeFalse();
        child.IsLayoutSuspended.ShouldBeFalse();
    }

    [Fact]
    public void ApplyState_ShouldKeepLayoutSuspended_WhenSuspendedBeforeTheCallAndAVariableThrows()
    {
        ContainerRuntime parent = new();
        ContainerRuntime child = new();
        parent.Children.Add(child);
        parent.SuspendLayout(recursive: true);
        parent.PositionChanged += (_, _) => throw new InvalidOperationException("handler failed");

        Should.Throw<InvalidOperationException>(() => parent.ApplyState(CreateStateSettingX(50)));

        parent.IsLayoutSuspended.ShouldBeTrue();
        child.IsLayoutSuspended.ShouldBeTrue();
    }

    [Fact]
    public void ApplyStateVariableValues_ShouldResumeLayoutOnSubtree_WhenSettingAVariableThrows()
    {
        ContainerRuntime parent = new();
        ContainerRuntime child = new();
        parent.Children.Add(child);
        parent.PositionChanged += (_, _) => throw new InvalidOperationException("handler failed");
        List<VariableSaveValues> values = new() { new VariableSaveValues { Name = "X", Value = 50f } };

        Should.Throw<InvalidOperationException>(() => parent.ApplyState(values));

        parent.IsLayoutSuspended.ShouldBeFalse();
        child.IsLayoutSuspended.ShouldBeFalse();
    }
}
