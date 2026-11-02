using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Values that code sets on a Forms control (what a generated CustomInitialize does) stay set when
/// ancestor elements are built afterward, while values the project authored still apply (#7160).
/// </summary>
public class FormsPropertiesSetInCodeSurviveAncestorBuildTests : BaseTestClass
{
    private const string HostName = "Host";

    /// <summary>
    /// Stands in for a generated component whose CustomInitialize runs in AfterFullCreation.
    /// </summary>
    private class HostRuntime : ContainerRuntime
    {
        public static Action<HostRuntime>? CustomInitialize;

        public HostRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
            : base()
        {
        }

        public override void AfterFullCreation()
        {
            base.AfterFullCreation();
            CustomInitialize?.Invoke(this);
        }

        public T GetControl<T>(string name) where T : FrameworkElement
        {
            return (T)((InteractiveGue)GetGraphicalUiElementByName(name)!).FormsControlAsObject!;
        }
    }

    public override void Dispose()
    {
        HostRuntime.CustomInitialize = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    // Slider -------------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SliderRangeSetInCustomInitialize_AncestorsBuilt_KeepsRange(int wrapperCount)
    {
        HostRuntime.CustomInitialize = host =>
        {
            Slider slider = host.GetControl<Slider>("Control");
            slider.Minimum = 0;
            slider.Maximum = 1;
            slider.Value = 0.5;
            slider.SmallChange = 0.1;
            slider.LargeChange = 0.25;
        };
        LoadProject(wrapperCount, SliderBehavior(), controlComponent: "Controls/Slider");

        Slider slider = BuildAndFindControl<Slider>(wrapperCount);

        slider.Minimum.ShouldBe(0);
        slider.Maximum.ShouldBe(1);
        slider.Value.ShouldBe(0.5);
        slider.SmallChange.ShouldBe(0.1);
        slider.LargeChange.ShouldBe(0.25);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SliderWithNothingSetInCode_AncestorsBuilt_HasBehaviorDefaults(int wrapperCount)
    {
        LoadProject(wrapperCount, SliderBehavior(), controlComponent: "Controls/Slider");

        Slider slider = BuildAndFindControl<Slider>(wrapperCount);

        slider.Minimum.ShouldBe(0);
        slider.Maximum.ShouldBe(100);
        slider.Value.ShouldBe(0);
        slider.SmallChange.ShouldBe(5);
        slider.LargeChange.ShouldBe(25);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SliderRangeAuthoredOnTheComponentDefault_AncestorsBuilt_AppliesAuthoredRange(int wrapperCount)
    {
        LoadProject(wrapperCount, SliderBehavior(), controlComponent: "Controls/Slider",
            controlComponentVariables: new[] { Variable("double", "Maximum", 250.0) });

        Slider slider = BuildAndFindControl<Slider>(wrapperCount);

        slider.Maximum.ShouldBe(250);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SliderRangeAuthoredOnTheComponentDefault_CustomInitializeSetsIt_CodeWins(int wrapperCount)
    {
        HostRuntime.CustomInitialize = host => host.GetControl<Slider>("Control").Maximum = 1;
        LoadProject(wrapperCount, SliderBehavior(), controlComponent: "Controls/Slider",
            controlComponentVariables: new[] { Variable("double", "Maximum", 250.0) });

        Slider slider = BuildAndFindControl<Slider>(wrapperCount);

        slider.Maximum.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SliderRangeAuthoredOnTheHostInstance_AncestorsBuilt_AppliesAuthoredRange(int wrapperCount)
    {
        LoadProject(wrapperCount, SliderBehavior(), controlComponent: "Controls/Slider",
            hostVariables: new[] { Variable("double", "Control.Maximum", 7.0) });

        Slider slider = BuildAndFindControl<Slider>(wrapperCount);

        slider.Maximum.ShouldBe(7);
        slider.Minimum.ShouldBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SliderMaximumAuthoredOnTheHostInstance_CustomInitializeSetsOtherProperties_BothHold(int wrapperCount)
    {
        HostRuntime.CustomInitialize = host => host.GetControl<Slider>("Control").Minimum = -3;
        LoadProject(wrapperCount, SliderBehavior(), controlComponent: "Controls/Slider",
            hostVariables: new[] { Variable("double", "Control.Maximum", 7.0) });

        Slider slider = BuildAndFindControl<Slider>(wrapperCount);

        slider.Maximum.ShouldBe(7);
        slider.Minimum.ShouldBe(-3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void SliderMaximumAuthoredOnTheHostInstance_CustomInitializeSetsSameProperty_AuthoredValueWins(int wrapperCount)
    {
        HostRuntime.CustomInitialize = host => host.GetControl<Slider>("Control").Maximum = 1;
        LoadProject(wrapperCount, SliderBehavior(), controlComponent: "Controls/Slider",
            hostVariables: new[] { Variable("double", "Control.Maximum", 7.0) });

        Slider slider = BuildAndFindControl<Slider>(wrapperCount);

        slider.Maximum.ShouldBe(7);
    }

    private class RangedSliderRuntime : Gum.Forms.DefaultFromFileVisuals.DefaultFromFileSliderRuntime
    {
        public override void AfterFullCreation()
        {
            base.AfterFullCreation();
            FormsControl!.Maximum = 1;
        }
    }

    // The control's defaults are applied when the control is created, before the rest of the
    // built element's own AfterFullCreation, so code there keeps what it sets (#7197).
    [Fact]
    public void RootSliderBuiltDirectly_MaximumSetInAfterFullCreation_KeepsIt()
    {
        LoadProject(wrapperCount: 0, SliderBehavior(), controlComponent: "Controls/Slider");
        ElementSaveExtensions.RegisterGueInstantiationType("Controls/Slider", typeof(RangedSliderRuntime));
        ComponentSave component = ObjectFinder.Self.GumProjectSave!.Components.Single(item => item.Name == "Controls/Slider");

        GraphicalUiElement visual = component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        Slider slider = (Slider)((InteractiveGue)visual).FormsControlAsObject!;
        slider.Maximum.ShouldBe(1);
    }

    [Fact]
    public void RootSliderBuiltDirectly_RangeAuthoredOnTheComponent_AfterFullCreationSetsMaximum_CodeWinsAndOtherAuthoredValueApplies()
    {
        LoadProject(wrapperCount: 0, SliderBehavior(), controlComponent: "Controls/Slider",
            controlComponentVariables: new[]
            {
                Variable("double", "Minimum", -5.0),
                Variable("double", "Maximum", 250.0)
            });
        ElementSaveExtensions.RegisterGueInstantiationType("Controls/Slider", typeof(RangedSliderRuntime));
        ComponentSave component = ObjectFinder.Self.GumProjectSave!.Components.Single(item => item.Name == "Controls/Slider");

        GraphicalUiElement visual = component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        Slider slider = (Slider)((InteractiveGue)visual).FormsControlAsObject!;
        slider.Maximum.ShouldBe(1);
        slider.Minimum.ShouldBe(-5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void NestedSliderWithCustomRuntime_MaximumSetInAfterFullCreation_AncestorsBuilt_KeepsIt(int wrapperCount)
    {
        LoadProject(wrapperCount, SliderBehavior(), controlComponent: "Controls/Slider");
        ElementSaveExtensions.RegisterGueInstantiationType("Controls/Slider", typeof(RangedSliderRuntime));

        Slider slider = BuildAndFindControl<Slider>(wrapperCount);

        slider.Maximum.ShouldBe(1);
    }

    [Fact]
    public void NestedSliderWithCustomRuntime_MaximumAuthoredOnTheHostInstance_AuthoredValueWins()
    {
        LoadProject(wrapperCount: 0, SliderBehavior(), controlComponent: "Controls/Slider",
            hostVariables: new[] { Variable("double", "Control.Maximum", 7.0) });
        ElementSaveExtensions.RegisterGueInstantiationType("Controls/Slider", typeof(RangedSliderRuntime));

        Slider slider = BuildAndFindControl<Slider>(wrapperCount: 0);

        slider.Maximum.ShouldBe(7);
    }

    // Other controls -----------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void CheckBoxIsCheckedSetInCustomInitialize_AncestorsBuilt_StaysChecked(int wrapperCount)
    {
        HostRuntime.CustomInitialize = host => host.GetControl<CheckBox>("Control").IsChecked = true;
        LoadProject(wrapperCount, CheckBoxBehavior(), controlComponent: "Controls/CheckBox");

        CheckBox checkBox = BuildAndFindControl<CheckBox>(wrapperCount);

        checkBox.IsChecked.ShouldBe(true);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void ButtonToolTipAndIsEnabledSetInCustomInitialize_AncestorsBuilt_KeepsValues(int wrapperCount)
    {
        HostRuntime.CustomInitialize = host =>
        {
            Button button = host.GetControl<Button>("Control");
            button.ToolTip = "from code";
            button.IsEnabled = false;
        };
        LoadProject(wrapperCount, ButtonBehavior(), controlComponent: "Controls/Button");

        Button button = BuildAndFindControl<Button>(wrapperCount);

        button.ToolTip.ShouldBe("from code");
        button.IsEnabled.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void ButtonIsEnabledAuthoredFalseOnTheHostInstance_AncestorsBuilt_StaysDisabled(int wrapperCount)
    {
        LoadProject(wrapperCount, ButtonBehavior(), controlComponent: "Controls/Button",
            hostVariables: new[] { Variable("bool", "Control.IsEnabled", false) });

        Button button = BuildAndFindControl<Button>(wrapperCount);

        button.IsEnabled.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void ScrollBarRangeSetInCustomInitialize_AncestorsBuilt_KeepsValues(int wrapperCount)
    {
        HostRuntime.CustomInitialize = host =>
        {
            ScrollBar bar = host.GetControl<ScrollBar>("Control");
            bar.Maximum = 9;
            bar.SmallChange = 3;
        };
        LoadProject(wrapperCount, ScrollBarBehavior(), controlComponent: "Controls/ScrollBar");

        ScrollBar bar = BuildAndFindControl<ScrollBar>(wrapperCount);

        bar.Maximum.ShouldBe(9);
        bar.SmallChange.ShouldBe(3);
    }

    // A component that is itself the control ----------------------------------------------

    private class RangedSlider : Slider
    {
        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();
            Minimum = 0;
            Maximum = 1;
            Value = 0.5;
            SmallChange = 0.1;
            LargeChange = 0.25;
        }
    }

    [Fact]
    public void SliderSubclass_SetsRangeWhenVisualIsAssigned_KeepsRange()
    {
        RangedSlider slider = new RangedSlider();

        slider.Minimum.ShouldBe(0);
        slider.Maximum.ShouldBe(1);
        slider.Value.ShouldBe(0.5);
        slider.SmallChange.ShouldBe(0.1);
        slider.LargeChange.ShouldBe(0.25);
    }

    [Fact]
    public void Slider_NothingSetInCode_HasSliderDefaults()
    {
        Slider slider = new Slider();

        slider.Minimum.ShouldBe(0);
        slider.Maximum.ShouldBe(100);
        slider.SmallChange.ShouldBe(5);
        slider.LargeChange.ShouldBe(25);
    }

    [Fact]
    public void Slider_WithoutADefaultVisual_HasSliderDefaults()
    {
        Slider slider = CreateWithoutDefaultVisual(() => new Slider());

        slider.Maximum.ShouldBe(100);
        slider.SmallChange.ShouldBe(5);
        slider.LargeChange.ShouldBe(25);
    }

    [Fact]
    public void ScrollBar_WithoutADefaultVisual_IsVertical()
    {
        ScrollBar bar = CreateWithoutDefaultVisual(() => new ScrollBar());

        bar.Orientation.ShouldBe(Orientation.Vertical);
    }

    private static T CreateWithoutDefaultVisual<T>(Func<T> create)
    {
        Dictionary<Type, VisualTemplate> saved = new Dictionary<Type, VisualTemplate>(FrameworkElement.DefaultFormsTemplates);
        FrameworkElement.DefaultFormsTemplates.Clear();
        try
        {
            return create();
        }
        finally
        {
            foreach (KeyValuePair<Type, VisualTemplate> pair in saved)
            {
                FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value;
            }
        }
    }

    private class HorizontalScrollBar : ScrollBar
    {
        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();
            Orientation = Orientation.Horizontal;
        }
    }

    [Fact]
    public void ScrollBarSubclass_SetsOrientationWhenVisualIsAssigned_KeepsOrientation()
    {
        HorizontalScrollBar bar = new HorizontalScrollBar();

        bar.Orientation.ShouldBe(Orientation.Horizontal);
    }

    private class CheckedRadioButton : RadioButton
    {
        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();
            IsChecked = true;
        }
    }

    [Fact]
    public void RadioButtonSubclass_SetsIsCheckedWhenVisualIsAssigned_KeepsIsChecked()
    {
        CheckedRadioButton radio = new CheckedRadioButton();

        radio.IsChecked.ShouldBe(true);
    }

    private class CheckedToggleButton : ToggleButton
    {
        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();
            IsChecked = true;
        }
    }

    [Fact]
    public void ToggleButtonSubclass_SetsIsCheckedWhenVisualIsAssigned_KeepsIsChecked()
    {
        CheckedToggleButton toggle = new CheckedToggleButton();

        toggle.IsChecked.ShouldBe(true);
    }

    // Project construction -----------------------------------------------------------------

    private static VariableSave Variable(string type, string name, object value)
    {
        return new VariableSave { Type = type, Name = name, Value = value, SetsValue = true };
    }

    private static BehaviorSave SliderBehavior()
    {
        BehaviorSave behavior = new BehaviorSave { Name = StandardFormsBehaviorNames.SliderBehaviorName };
        behavior.FormsProperties.Add(new VariableSave { Type = "double", Name = "Minimum", Value = 0.0 });
        behavior.FormsProperties.Add(new VariableSave { Type = "double", Name = "Maximum", Value = 100.0 });
        behavior.FormsProperties.Add(new VariableSave { Type = "double", Name = "Value", Value = 0.0 });
        behavior.FormsProperties.Add(new VariableSave { Type = "double", Name = "SmallChange", Value = 5.0 });
        behavior.FormsProperties.Add(new VariableSave { Type = "double", Name = "LargeChange", Value = 25.0 });
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        return behavior;
    }

    private static BehaviorSave ScrollBarBehavior()
    {
        BehaviorSave behavior = new BehaviorSave { Name = StandardFormsBehaviorNames.ScrollBarBehaviorName };
        behavior.FormsProperties.Add(new VariableSave { Type = "double", Name = "Maximum", Value = 100.0 });
        behavior.FormsProperties.Add(new VariableSave { Type = "double", Name = "SmallChange", Value = 10.0 });
        return behavior;
    }

    private static BehaviorSave CheckBoxBehavior()
    {
        BehaviorSave behavior = new BehaviorSave { Name = StandardFormsBehaviorNames.CheckBoxBehaviorName };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsChecked", Value = false });
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        return behavior;
    }

    private static BehaviorSave ButtonBehavior()
    {
        BehaviorSave behavior = new BehaviorSave { Name = StandardFormsBehaviorNames.ButtonBehaviorName };
        behavior.FormsProperties.Add(new VariableSave { Type = "string", Name = "ToolTip", Value = "default tip" });
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        return behavior;
    }

    /// <summary>
    /// Screen -> Wrapper1 -> ... -> WrapperN -> Host -> Control (an instance of controlComponent).
    /// With no wrappers the screen holds the Host directly.
    /// </summary>
    private static void LoadProject(
        int wrapperCount,
        BehaviorSave behavior,
        string controlComponent,
        IEnumerable<VariableSave>? controlComponentVariables = null,
        IEnumerable<VariableSave>? hostVariables = null)
    {
        GumProjectSave project = new GumProjectSave();
        project.Behaviors.Add(behavior);

        // The standard must exist in the project for ObjectFinder to resolve the child instances.
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(new StateSave { Name = "Default", ParentContainer = container });
        project.StandardElements.Add(container);

        ComponentSave control = new ComponentSave { Name = controlComponent, BaseType = "Container" };
        StateSave controlState = new StateSave { Name = "Default", ParentContainer = control };
        foreach (VariableSave variable in controlComponentVariables ?? Array.Empty<VariableSave>())
        {
            controlState.Variables.Add(variable);
        }
        control.States.Add(controlState);
        if (behavior.Name == StandardFormsBehaviorNames.SliderBehaviorName
            || behavior.Name == StandardFormsBehaviorNames.ScrollBarBehaviorName)
        {
            // The thumb and track must be InteractiveGues, which a Button-behavior component is.
            ComponentSave part = new ComponentSave { Name = "Controls/Part", BaseType = "Container" };
            part.States.Add(new StateSave { Name = "Default", ParentContainer = part });
            part.Behaviors.Add(new ElementBehaviorReference { BehaviorName = StandardFormsBehaviorNames.ButtonBehaviorName });
            project.Components.Add(part);
            project.Behaviors.Add(new BehaviorSave { Name = StandardFormsBehaviorNames.ButtonBehaviorName });

            foreach (string childName in new[] { "ThumbInstance", "TrackInstance" })
            {
                control.Instances.Add(new InstanceSave
                {
                    Name = childName,
                    BaseType = part.Name,
                    ParentContainer = control
                });
            }
        }
        control.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behavior.Name });
        project.Components.Add(control);

        ComponentSave host = AddComponentHolding(project, HostName, "Control", controlComponent);
        foreach (VariableSave variable in hostVariables ?? Array.Empty<VariableSave>())
        {
            host.DefaultState.Variables.Add(variable);
        }

        string innerName = HostName;
        for (int i = 1; i <= wrapperCount; i++)
        {
            string wrapperName = "Wrapper" + i;
            AddComponentHolding(project, wrapperName, "Inner", innerName);
            innerName = wrapperName;
        }

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Inner", BaseType = innerName, ParentContainer = screen });
        project.Screens.Add(screen);

        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;

        FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        ElementSaveExtensions.RegisterGueInstantiationType(HostName, typeof(HostRuntime));
    }

    private static ComponentSave AddComponentHolding(
        GumProjectSave project, string name, string instanceName, string instanceBaseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Instances.Add(new InstanceSave
        {
            Name = instanceName,
            BaseType = instanceBaseType,
            ParentContainer = component
        });
        project.Components.Add(component);
        return component;
    }

    private static T BuildAndFindControl<T>(int wrapperCount) where T : FrameworkElement
    {
        ScreenSave screen = ObjectFinder.Self.GumProjectSave!.Screens.Single();
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        GraphicalUiElement current = screenVisual.GetGraphicalUiElementByName("Inner")!;
        for (int i = 0; i < wrapperCount; i++)
        {
            current = current.GetGraphicalUiElementByName("Inner")!;
        }

        return (T)((InteractiveGue)current.GetGraphicalUiElementByName("Control")!).FormsControlAsObject!;
    }
}
