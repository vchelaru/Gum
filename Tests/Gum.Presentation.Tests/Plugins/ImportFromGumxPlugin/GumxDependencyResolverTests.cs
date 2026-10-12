using Gum.DataTypes;
using Gum.DataTypes.Variables;
using ImportFromGumxPlugin.Services;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Presentation.Tests.Plugins.ImportFromGumxPlugin;

public class GumxDependencyResolverTests
{
    [Fact]
    public void ComputeTransitive_IncludesComponentsReadByVariableReferences_Transitively()
    {
        GumProjectSave source = new GumProjectSave();
        ComponentSave button = AddComponent(source, "Controls/Button");
        // An instance-level reference, as the Forms themes write them.
        button.Instances.Add(new InstanceSave { Name = "Background", BaseType = "NineSlice", ParentContainer = button });
        AddReferences(button, "Background.VariableReferences", "Color = Components/Styles.Primary.FillColor");
        ComponentSave styles = AddComponent(source, "Styles");
        AddReferences(styles, "VariableReferences", "Width = Components/Palette.Width");
        AddComponent(source, "Palette");
        AddComponent(source, "Unrelated");

        DependencySet result = new GumxDependencyResolver().ComputeTransitive(
            new List<ElementSave> { button }, source, new GumProjectSave());

        // In import order: a component comes after the ones it reads from.
        result.TransitiveComponents.Select(component => component.Name).ShouldBe(new[] { "Palette", "Styles" });
    }

    [Fact]
    public void ComputeTransitive_ListsTheUsersOfEachDifferingStandard_AndOnlyDifferingOnes()
    {
        GumProjectSave source = new GumProjectSave();
        GumProjectSave destination = new GumProjectSave();
        AddStandard(source, "Text").Categories.Add(new StateSaveCategory { Name = "TextColor" });
        AddStandard(destination, "Text");
        AddStandard(source, "Sprite");
        AddStandard(destination, "Sprite");
        ComponentSave label = AddComponent(source, "Label");
        label.Instances.Add(new InstanceSave { Name = "Text", BaseType = "Text", ParentContainer = label });
        label.Instances.Add(new InstanceSave { Name = "Icon", BaseType = "Sprite", ParentContainer = label });
        ComponentSave button = AddComponent(source, "Button");
        button.Instances.Add(new InstanceSave { Name = "Text", BaseType = "Text", ParentContainer = button });

        DependencySet result = new GumxDependencyResolver().ComputeTransitive(
            new List<ElementSave> { label, button }, source, destination);

        result.DifferingStandardUsers.Keys.Select(standard => standard.Name).ShouldBe(new[] { "Text" });
        result.DifferingStandardUsers.Values.Single().ShouldBe(new[] { "Button", "Label" });
    }

    [Fact]
    public void SortLeavesFirst_PutsBaseTypesInstancesAndReferencedComponentsBeforeTheOnesThatUseThem()
    {
        GumProjectSave source = new GumProjectSave();
        ComponentSave panel = AddComponent(source, "Panel");
        ComponentSave list = AddComponent(source, "List");
        ComponentSave fancyList = AddComponent(source, "FancyList");
        fancyList.BaseType = "List";
        ComponentSave label = AddComponent(source, "Label");
        panel.Instances.Add(new InstanceSave { Name = "Items", BaseType = "FancyList", ParentContainer = panel });
        list.Instances.Add(new InstanceSave { Name = "Caption", BaseType = "Label", ParentContainer = list });
        ComponentSave styles = AddComponent(source, "Styles");
        AddReferences(label, "VariableReferences", "Width = Components/Styles.Width");

        IReadOnlyList<ComponentSave> sorted = new GumxDependencyResolver().SortLeavesFirst(
            new List<ComponentSave> { panel, fancyList, list, label, styles }, source);

        List<string> names = sorted.Select(component => component.Name).ToList();
        names.Count.ShouldBe(5);
        names.IndexOf("Styles").ShouldBeLessThan(names.IndexOf("Label"));
        names.IndexOf("Label").ShouldBeLessThan(names.IndexOf("List"));
        names.IndexOf("List").ShouldBeLessThan(names.IndexOf("FancyList"));
        names.IndexOf("FancyList").ShouldBeLessThan(names.IndexOf("Panel"));
    }

    private static StandardElementSave AddStandard(GumProjectSave project, string name)
    {
        StandardElementSave standard = new StandardElementSave { Name = name };
        standard.States.Add(new StateSave { Name = "Default", ParentContainer = standard });
        project.StandardElements.Add(standard);
        return standard;
    }

    private static ComponentSave AddComponent(GumProjectSave project, string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        project.Components.Add(component);
        return component;
    }

    private static void AddReferences(ComponentSave component, string listName, string line)
    {
        VariableListSave<string> references = new VariableListSave<string> { Name = listName, Type = "string" };
        references.ValueAsIList.Add(line);
        component.States.Single().VariableLists.Add(references);
    }
}
