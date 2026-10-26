using System.Collections.Generic;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Shouldly;

namespace Gum.Bundle.Tests;

public class GumElementReachabilityTests
{
    [Fact]
    public void Compute_keeps_screens_and_their_instance_types_and_trims_the_rest()
    {
        ScreenSave screen = TestProjectBuilder.BuildScreen("Game");
        screen.Instances.Add(new InstanceSave { Name = "Card1", BaseType = "Card", ParentContainer = screen });
        ComponentSave card = TestProjectBuilder.BuildComponent("Card");
        card.Instances.Add(new InstanceSave { Name = "Face", BaseType = "CardFace", ParentContainer = card });
        ComponentSave cardFace = TestProjectBuilder.BuildComponent("CardFace");
        ComponentSave unused = TestProjectBuilder.BuildComponent("Unused");
        StandardElementSave sprite = TestProjectBuilder.BuildStandard("Sprite");
        GumProjectSave project = TestProjectBuilder.BuildProject(
            screens: new[] { screen }, components: new[] { card, cardFace, unused }, standards: new[] { sprite });

        GumElementReachabilityResult result = new GumElementReachability().Compute(project, extraRoots: new string[0]);

        result.KeptElementNames.ShouldBe(new[] { "Card", "CardFace", "Game", "Sprite" }, ignoreOrder: true);
        result.TrimmedElementNames.ShouldBe(new[] { "Unused" });
        result.UnknownRoots.ShouldBeEmpty();
    }

    [Fact]
    public void Compute_follows_extra_roots_base_types_and_cross_element_references()
    {
        ComponentSave card = TestProjectBuilder.BuildComponent("Card");
        card.BaseType = "CardBase";
        ComponentSave cardBase = TestProjectBuilder.BuildComponent("CardBase");
        VariableListSave<string> references = new VariableListSave<string>
        {
            Type = "string",
            Name = "VariableReferences",
            Value = new List<string> { "Width = Components/Styles.CardWidth" },
        };
        cardBase.DefaultState.VariableLists.Add(references);
        ComponentSave styles = TestProjectBuilder.BuildComponent("Styles");
        ComponentSave unused = TestProjectBuilder.BuildComponent("Unused");
        GumProjectSave project = TestProjectBuilder.BuildProject(components: new[] { card, cardBase, styles, unused });

        GumElementReachabilityResult result = new GumElementReachability().Compute(project, extraRoots: new[] { "Card", "Misspelled" });

        result.KeptElementNames.ShouldBe(new[] { "Card", "CardBase", "Styles" }, ignoreOrder: true);
        result.TrimmedElementNames.ShouldBe(new[] { "Unused" });
        result.UnknownRoots.ShouldBe(new[] { "Misspelled" });
    }

    [Fact]
    public void Compute_keeps_the_components_Forms_creates_on_its_own_for_a_kept_control()
    {
        ScreenSave screen = TestProjectBuilder.BuildScreen("Menu");
        screen.Instances.Add(new InstanceSave { Name = "List", BaseType = "ListBox", ParentContainer = screen });
        ComponentSave listBox = TestProjectBuilder.BuildComponent("ListBox");
        listBox.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ListBoxBehavior" });
        ComponentSave listBoxItem = TestProjectBuilder.BuildComponent("ListBoxItem");
        listBoxItem.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ListBoxItemBehavior" });
        ComponentSave tooltip = TestProjectBuilder.BuildComponent("Tooltip");
        tooltip.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "TooltipBehavior" });
        ComponentSave menuItem = TestProjectBuilder.BuildComponent("MenuItem");
        menuItem.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "MenuItemBehavior" });
        BehaviorSave listBoxItemBehavior = TestProjectBuilder.BuildBehavior("ListBoxItemBehavior");
        listBoxItemBehavior.DefaultImplementation = "ListBoxItem";
        GumProjectSave project = TestProjectBuilder.BuildProject(
            screens: new[] { screen },
            components: new[] { listBox, listBoxItem, tooltip, menuItem },
            behaviors: new[] { listBoxItemBehavior });

        GumElementReachabilityResult result = new GumElementReachability().Compute(project, extraRoots: new string[0]);

        result.KeptElementNames.ShouldBe(new[] { "Menu", "ListBox", "ListBoxItem", "Tooltip" }, ignoreOrder: true);
        result.TrimmedElementNames.ShouldBe(new[] { "MenuItem" });
    }
}
