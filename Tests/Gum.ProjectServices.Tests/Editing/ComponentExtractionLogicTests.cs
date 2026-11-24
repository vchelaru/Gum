using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

/// <summary>
/// Create Component on MyButton in MainScreen: MyButton (X 50, Y 120, Width 200) with ChildText attached to it,
/// GrandChild attached to ChildText, and an UnrelatedSibling outside the subtree.
/// </summary>
public class ComponentExtractionLogicTests : BaseTestClass
{
    private readonly ScreenSave _screen;
    private readonly InstanceSave _myButton;
    private readonly StateSave _default;

    public ComponentExtractionLogicTests()
    {
        _screen = EditingTestProject.AddScreen(Project, "MainScreen");
        _default = _screen.GetDefaultStateOrThrow();
        _myButton = EditingTestProject.AddInstance(_screen, "MyButton", "Container");
        EditingTestProject.AddInstance(_screen, "ChildText", "Text");
        EditingTestProject.AddInstance(_screen, "GrandChild", "Container");
        EditingTestProject.AddInstance(_screen, "UnrelatedSibling", "Text");
        _default.SetValue("ChildText.Parent", "MyButton", "string");
        _default.SetValue("GrandChild.Parent", "ChildText", "string");
        _default.SetValue("MyButton.X", 50f, "float");
        _default.SetValue("MyButton.Y", 120f, "float");
        _default.SetValue("MyButton.Width", 200f, "float");
        _default.SetValue("ChildText.Text", "Hello", "string");
        _default.SetValue("UnrelatedSibling.Text", "Unrelated", "string");
    }

    private static ComponentExtractionLogic CreateLogic() => new ComponentExtractionLogic(new ElementCreationLogic());

    private static VariableListSave<string> List(string name, params string[] items)
    {
        VariableListSave<string> list = new VariableListSave<string> { Name = name, Type = "string" };
        foreach (string item in items)
        {
            list.ValueAsIList.Add(item);
        }
        return list;
    }

    [Fact]
    public void CreateComponent_PromotesTheInstanceAndCopiesItsSubtree_LeavingTheSourceAlone()
    {
        _default.VariableLists.Add(List("ChildText.Points", "1"));
        _default.VariableLists.Add(List("ChildText.VariableReferences", "FontSize = MyButton.Height"));
        _default.VariableLists.Add(List("MyButton.VariableReferences", "Width = UnrelatedSibling.Width"));
        _default.SetValue("ChildText.FontSize", 28, "int");
        _default.GetVariableSave("ChildText.Text")!.ExposedAsName = "Caption";
        int sourceVariableCount = _default.Variables.Count;

        ComponentExtraction extraction = CreateLogic().CreateComponent(_myButton, "Controls/ButtonComponent");

        ComponentSave component = extraction.Component;
        component.Name.ShouldBe("Controls/ButtonComponent");
        component.BaseType.ShouldBe("Container");
        component.Instances.Select(item => item.Name).ShouldBe(new[] { "ChildText", "GrandChild" });
        component.Instances.ShouldAllBe(item => item.ParentContainer == component);
        extraction.MovedInstances.Select(item => item.Name).ShouldBe(new[] { "ChildText", "GrandChild" });
        StateSave componentDefault = component.GetDefaultStateOrThrow();
        componentDefault.GetValue("Width").ShouldBe(200f);
        componentDefault.Variables.ShouldNotContain(item => (item.Name == "X" || item.Name == "Y") && item.SetsValue);
        componentDefault.GetVariableSave("ChildText.Parent").ShouldBeNull();
        componentDefault.GetValue("GrandChild.Parent").ShouldBe("ChildText");
        componentDefault.GetVariableSave("ChildText.Text")!.ExposedAsName.ShouldBe("Caption");
        componentDefault.GetValue("ChildText.FontSize").ShouldBe(28);
        componentDefault.GetVariableSave("UnrelatedSibling.Text").ShouldBeNull();
        componentDefault.VariableLists.Select(item => item.Name).ShouldBe(new[] { "ChildText.Points" });
        Project.Components.ShouldNotContain(component);
        _screen.Instances.Count.ShouldBe(4);
        _default.Variables.Count.ShouldBe(sourceVariableCount);
    }

    [Fact]
    public void ReplaceWithComponentInstance_PutsOneInstanceInTheSubtreesPlace_KeepingWhatStillAppliesToIt()
    {
        InstanceSave first = new InstanceSave { Name = "First", BaseType = "Text", ParentContainer = _screen };
        _screen.Instances.Insert(0, first);
        StateSaveCategory size = EditingTestProject.AddCategory(_screen, "Size", "Big");
        StateSave big = size.States[0];
        big.SetValue("MyButton.Width", 300f, "float");
        big.SetValue("ChildText.Text", "Big", "string");
        big.VariableLists.Add(List("MyButton.VariableReferences", "Height = First.Height"));
        big.SetValue("UnrelatedSibling.Parent", "MyButton", "string");
        ComponentExtractionLogic logic = CreateLogic();
        ComponentExtraction extraction = logic.CreateComponent(_myButton, "ButtonComponent");
        List<string> removed = new List<string>();

        InstanceSave replacement = logic.ReplaceWithComponentInstance(extraction, instance =>
        {
            removed.Add(instance.Name);
            new InstanceRemovalLogic(EditingTestProject.CreateReferenceFinder(), new VariableOverrideRemover())
                .RemoveInstance(instance, _screen);
        });

        removed.ShouldBe(new[] { "ChildText", "GrandChild", "MyButton" });
        _screen.Instances.Select(item => item.Name).ShouldBe(new[] { "First", "MyButton", "UnrelatedSibling" });
        replacement.ShouldBeSameAs(_screen.Instances[1]);
        replacement.BaseType.ShouldBe("ButtonComponent");
        replacement.ParentContainer.ShouldBe(_screen);
        _default.GetValue("MyButton.X").ShouldBe(50f);
        _default.GetValue("MyButton.Y").ShouldBe(120f);
        _default.GetVariableSave("MyButton.Width").ShouldBeNull();
        big.GetValue("MyButton.Width").ShouldBe(300f);
        big.GetVariableSave("ChildText.Text").ShouldBeNull();
        big.VariableLists.ShouldHaveSingleItem().Name.ShouldBe("MyButton.VariableReferences");
        big.GetValue("UnrelatedSibling.Parent").ShouldBe("MyButton");
    }
}
