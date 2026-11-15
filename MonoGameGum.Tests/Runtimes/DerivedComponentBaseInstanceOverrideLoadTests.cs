using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// The runtime loader applies a derived component's overrides of instances its base owns (#7093), so only
/// the generated code ever lacked them.
/// </summary>
public class DerivedComponentBaseInstanceOverrideLoadTests : BaseTestClass
{
    public DerivedComponentBaseInstanceOverrideLoadTests()
    {
        StandardElementsManager.Self.Initialize();
    }

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static ComponentSave AddComponent(GumProjectSave project, string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        project.Components.Add(component);
        return component;
    }

    [Fact]
    public void ToGraphicalUiElement_DerivedOverridesVariablesOfInstanceOwnedByGrandparent_AppliesThem()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container")!);
        container.DefaultState!.ParentContainer = container;
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;
        StandardRuntimeRegistrations.Register();

        ComponentSave grand = AddComponent(project, "Grand", "Container");
        grand.Instances.Add(new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = grand });
        ComponentSave middle = AddComponent(project, "Middle", "Grand");
        middle.Instances.Add(new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = middle, DefinedByBase = true });
        middle.DefaultState.Variables.Add(new VariableSave { Name = "Holder.Width", Type = "float", Value = 111f, SetsValue = true });
        ComponentSave leaf = AddComponent(project, "Leaf", "Middle");
        leaf.Instances.Add(new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = leaf, DefinedByBase = true });
        leaf.DefaultState.Variables.Add(new VariableSave { Name = "Holder.Height", Type = "float", Value = 222f, SetsValue = true });
        leaf.DefaultState.Variables.Add(new VariableSave { Name = "Holder.IsRenderTarget", Type = "bool", Value = true, SetsValue = true });

        GraphicalUiElement runtime = leaf.ToGraphicalUiElement();

        GraphicalUiElement holder = runtime.GetGraphicalUiElementByName("Holder")!;
        holder.Width.ShouldBe(111f);
        holder.Height.ShouldBe(222f);
        ((ContainerRuntime)holder).IsRenderTarget.ShouldBeTrue();
    }
}
