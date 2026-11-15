using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// The values a state animation applies, tick by tick (#7873). Each scenario samples the element after a fixed
/// schedule of ticks and compares the text to what the code recorded before the per-tick interpolated state
/// was reused. Set GUM_RECORD_7873 to a file path to write the current values in the baseline's format.
/// </summary>
public class AnimationTickValuesTests : BaseTestClass
{
    private static readonly double[] Schedule = { 0, 0.05, 0.13, 0.37, 0.5, 0.71, 0.99, 1.0, 1.2, 1.9, 0.3, 0.0 };

    private sealed class World
    {
        public ContainerRuntime Root = null!;
        public ContainerRuntime Box = null!;
        public ColoredRectangleRuntime Fill = null!;
        public TextRuntime Label = null!;
    }

    private static World BuildWorld()
    {
        World world = new World();
        world.Root = new ContainerRuntime { Name = "Root" };
        world.Box = new ContainerRuntime { Name = "Box" };
        world.Fill = new ColoredRectangleRuntime { Name = "Fill" };
        world.Label = new TextRuntime { Name = "Label" };
        world.Root.AddChild(world.Box);
        world.Root.AddChild(world.Fill);
        world.Root.AddChild(world.Label);
        return world;
    }

    private static StateSave AddState(World world, string category, string name, params (string Variable, object Value)[] values)
    {
        if (!world.Root.Categories.TryGetValue(category, out StateSaveCategory? found))
        {
            found = new StateSaveCategory { Name = category };
            world.Root.Categories[category] = found;
        }
        StateSave state = new StateSave { Name = name };
        foreach ((string variable, object value) in values)
        {
            state.Variables.Add(new VariableSave { Name = variable, Value = value, SetsValue = true });
        }
        found.States.Add(state);
        return state;
    }

    private static KeyframeRuntime Key(float time, string stateName,
        InterpolationType interpolation = InterpolationType.Linear, Easing easing = Easing.In)
    {
        return new KeyframeRuntime { Time = time, StateName = stateName, InterpolationType = interpolation, Easing = easing };
    }

    private static AnimationRuntime Animation(World world, bool loops, params KeyframeRuntime[] keyframes)
    {
        AnimationRuntime animation = new AnimationRuntime { Name = "Anim", Loops = loops };
        animation.Keyframes.AddRange(keyframes);
        animation.RefreshCumulativeStates(world.Root);
        world.Root.Animations = new List<AnimationRuntime> { animation };
        return animation;
    }

    private static string Format(object? value)
    {
        return value switch
        {
            null => "null",
            float f => "f" + f.ToString("R", CultureInfo.InvariantCulture),
            double d => "d" + d.ToString("R", CultureInfo.InvariantCulture),
            _ => value.GetType().Name + ":" + value
        };
    }

    private static string Snapshot(World w)
    {
        StringBuilder text = new StringBuilder();
        void Add(string name, object? value) => text.Append(name).Append('=').Append(Format(value)).Append(' ');
        Add("X", w.Root.X); Add("Y", w.Root.Y); Add("W", w.Root.Width); Add("H", w.Root.Height);
        Add("Rot", w.Root.Rotation); Add("Alpha", w.Root.Alpha); Add("Vis", w.Root.Visible);
        Add("BoxX", w.Box.X); Add("BoxW", w.Box.Width); Add("BoxVis", w.Box.Visible);
        Add("R", w.Fill.Red); Add("G", w.Fill.Green); Add("B", w.Fill.Blue); Add("FillAlpha", w.Fill.Alpha);
        Add("Text", w.Label.Text); Add("HA", w.Label.HorizontalAlignment);
        return text.ToString().TrimEnd();
    }

    private static string StateSignature(StateSave state)
    {
        StringBuilder text = new StringBuilder();
        foreach (VariableSave variable in state.Variables)
        {
            text.Append(variable.Name).Append(variable.SetsValue ? "!" : "?").Append(Format(variable.Value)).Append(';');
        }
        return text.ToString();
    }

    // Applies the animation at each scheduled time, then plays it through the controller, then reads the
    // public GetStateToSet, so the three ways to reach the interpolation are covered by one baseline.
    private static string Run(Func<(World World, AnimationRuntime Animation)> create)
    {
        StringBuilder text = new StringBuilder();

        (World direct, AnimationRuntime animation) = create();
        foreach (double time in Schedule)
        {
            animation.ApplyAtTimeTo(time, direct.Root);
            text.Append("at ").Append(time.ToString("R", CultureInfo.InvariantCulture)).Append(": ").AppendLine(Snapshot(direct));
        }

        (World played, AnimationRuntime _) = create();
        List<string> events = new List<string>();
        played.Root.AnimationController.NamedEventOccurred += args => events.Add(args.Name + "@" + args.Time);
        played.Root.PlayAnimation("Anim");
        for (int i = 0; i < 14; i++)
        {
            played.Root.AnimateSelf(0.17);
            text.Append("tick ").Append(i).Append(": ").Append(Snapshot(played))
                .Append(" playing=").AppendLine(played.Root.AnimationController.IsPlaying.ToString());
        }
        text.Append("events: ").AppendLine(string.Join(",", events));

        (World queried, AnimationRuntime queriedAnimation) = create();
        foreach (double time in Schedule)
        {
            text.Append("state ").Append(time.ToString("R", CultureInfo.InvariantCulture)).Append(": ")
                .AppendLine(StateSignature(queriedAnimation.GetStateToSet(time, queried.Root)));
        }
        return text.ToString();
    }

    private static readonly string RecordPath = Environment.GetEnvironmentVariable("GUM_RECORD_7873") ?? string.Empty;

    private static void Check(string scenario, string actual)
    {
        if (RecordPath.Length > 0)
        {
            File.AppendAllText(RecordPath, "### " + scenario + "\n" + actual + "\n");
            return;
        }
        AnimationTickValuesBaseline.Values.TryGetValue(scenario, out string? expected).ShouldBeTrue("no baseline for " + scenario);
        actual.Replace("\r\n", "\n").ShouldBe(expected!.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Tick_FloatX_Loops()
    {
        Check(nameof(Tick_FloatX_Loops), Run(() =>
        {
            World w = BuildWorld();
            AddState(w, "C", "A", ("X", 10f));
            AddState(w, "C", "B", ("X", 110f));
            return (w, Animation(w, true, Key(0, "C/A"), Key(1, "C/B")));
        }));
    }

    [Fact]
    public void Tick_WidthAndHeight_NonLooping()
    {
        Check(nameof(Tick_WidthAndHeight_NonLooping), Run(() =>
        {
            World w = BuildWorld();
            AddState(w, "C", "A", ("Width", 200f), ("Height", 50f));
            AddState(w, "C", "B", ("Width", 300f), ("Height", 150f));
            return (w, Animation(w, false, Key(0, "C/A"), Key(1, "C/B")));
        }));
    }

    [Fact]
    public void Tick_ColorChannelsAlphaAndRotation()
    {
        Check(nameof(Tick_ColorChannelsAlphaAndRotation), Run(() =>
        {
            World w = BuildWorld();
            AddState(w, "C", "A", ("Fill.Red", 0), ("Fill.Green", 255), ("Fill.Blue", 10), ("Alpha", 255), ("Rotation", 0f), ("Fill.Alpha", 100));
            AddState(w, "C", "B", ("Fill.Red", 255), ("Fill.Green", 0), ("Fill.Blue", 21), ("Alpha", 0), ("Rotation", 90f), ("Fill.Alpha", 200));
            return (w, Animation(w, true, Key(0, "C/A"), Key(1, "C/B")));
        }));
    }

    [Fact]
    public void Tick_StringEnumAndBool_DoNotInterpolate()
    {
        Check(nameof(Tick_StringEnumAndBool_DoNotInterpolate), Run(() =>
        {
            World w = BuildWorld();
            AddState(w, "C", "A", ("Label.Text", "first"), ("Label.HorizontalAlignment", RenderingLibrary.Graphics.HorizontalAlignment.Left), ("Visible", true), ("Box.Visible", false));
            AddState(w, "C", "B", ("Label.Text", "second"), ("Label.HorizontalAlignment", RenderingLibrary.Graphics.HorizontalAlignment.Right), ("Visible", false), ("Box.Visible", true));
            return (w, Animation(w, true, Key(0, "C/A"), Key(1, "C/B")));
        }));
    }

    [Fact]
    public void Tick_ManyKeyframes_MixedEasing()
    {
        Check(nameof(Tick_ManyKeyframes_MixedEasing), Run(() =>
        {
            World w = BuildWorld();
            AddState(w, "C", "A", ("X", 0f), ("Y", 5f));
            AddState(w, "C", "B", ("X", 100f), ("Y", 50f));
            AddState(w, "C", "D", ("X", 40f), ("Y", 7f));
            return (w, Animation(w, true,
                Key(0, "C/A", InterpolationType.Quadratic, Easing.Out),
                Key(0.4f, "C/B", InterpolationType.Bounce, Easing.In),
                Key(1.1f, "C/D", InterpolationType.Elastic, Easing.InOut),
                Key(1.6f, "C/A")));
        }));
    }

    [Fact]
    public void Tick_EveryInterpolationTypeAndEasing()
    {
        StringBuilder text = new StringBuilder();
        foreach (InterpolationType interpolation in Enum.GetValues(typeof(InterpolationType)))
        {
            foreach (Easing easing in Enum.GetValues(typeof(Easing)))
            {
                World w = BuildWorld();
                AddState(w, "C", "A", ("X", 3f), ("Rotation", 20f));
                AddState(w, "C", "B", ("X", 97f), ("Rotation", -40f));
                AnimationRuntime animation = Animation(w, false, Key(0, "C/A", interpolation, easing), Key(1, "C/B"));
                text.Append(interpolation).Append('/').Append(easing).Append(':');
                foreach (double time in new[] { 0.0, 0.13, 0.5, 0.87, 1.0 })
                {
                    animation.ApplyAtTimeTo(time, w.Root);
                    text.Append(' ').Append(Format(w.Root.X)).Append('|').Append(Format(w.Root.Rotation));
                }
                text.AppendLine();
            }
        }
        Check(nameof(Tick_EveryInterpolationTypeAndEasing), text.ToString());
    }

    [Fact]
    public void Tick_InstanceTargetedVariables()
    {
        Check(nameof(Tick_InstanceTargetedVariables), Run(() =>
        {
            World w = BuildWorld();
            AddState(w, "C", "A", ("Box.X", 0f), ("Box.Width", 20f), ("Fill.Red", 10), ("X", 1f));
            AddState(w, "C", "B", ("Box.X", 60f), ("Box.Width", 80f), ("Fill.Red", 250), ("X", 9f));
            return (w, Animation(w, true, Key(0, "C/A"), Key(1, "C/B")));
        }));
    }

    [Fact]
    public void Tick_VariableOnlyInTheSecondKeyframe_InterpolatesFromZero()
    {
        Check(nameof(Tick_VariableOnlyInTheSecondKeyframe_InterpolatesFromZero), Run(() =>
        {
            World w = BuildWorld();
            AddState(w, "C", "A", ("X", 10f));
            AddState(w, "C", "B", ("X", 110f), ("Y", 80f), ("Fill.Blue", 90));
            return (w, Animation(w, true, Key(0, "C/A"), Key(1, "C/B")));
        }));
    }

    [Fact]
    public void Tick_TwoCategoriesPlayAsTwoTracks()
    {
        Check(nameof(Tick_TwoCategoriesPlayAsTwoTracks), Run(() =>
        {
            World w = BuildWorld();
            AddState(w, "Move", "A", ("X", 0f));
            AddState(w, "Move", "B", ("X", 100f));
            AddState(w, "Fade", "A", ("Alpha", 255), ("X", 5f));
            AddState(w, "Fade", "B", ("Alpha", 0));
            return (w, Animation(w, true, Key(0, "Move/A"), Key(1, "Move/B"), Key(0.3f, "Fade/A"), Key(0.8f, "Fade/B")));
        }));
    }

    [Fact]
    public void Tick_SubAnimationOnInstanceAndSelf_WithNamedEvent()
    {
        Check(nameof(Tick_SubAnimationOnInstanceAndSelf_WithNamedEvent), Run(() =>
        {
            World w = BuildWorld();
            AddState(w, "C", "A", ("X", 0f));
            AddState(w, "C", "B", ("X", 100f));
            AnimationRuntime sub = new AnimationRuntime { Name = "Sub" };
            sub.Keyframes.Add(Key(0, "C/A"));
            sub.Keyframes.Add(Key(0.5f, "C/B"));
            sub.RefreshCumulativeStates(w.Root);
            AnimationRuntime animation = new AnimationRuntime { Name = "Anim", Loops = true };
            animation.Keyframes.Add(Key(0, "C/A"));
            animation.Keyframes.Add(Key(1, "C/B"));
            animation.Keyframes.Add(new KeyframeRuntime { Time = 0.2f, AnimationName = "Box.Sub", SubAnimation = sub });
            animation.Keyframes.Add(new KeyframeRuntime { Time = 0.4f, AnimationName = "Sub", SubAnimation = sub });
            animation.Keyframes.Add(new KeyframeRuntime { Time = 0.5f, EventName = "Ping" });
            animation.RefreshCumulativeStates(w.Root);
            w.Root.Animations = new List<AnimationRuntime> { animation };
            return (w, animation);
        }));
    }

    // The same AnimationRuntime playing on two elements at different times, interleaved.
    [Fact]
    public void Tick_OneAnimationOnTwoElements_KeepsEachElementsOwnValues()
    {
        World first = BuildWorld();
        World second = BuildWorld();
        AddState(first, "C", "A", ("X", 0f), ("Fill.Red", 0));
        AddState(first, "C", "B", ("X", 100f), ("Fill.Red", 200));
        AnimationRuntime animation = Animation(first, true, Key(0, "C/A"), Key(1, "C/B"));
        second.Root.Categories["C"] = first.Root.Categories["C"];
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 10; i++)
        {
            animation.ApplyAtTimeTo(i * 0.1, first.Root);
            animation.ApplyAtTimeTo(1 - i * 0.1, second.Root);
            text.AppendLine(Snapshot(first)).AppendLine(Snapshot(second));
        }
        Check(nameof(Tick_OneAnimationOnTwoElements_KeepsEachElementsOwnValues), text.ToString());
    }

    // A component with instances, played through the element path (ApplyAtTimeTo with an ElementSave).
    [Fact]
    public void Tick_ElementPath_VariablesOnlyInTheSecondKeyframeComeFromTheDefaultState()
    {
        StandardElementsManager.Self.Initialize();
        ComponentSave element = new ComponentSave { Name = "Wave", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(defaultState);
        defaultState.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 33f, SetsValue = true });
        element.Instances.Add(new InstanceSave { Name = "Box", BaseType = "Container", ParentContainer = element });
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(element);
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container")!);
        container.DefaultState!.ParentContainer = container;
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;

        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        StateSave a = new StateSave { Name = "A", ParentContainer = element };
        a.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 0f, SetsValue = true });
        a.Variables.Add(new VariableSave { Name = "Box.X", Type = "float", Value = 5f, SetsValue = true });
        StateSave b = new StateSave { Name = "B", ParentContainer = element };
        b.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 100f, SetsValue = true });
        b.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 133f, SetsValue = true });
        b.Variables.Add(new VariableSave { Name = "Box.X", Type = "float", Value = 45f, SetsValue = true });
        b.Variables.Add(new VariableSave { Name = "Box.Y", Type = "float", Value = 9f, SetsValue = true });
        category.States.Add(a);
        category.States.Add(b);
        element.Categories.Add(category);
        AnimationRuntime animation = new AnimationRuntime { Loops = true };
        animation.Keyframes.Add(Key(0, "Cat/A"));
        animation.Keyframes.Add(Key(1, "Cat/B"));
        animation.RefreshCumulativeStates(element);

        GraphicalUiElement gue = element.ToGraphicalUiElement();
        GraphicalUiElement box = gue.GetChildByNameRecursively("Box")!;
        StringBuilder text = new StringBuilder();
        foreach (double time in Schedule)
        {
            animation.ApplyAtTimeTo(time, gue);
            text.Append(time.ToString("R", CultureInfo.InvariantCulture)).Append(": X=").Append(Format(gue.X))
                .Append(" W=").Append(Format(gue.Width)).Append(" BoxX=").Append(Format(box.X))
                .Append(" BoxY=").Append(Format(box.Y)).Append(" | ")
                .AppendLine(StateSignature(animation.GetStateToSet(time, element)));
        }
        Check(nameof(Tick_ElementPath_VariablesOnlyInTheSecondKeyframeComeFromTheDefaultState), text.ToString());
        ObjectFinder.Self.GumProjectSave = null;
    }

    // Hazards that need no recorded baseline.

    [Fact]
    public void GetStateToSet_ReturnedStateKeptByTheCaller_IsNotChangedByLaterTicks()
    {
        World w = BuildWorld();
        AddState(w, "C", "A", ("X", 0f), ("Fill.Red", 0));
        AddState(w, "C", "B", ("X", 100f), ("Fill.Red", 200));
        AnimationRuntime animation = Animation(w, true, Key(0, "C/A"), Key(1, "C/B"));
        StateSave kept = animation.GetStateToSet(0.25, w.Root);
        string before = StateSignature(kept);

        animation.ApplyAtTimeTo(0.75, w.Root);
        animation.GetStateToSet(0.9, w.Root);
        animation.ApplyAtTimeTo(0.1, w.Root);

        StateSignature(kept).ShouldBe(before);
        before.ShouldContain("f25");
    }

    private sealed class ReentrantContainer : ContainerRuntime
    {
        public AnimationRuntime? Animation;
        public GraphicalUiElement? Other;
        public bool Inside;

        public override void ApplyState(StateSave state)
        {
            // Another tick of the same animation lands between the state being built and being applied.
            if (!Inside && Animation != null && Other != null)
            {
                Inside = true;
                Animation.ApplyAtTimeTo(1.0, Other);
                Inside = false;
            }
            base.ApplyState(state);
        }
    }

    [Fact]
    public void ApplyAtTimeTo_AnotherTickOfTheSameAnimationDuringApplyState_DoesNotChangeTheOuterTick()
    {
        World w = BuildWorld();
        ReentrantContainer outer = new ReentrantContainer { Name = "Outer" };
        outer.Categories["C"] = new StateSaveCategory { Name = "C" };
        StateSave a = new StateSave { Name = "A" };
        a.Variables.Add(new VariableSave { Name = "X", Value = 0f, SetsValue = true });
        a.Variables.Add(new VariableSave { Name = "Y", Value = 0f, SetsValue = true });
        StateSave b = new StateSave { Name = "B" };
        b.Variables.Add(new VariableSave { Name = "X", Value = 100f, SetsValue = true });
        b.Variables.Add(new VariableSave { Name = "Y", Value = 200f, SetsValue = true });
        outer.Categories["C"].States.Add(a);
        outer.Categories["C"].States.Add(b);
        AnimationRuntime animation = new AnimationRuntime { Name = "Anim", Loops = true };
        animation.Keyframes.Add(Key(0, "C/A"));
        animation.Keyframes.Add(Key(1, "C/B"));
        animation.RefreshCumulativeStates(outer);
        outer.Animation = animation;
        outer.Other = w.Root;
        w.Root.Categories["C"] = outer.Categories["C"];

        animation.ApplyAtTimeTo(0.25, outer);

        outer.X.ShouldBe(25f);
        outer.Y.ShouldBe(50f);
        w.Root.X.ShouldBe(0f); // t = 1.0 on a looping animation of length 1 wraps to 0
    }
}
