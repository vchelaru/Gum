using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultFromFileVisuals;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public partial class VariableReferenceCreationTests
{
    // ---- Index rows ----

    [Fact]
    public void IndexRows_CustomVariableOverriddenInTheScreen_EachRowShowsTheOverride()
    {
        UseExpressions(true);
        GumProjectSave project = CreateProject();
        ComponentSave menu = new ComponentSave { Name = "Menu", BaseType = "Container" };
        menu.States.Add(new StateSave { Name = "Default", ParentContainer = menu });
        menu.Instances.Add(new InstanceSave { Name = "First", BaseType = "Text", ParentContainer = menu });
        menu.Instances.Add(new InstanceSave { Name = "Second", BaseType = "Text", ParentContainer = menu });
        menu.DefaultState.Variables.Add(new VariableSave { Name = "Offset", Type = "float", Value = 0f, SetsValue = true, IsCustomVariable = true });
        AddRows(menu.DefaultState, "First", "Y = @Index * 10 + Offset");
        AddRows(menu.DefaultState, "Second", "Y = @Index * 10 + Offset");
        project.Components.Add(menu);
        ScreenSave screen = AddScreen(project, "Menu", "Menu1");
        AddVariable(screen.DefaultState, "Menu1.Offset", "float", 100f);

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        GraphicalUiElement menu1 = runtime.GetGraphicalUiElementByName("Menu1")!;
        menu1.GetGraphicalUiElementByName("First")!.Y.ShouldBe(100f);
        menu1.GetGraphicalUiElementByName("Second")!.Y.ShouldBe(110f);
    }

    // ---- Forms Button: Text is set on the button, a sibling copies the text child ----

    [Fact]
    public void FormsButton_TextSetFromCode_ShadowReadingTheTextChildFollows()
    {
        GumProjectSave project = CreateProject();
        AddButtonLike(project);
        ElementSaveExtensions.RegisterGueInstantiation("ButtonLike", () => new DefaultFromFileButtonRuntime());
        ScreenSave screen = AddScreen(project, "ButtonLike", "Button1");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        GraphicalUiElement button1 = runtime.GetGraphicalUiElementByName("Button1")!;
        Button button = ((DefaultFromFileButtonRuntime)button1).FormsControl!;

        button.Text = "Clicked";

        ((TextRuntime)button1.GetGraphicalUiElementByName("Shadow")!).Text.ShouldBe("Clicked");
    }

    [Fact]
    public void FormsButton_TextOverriddenInTheScreen_ShadowReadingTheTextChildShowsIt()
    {
        GumProjectSave project = CreateProject();
        AddButtonLike(project);
        ElementSaveExtensions.RegisterGueInstantiation("ButtonLike", () => new DefaultFromFileButtonRuntime());
        ScreenSave screen = AddScreen(project, "ButtonLike", "Button1");
        AddVariable(screen.DefaultState, "Button1.Text", "string", "From file");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        GraphicalUiElement button1 = runtime.GetGraphicalUiElementByName("Button1")!;
        ((TextRuntime)button1.GetGraphicalUiElementByName("Shadow")!).Text.ShouldBe("From file");
    }

    // ---- A project saved to disk and loaded back, as a game loads a .gumx ----

    [Fact]
    public void LoadedGumx_ScreenOverride_ShadowShowsIt()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "GumRefCreation_" + System.Guid.NewGuid().ToString("N"));
        try
        {
            GumProjectSave project = new GumProjectSave();
            StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);
            ComponentSave label = AddLabelComponent(project, "Label", "Text = Text");
            project.ComponentReferences.Add(new ElementReference { Name = "Label", ElementType = ElementType.Component });
            ScreenSave screen = AddScreen(project, "Label", "Label1");
            AddVariable(screen.DefaultState, "Label1.Text", "string", "From disk");
            project.ScreenReferences.Add(new ElementReference { Name = "TestScreen", ElementType = ElementType.Screen });

            Directory.CreateDirectory(Path.Combine(tempDirectory, ElementReference.ScreenSubfolder));
            Directory.CreateDirectory(Path.Combine(tempDirectory, ElementReference.ComponentSubfolder));
            Directory.CreateDirectory(Path.Combine(tempDirectory, ElementReference.StandardSubfolder));
            string gumxPath = Path.Combine(tempDirectory, "Test." + GumProjectSave.ProjectExtension);
            project.Save(gumxPath, saveElements: true);

            GumProjectSave? loaded = GumProjectSave.Load(gumxPath, out GumLoadResult loadResult);
            loaded.ShouldNotBeNull();
            loadResult.ErrorMessage.ShouldBeNullOrEmpty();
            loaded.Initialize();
            ObjectFinder.Self.GumProjectSave = loaded;
            StandardRuntimeRegistrations.Register();

            GraphicalUiElement runtime = loaded.Screens.First(s => s.Name == "TestScreen").ToGraphicalUiElement();

            ShadowOf(runtime, "Label1").Text.ShouldBe("From disk");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}
