using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class StateVariableWriterTests : BaseTestClass
{
    [Fact]
    public void SetVariable_UnsetInstanceVariable_TakesTheTypeFromTheRootStandard()
    {
        Project.StandardElements.Single(item => item.Name == "Text").DefaultState.Variables
            .Add(new VariableSave { Name = "FontSize", Type = "int", Value = 18 });
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        InstanceSave counter = new InstanceSave { Name = "Counter1", BaseType = "Text", ParentContainer = hud };
        hud.Instances.Add(counter);
        StateVariableWriter writer = new StateVariableWriter();

        writer.ResolveVariableType(hud, hud.DefaultState, "Counter1.FontSize").ShouldBe("int");
        writer.SetVariable(hud, hud.DefaultState, "Counter1.FontSize", 30, counter, fallbackType: null);

        VariableSave written = hud.DefaultState.GetVariableSave("Counter1.FontSize")!;
        written.Value.ShouldBe(30);
        written.Type.ShouldBe("int");
    }

    [Fact]
    public void SetVariable_ExposedVariableInANamedState_KeepsTheExposedName()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        hud.Instances.Add(new InstanceSave { Name = "Counter1", BaseType = "Text", ParentContainer = hud });
        hud.DefaultState.Variables.Add(new VariableSave { Name = "Counter1.Text", Type = "string", ExposedAsName = "Counter1Text", SetsValue = false });
        StateSave big = new StateSave { Name = "Big", ParentContainer = hud };
        hud.States.Add(big);

        new StateVariableWriter().SetVariable(hud, big, "Counter1.Text", "Hi", hud.Instances[0], fallbackType: null);

        big.GetVariableSave("Counter1.Text")!.ExposedAsName.ShouldBe("Counter1Text");
    }

    [Fact]
    public void SetVariable_FilePathWithBackslashes_IsStoredWithForwardSlashes()
    {
        // gumcli edit setVariable writes through this; a path typed on Windows uses backslashes.
        StandardElementSave? sprite = Project.StandardElements.SingleOrDefault(item => item.Name == "Sprite");
        if (sprite == null)
        {
            sprite = new StandardElementSave { Name = "Sprite" };
            sprite.States.Add(new StateSave { Name = "Default", ParentContainer = sprite });
            Project.StandardElements.Add(sprite);
        }
        sprite.DefaultState.Variables.RemoveAll(item => item.Name == "SourceFile");
        sprite.DefaultState.Variables.Add(new VariableSave { Name = "SourceFile", Type = "string", IsFile = true });
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        InstanceSave icon = new InstanceSave { Name = "Icon", BaseType = "Sprite", ParentContainer = hud };
        hud.Instances.Add(icon);

        new StateVariableWriter().SetVariable(hud, hud.DefaultState, "Icon.SourceFile", "Images\\Hero.png", icon, fallbackType: null);

        hud.DefaultState.GetVariableSave("Icon.SourceFile")!.Value.ShouldBe("Images/Hero.png");
    }
}
