using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The tree's Duplicate on a component copies the whole element under the new name and points every state and
/// instance at the copy.
/// </summary>
[Trait("Category", "EndToEnd")]
public class ComponentDuplicateScenarioTests
{
    [AvaloniaFact]
    public void DuplicateComponent_FromTheTreeMenu_CopiesEverythingButTheName()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.Project.AddInstance(card, "Body", "Container");
        tree.Project.AddInstance(card, "Title", "Text");
        StateSave defaultState = card.DefaultState!;
        defaultState.SetValue("Title.Parent", "Body", "string");
        defaultState.SetValue("Title.Text", "Hello", "string");
        defaultState.GetVariableSave("Title.Text")!.ExposedAsName = "TitleText";
        defaultState.VariableLists.Add(new VariableListSave<string>
        {
            Name = "Title.VariableReferences",
            Type = "string",
            ValueAsIList = new List<string> { "Width = Body.Width" },
        });
        StateSaveCategory size = tree.Project.AddCategory(card, "Size");
        tree.Project.AddState(card, size, "Big");
        size.States.Single().SetValue("Width", 200f, "float");
        tree.Project.Project.Behaviors.Add(new BehaviorSave { Name = "CardBehavior" });
        card.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "CardBehavior" });
        tree.Click(tree.NodeFor(card));

        tree.Dialogs.AnswerNextUserString("CardCopy");
        tree.RightClick(tree.NodeFor(card));
        tree.PickMenu("Duplicate Card");

        ComponentSave copy = tree.Project.Project.Components.Single(component => component.Name == "CardCopy");
        Serialize(copy).ShouldBe(Serialize(card).Replace("<Name>Card</Name>", "<Name>CardCopy</Name>"));
        copy.AllStates.ShouldAllBe(state => state.ParentContainer == copy);
        copy.Instances.ShouldAllBe(instance => instance.ParentContainer == copy);
    }

    private static string Serialize(ComponentSave component)
    {
        FileManager.XmlSerialize(component, out string text);
        return text;
    }
}
