using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Pins that a Button, CheckBox or RadioButton authored disabled in the Gum tool (the Forms
/// "IsEnabled" property, a FormsProperty of the control's behavior) loads disabled at runtime and
/// stays disabled across RefreshStyles and a Visual swap. The component and behavior go through
/// the same compact XML the tool writes, then through the runtime's file-load path.
/// </summary>
public class AuthoredDisabledFormsControlTests : BaseTestClass
{
    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Theory]
    [InlineData(StandardFormsBehaviorNames.ButtonBehaviorName)]
    [InlineData(StandardFormsBehaviorNames.CheckBoxBehaviorName)]
    [InlineData(StandardFormsBehaviorNames.RadioButtonBehaviorName)]
    public void ComponentDisabledInDefaultState_LoadedAsRoot_StartsDisabled(string behaviorName)
    {
        ComponentSave component = LoadProjectWithAuthoredComponent(
            behaviorName, authorDisabledOnComponent: true, instanceOverride: null);

        InteractiveGue visual = component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false)
            .ShouldBeAssignableTo<InteractiveGue>();

        FrameworkElement control = visual.FormsControlAsObject.ShouldBeAssignableTo<FrameworkElement>()!;
        control.IsEnabled.ShouldBeFalse();
        visual.IsEnabled.ShouldBeFalse();
    }

    [Theory]
    [InlineData(StandardFormsBehaviorNames.ButtonBehaviorName)]
    [InlineData(StandardFormsBehaviorNames.CheckBoxBehaviorName)]
    [InlineData(StandardFormsBehaviorNames.RadioButtonBehaviorName)]
    public void InstanceDisabledOnScreen_Loaded_StartsDisabledAndStaysDisabledAfterRefreshStylesAndSwap(
        string behaviorName)
    {
        LoadProjectWithAuthoredComponent(behaviorName, authorDisabledOnComponent: false, instanceOverride: false);
        ScreenSave screen = ObjectFinder.Self.GumProjectSave!.Screens.Single();

        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        InteractiveGue visual = screenVisual.Children.OfType<InteractiveGue>().ShouldHaveSingleItem();
        FrameworkElement control = visual.FormsControlAsObject.ShouldBeAssignableTo<FrameworkElement>()!;

        control.IsEnabled.ShouldBeFalse();
        visual.IsEnabled.ShouldBeFalse();

        screenVisual.RefreshStyles();

        control.IsEnabled.ShouldBeFalse();
        visual.IsEnabled.ShouldBeFalse();

        InteractiveGue newVisual = CreateDefaultVisual(behaviorName);
        control.Visual = newVisual;

        control.IsEnabled.ShouldBeFalse();
        newVisual.IsEnabled.ShouldBeFalse();
    }

    private static InteractiveGue CreateDefaultVisual(string behaviorName)
    {
        return behaviorName switch
        {
            StandardFormsBehaviorNames.CheckBoxBehaviorName =>
                new CheckBoxVisual(fullInstantiation: true, tryCreateFormsObject: false),
            StandardFormsBehaviorNames.RadioButtonBehaviorName =>
                new RadioButtonVisual(fullInstantiation: true, tryCreateFormsObject: false),
            _ => new ButtonVisual(fullInstantiation: true, tryCreateFormsObject: false)
        };
    }

    /// <summary>
    /// Builds a project the way a tool-authored one loads: a behavior declaring the IsEnabled
    /// FormsProperty and a component using it, both round-tripped through the compact XML, plus a
    /// screen with one instance of the component. Returns the component as loaded.
    /// </summary>
    private static ComponentSave LoadProjectWithAuthoredComponent(
        string behaviorName, bool authorDisabledOnComponent, bool? instanceOverride)
    {
        BehaviorSave behavior = new BehaviorSave { Name = behaviorName };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });

        ComponentSave component = new ComponentSave { Name = "Controls/Authored", BaseType = "Container" };
        StateSave componentDefault = new StateSave { Name = "Default", ParentContainer = component };
        if (authorDisabledOnComponent)
        {
            componentDefault.Variables.Add(new VariableSave
            {
                Type = "bool",
                Name = "IsEnabled",
                Value = false,
                SetsValue = true
            });
        }
        component.Instances.Add(new InstanceSave
        {
            Name = "TextInstance",
            BaseType = "Text",
            ParentContainer = component
        });
        componentDefault.Variables.Add(new VariableSave
        {
            Type = "string",
            Name = "TextInstance.Text",
            Value = "Authored",
            SetsValue = true
        });
        component.States.Add(componentDefault);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });

        ScreenSave screen = new ScreenSave { Name = "AuthoredScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.Instances.Add(new InstanceSave
        {
            Name = "ControlInstance",
            BaseType = component.Name,
            ParentContainer = screen
        });
        if (instanceOverride is bool isEnabled)
        {
            screenDefault.Variables.Add(new VariableSave
            {
                Type = "bool",
                Name = "ControlInstance.IsEnabled",
                Value = isEnabled,
                SetsValue = true
            });
        }
        screen.States.Add(screenDefault);

        int version = (int)GumProjectSave.GumxVersions.AttributeVersion;
        BehaviorSave loadedBehavior = GumFileSerializer.DeserializeBehaviorSave(
            RoundTripXml(behavior), version)!;
        ComponentSave loadedComponent = GumFileSerializer.DeserializeElementSave<ComponentSave>(
            RoundTripXml(component), version)!;
        ScreenSave loadedScreen = GumFileSerializer.DeserializeElementSave<ScreenSave>(
            RoundTripXml(screen), version)!;

        GumProjectSave project = new GumProjectSave();
        project.Behaviors.Add(loadedBehavior);
        project.Components.Add(loadedComponent);
        project.Screens.Add(loadedScreen);
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;

        FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        return loadedComponent;
    }

    private static string RoundTripXml(object element)
    {
        XmlSerializer serializer = GumFileSerializer.GetCompactSerializer(element.GetType());
        XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
        namespaces.Add("xsd", "http://www.w3.org/2001/XMLSchema");
        namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        using StringWriter writer = new StringWriter();
        serializer.Serialize(writer, element, namespaces);
        return writer.ToString();
    }
}
