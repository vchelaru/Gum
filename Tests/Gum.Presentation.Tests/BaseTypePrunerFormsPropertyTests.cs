using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Services;
using Gum.Managers;
using Gum.ProjectServices.Editing;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// A base type change keeps a stored Forms property (a Slider's Maximum) when the new type's behaviors still
/// declare it, and prunes it when they do not.
/// </summary>
public class BaseTypePrunerFormsPropertyTests : BaseTestClass
{
    private readonly GumProjectSave _project = new GumProjectSave();
    private readonly BaseTypeVariablePruner _pruner;

    public BaseTypePrunerFormsPropertyTests()
    {
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_project);
        ObjectFinder.Self.GumProjectSave = _project;
        _pruner = new BaseTypeVariablePruner(
            new BaseTypeVariableRemover(new VariableDefinedByTypeLogic(), new Mock<IReferenceFinder>().Object, new VariableOverrideRemover()),
            new Mock<IInstanceOverrideRemover>().Object);
    }

    private ComponentSave AddComponent(string name, string? behavior)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        if (behavior != null)
        {
            component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behavior });
        }
        _project.Components.Add(component);
        return component;
    }

    [Fact]
    public void PruneInstance_FormsPropertyTheNewTypeStillDeclares_IsKeptThroughTheBaseChainAndPrunedWhenTheNewTypeLacksIt()
    {
        BehaviorSave sliderBehavior = new BehaviorSave { Name = "SliderBehavior" };
        sliderBehavior.FormsProperties.Add(new VariableSave { Name = "Maximum", Type = "double" });
        _project.Behaviors.Add(sliderBehavior);
        AddComponent("Slider", "SliderBehavior");
        AddComponent("Derived", null).BaseType = "Slider";
        AddComponent("Panel", null);
        ComponentSave screen = AddComponent("Host", null);
        InstanceSave instance = new InstanceSave { Name = "Thing", BaseType = "Derived", ParentContainer = screen };
        screen.Instances.Add(instance);
        screen.DefaultState!.SetValue("Thing.Maximum", 10.0, instance, "double");
        _pruner.PruneInstance(screen, instance);
        screen.DefaultState.Variables.ShouldContain(item => item.Name == "Thing.Maximum");

        instance.BaseType = "Panel";
        _pruner.PruneInstance(screen, instance);
        screen.DefaultState.Variables.ShouldNotContain(item => item.Name == "Thing.Maximum");
    }
}
