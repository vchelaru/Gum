using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

/// <summary>
/// Create Component moving Label (a child of Panel) out of Hud. The tool's own tests in
/// MovedInstanceReferenceDropperTests cover each kind of reference and undo; these pin the data change alone.
/// </summary>
public class MovedInstanceReferenceDropLogicTests : BaseTestClass
{
    [Fact]
    public void FindThenDrop_RemovesWhatNamesTheMovedInstance_AndReportsIt()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        InstanceSave panel = EditingTestProject.AddInstance(hud, "Panel", "Container");
        InstanceSave label = EditingTestProject.AddInstance(hud, "Label", "Text");
        EditingTestProject.AddInstance(hud, "Caption", "Text");
        StateSave hudDefault = hud.GetDefaultStateOrThrow();
        hudDefault.SetValue("Label.Parent", "Panel", "string");
        hudDefault.Variables.Add(new VariableSave { Name = "Label.Text", Type = "string", Value = "Hi", ExposedAsName = "LabelText" });
        hudDefault.SetValue("Caption.X", 5f, "float");
        VariableListSave<string> captionReferences = new VariableListSave<string> { Name = "Caption.VariableReferences", Type = "string" };
        captionReferences.ValueAsIList.Add("X = Label.X");
        hudDefault.VariableLists.Add(captionReferences);
        StateSave big = EditingTestProject.AddCategory(hud, "Size", "Big").States[0];
        big.SetValue("Label.Text", "Big", "string");
        ScreenSave game = EditingTestProject.AddScreen(Project, "Game");
        EditingTestProject.AddInstance(game, "HudInstance", "Hud");
        game.GetDefaultStateOrThrow().SetValue("HudInstance.LabelText", "Yo", "string");
        MovedInstanceReferenceDropLogic logic = new MovedInstanceReferenceDropLogic(EditingTestProject.CreateReferenceFinder());

        MovedInstanceReferenceDrops drops = logic.Find(hud, panel, new[] { label }, animations: null);
        logic.Drop(drops);

        drops.Reports.Select(item => item.Description).ShouldBe(new[]
        {
            "Hud (Default): Caption.VariableReferences line \"X = Label.X\"",
        }, ignoreOrder: true);
        hudDefault.VariableLists.ShouldBeEmpty();
        hudDefault.GetValue("Caption.X").ShouldBe(5f);
        // Label.Text is exposed, so the replacement carries it as Panel.LabelText; nothing set through it is dropped.
        big.GetValue("Label.Text").ShouldBe("Big");
        game.GetDefaultStateOrThrow().GetValue("HudInstance.LabelText").ShouldBe("Yo");
        hudDefault.GetValue("Label.Text").ShouldBe("Hi");
    }
}
