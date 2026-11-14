using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A VariableReferences line that reads a Forms-behavior property of an instance (Text = Slider.Minimum),
/// built at game runtime from a project made from the DarkPro theme.
/// </summary>
public class BehaviorValueReferenceRuntimeTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static string LocateRepoRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (directory != null && !File.Exists(Path.Combine(directory, "GumFull.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }
        return directory ?? throw new InvalidOperationException("Repo root not found.");
    }

    private static string BuildAndReadMinText(bool expressions, bool setMinimumOnSlider, string line)
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        if (expressions)
        {
            GumExpressionService.Initialize();
        }
        string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "DarkPro", "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave owner = new ComponentSave { Name = "Custom/PropertySlider", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = owner };
        owner.States.Add(state);
        owner.Instances.Add(new InstanceSave { Name = "Slider", BaseType = "DarkPro/Slider", ParentContainer = owner });
        owner.Instances.Add(new InstanceSave { Name = "MinText", BaseType = "Text", ParentContainer = owner });
        if (setMinimumOnSlider)
        {
            state.Variables.Add(new VariableSave { Name = "Slider.Minimum", Type = "System.Double", Value = 1.0, SetsValue = true });
        }
        VariableListSave<string> list = new VariableListSave<string> { Name = "MinText.VariableReferences", Type = "string" };
        list.Value.Add(line);
        state.VariableLists.Add(list);
        project.Components.Add(owner);

        ScreenSave screen = new ScreenSave { Name = "Custom/Screen8020" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Owner1", BaseType = "Custom/PropertySlider", ParentContainer = screen });
        project.Screens.Add(screen);

        project.ApplyAllVariableReferences();
        StandardRuntimeRegistrations.Register();
        // Circle, Rectangle and Polygon need a rendering SystemManagers a headless test lacks; the references do not read them.
        foreach (string unrendered in new[] { "Circle", "Rectangle", "Polygon" })
        {
            ElementSaveExtensions.RegisterGueInstantiation(unrendered, () => new Gum.GueDeriving.ContainerRuntime());
        }
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        GraphicalUiElement ownerGue = screenGue.GetGraphicalUiElementByName("Owner1")!;
        GraphicalUiElement minText = ownerGue.GetGraphicalUiElementByName("MinText")!;
        return "[" + ((Gum.GueDeriving.TextRuntime)minText).Text + "]";
    }

    [Fact]
    public void Build_ReferenceReadingBehaviorProperty_AppliesStoredValueOrBehaviorDefaultWhenExpressionsAreOn()
    {
        string result =
            "setExpr=" + BuildAndReadMinText(true, true, "Text = Slider.Minimum") +
            " setNoExpr=" + BuildAndReadMinText(false, true, "Text = Slider.Minimum") +
            " defExpr=" + BuildAndReadMinText(true, false, "Text = Slider.Minimum") +
            " defNoExpr=" + BuildAndReadMinText(false, false, "Text = Slider.Minimum");
        result.ShouldBe("setExpr=[1] setNoExpr=[Hello] defExpr=[0] defNoExpr=[Hello]");
    }
}
