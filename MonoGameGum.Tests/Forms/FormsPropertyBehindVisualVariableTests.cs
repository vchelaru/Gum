using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A state or an animation writes a visual variable (StackSpacing, ChildrenLayout, Text) through SetProperty. When a Forms
/// property sits on top of that variable, the Forms property must read the new value and the rows that read it
/// (<c>Text = Ctl1.Spacing</c>) must re-evaluate (#8058).
/// </summary>
public class FormsPropertyBehindVisualVariableTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static string RepoRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (directory != null && !File.Exists(Path.Combine(directory, "GumFull.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }
        return directory ?? throw new InvalidOperationException("Repo root not found.");
    }

    private sealed class Built
    {
        public required GraphicalUiElement Screen;
        public required InteractiveGue Visual;
        public required FrameworkElement Control;
        public required TextRuntime Probe;
    }

    // A screen with one control of the template component carrying the behavior, and a Text whose row reads "Ctl1.<property>".
    private static Built Build(string behavior, string property)
    {
        GumExpressionService.Initialize();
        string gumx = Path.Combine(RepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsTemplate", "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ComponentSave component = project.Components.First(c => c.Behaviors.Any(b => b.BehaviorName == behavior));
        ScreenSave screenSave = new ScreenSave { Name = "Probe_" + behavior };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screenSave };
        screenSave.States.Add(state);
        screenSave.Instances.Add(new InstanceSave { Name = "Ctl1", BaseType = component.Name, ParentContainer = screenSave });
        screenSave.Instances.Add(new InstanceSave { Name = "Probe", BaseType = "Text", ParentContainer = screenSave });
        VariableListSave<string> list = new VariableListSave<string> { Name = "Probe.VariableReferences", Type = "string" };
        list.Value.Add("Text = Ctl1." + property);
        state.VariableLists.Add(list);
        project.Screens.Add(screenSave);
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;

        StandardRuntimeRegistrations.Register();
        foreach (string unrendered in new[] { "Circle", "Rectangle", "Polygon" })
        {
            ElementSaveExtensions.RegisterGueInstantiation(unrendered, () => new ContainerRuntime());
        }
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        GraphicalUiElement screen = screenSave.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        InteractiveGue visual = (InteractiveGue)screen.GetGraphicalUiElementByName("Ctl1")!;
        return new Built
        {
            Screen = screen,
            Visual = visual,
            Control = (FrameworkElement)visual.FormsControlAsObject!,
            Probe = (TextRuntime)screen.GetGraphicalUiElementByName("Probe")!
        };
    }

    private static StateSave AddState(GraphicalUiElement visual, string name, string variable, string type, object value)
    {
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        StateSave state = new StateSave { Name = name };
        state.Variables.Add(new VariableSave { Name = variable, Type = type, Value = value, SetsValue = true });
        category.States.Add(state);
        visual.Categories["Cat"] = category;
        return state;
    }

    private static void RunAnimationToEnd(GraphicalUiElement visual, string variable, string type, object from, object to)
    {
        StateSaveCategory category = new StateSaveCategory { Name = "AnimCat" };
        StateSave start = new StateSave { Name = "Start" };
        start.Variables.Add(new VariableSave { Name = variable, Type = type, Value = from, SetsValue = true });
        StateSave end = new StateSave { Name = "End" };
        end.Variables.Add(new VariableSave { Name = variable, Type = type, Value = to, SetsValue = true });
        category.States.Add(start);
        category.States.Add(end);
        ElementSave element = visual.ElementSave!;
        start.ParentContainer = element;
        end.ParentContainer = element;
        element.Categories.Add(category);

        AnimationRuntime animation = new AnimationRuntime { Name = "Anim" };
        animation.Keyframes.Add(new KeyframeRuntime
        {
            StateName = "AnimCat/Start",
            Time = 0,
            InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear
        });
        animation.Keyframes.Add(new KeyframeRuntime { StateName = "AnimCat/End", Time = 1 });
        animation.RefreshCumulativeStates(element);
        visual.Animations = new List<AnimationRuntime> { animation };
        visual.PlayAnimation("Anim");
        visual.AnimateSelf(0);
        visual.AnimateSelf(2);
    }

    [Fact(Skip = "#8061: visual variable set by a state or animation does not reach the Forms layer")]
    public void ApplyState_StackSpacing_UpdatesSpacingAndReevaluatesRows()
    {
        Built built = Build("StackPanelBehavior", "Spacing");
        StackPanel panel = (StackPanel)built.Control;
        panel.Spacing.ShouldBe(0);
        string? before = built.Probe.Text;
        StateSave state = AddState(built.Visual, "Wide", "StackSpacing", "float", 12f);

        built.Visual.ApplyState(state);

        panel.Spacing.ShouldBe(12);
        built.Probe.Text.ShouldNotBe(before);
        string? afterState = built.Probe.Text;
        built.Screen.RefreshReferences("Ctl1.Spacing");
        built.Probe.Text.ShouldBe(afterState);
    }

    [Fact(Skip = "#8061: visual variable set by a state or animation does not reach the Forms layer")]
    public void Animation_StackSpacing_UpdatesSpacingAndReevaluatesRows()
    {
        Built built = Build("StackPanelBehavior", "Spacing");
        StackPanel panel = (StackPanel)built.Control;
        string? before = built.Probe.Text;

        RunAnimationToEnd(built.Visual, "StackSpacing", "float", 0f, 20f);

        panel.Spacing.ShouldBe(20);
        built.Probe.Text.ShouldNotBe(before);
        string? afterAnimation = built.Probe.Text;
        built.Screen.RefreshReferences("Ctl1.Spacing");
        built.Probe.Text.ShouldBe(afterAnimation);
    }

    [Fact(Skip = "#8061: visual variable set by a state or animation does not reach the Forms layer")]
    public void ApplyState_ChildrenLayout_UpdatesOrientationAndReevaluatesRows()
    {
        Built built = Build("StackPanelBehavior", "Orientation");
        StackPanel panel = (StackPanel)built.Control;
        panel.Orientation.ShouldBe(Orientation.Vertical);
        string? before = built.Probe.Text;
        StateSave state = AddState(built.Visual, "Row", "ChildrenLayout", "ChildrenLayout", ChildrenLayout.LeftToRightStack);

        built.Visual.ApplyState(state);

        built.Visual.ChildrenLayout.ShouldBe(ChildrenLayout.LeftToRightStack);
        panel.Orientation.ShouldBe(Orientation.Horizontal);
        built.Probe.Text.ShouldNotBe(before);
    }

    [Fact(Skip = "#8061: visual variable set by a state or animation does not reach the Forms layer")]
    public void Animation_ChildrenLayout_UpdatesOrientation()
    {
        Built built = Build("StackPanelBehavior", "Orientation");
        StackPanel panel = (StackPanel)built.Control;

        RunAnimationToEnd(built.Visual, "ChildrenLayout", "ChildrenLayout",
            ChildrenLayout.TopToBottomStack, ChildrenLayout.LeftToRightStack);

        built.Visual.ChildrenLayout.ShouldBe(ChildrenLayout.LeftToRightStack);
        panel.Orientation.ShouldBe(Orientation.Horizontal);
    }

    [Fact]
    public void ApplyState_LabelText_UpdatesTextAndReevaluatesRows()
    {
        Built built = Build("LabelBehavior", "Text");
        Label label = (Label)built.Control;
        string? before = built.Probe.Text;
        StateSave state = AddState(built.Visual, "Changed", "Text", "string", "from state");

        built.Visual.ApplyState(state);

        label.Text.ShouldBe("from state");
        built.Probe.Text.ShouldBe("from state");
        built.Probe.Text.ShouldNotBe(before);
    }
}
