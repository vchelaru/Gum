using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A tool author sets a tab's header text through the exposed Header variable on a theme's TabItem
/// component, so two tabs on one screen can read differently (#6408).
/// </summary>
public class ThemeTabItemHeaderTests : FileThemeInputTestBase
{
    [Theory]
    [InlineData("")]
    [InlineData("Bubblegum")]
    public void TabItemComponent_ExposesTextInstanceTextAsHeader(string theme)
    {
        GumProjectSave project = LoadTheme(theme);
        ComponentSave tabItem = project.Components.First(c => c.Name == ComponentNamed(project, "/TabItem"));

        VariableSave text = tabItem.DefaultState.Variables.First(v => v.Name == "TextInstance.Text");

        text.ExposedAsName.ShouldBe("Header");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Bubblegum")]
    public void AuthoredHeaders_ShowDifferentTextOnEachTab(string theme)
    {
        GumProjectSave project = LoadTheme(theme);
        string tabControl = ComponentNamed(project, "/TabControl");
        string tabItem = ComponentNamed(project, "/TabItem");

        ScreenSave screen = new ScreenSave { Name = "TabScreen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);
        screen.Instances.Add(new InstanceSave { Name = "Tabs", BaseType = tabControl, ParentContainer = screen });
        foreach (string name in new[] { "TabA", "TabB" })
        {
            screen.Instances.Add(new InstanceSave { Name = name, BaseType = tabItem, ParentContainer = screen });
            state.Variables.Add(new VariableSave { Type = "string", Name = name + ".Parent", Value = "Tabs.InnerPanelInstance", SetsValue = true });
            state.Variables.Add(new VariableSave { Type = "string", Name = name + ".Header", Value = "Header " + name, SetsValue = true });
        }
        project.Screens.Add(screen);

        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);

        string TextOf(string tab)
        {
            GraphicalUiElement text = screenVisual.GetGraphicalUiElementByName(tab)!.GetGraphicalUiElementByName("TextInstance")!;
            return ((IText)text.RenderableComponent).RawText;
        }
        TextOf("TabA").ShouldBe("Header TabA");
        TextOf("TabB").ShouldBe("Header TabB");
    }
}
