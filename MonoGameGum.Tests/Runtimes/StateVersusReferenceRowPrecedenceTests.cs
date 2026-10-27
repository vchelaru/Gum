using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// What a running game shows when the default state has a reference row (<c>X = Source.X</c>) and a
/// categorized state sets <c>X</c> to a plain value with no row of its own (#6851, PR #6858). The
/// state is applied the way a Forms control applies it, through <c>SetProperty("&lt;Category&gt;State", ...)</c>.
/// These pin the behavior as it is today (the maintainer decided on 2026-10-26 that a row re-driving the value is correct, #6861); the canvas-side counterparts are in
/// <c>StateVersusReferenceRowCanvasTests</c> (Gum.Presentation.Tests).
/// </summary>
public class StateVersusReferenceRowPrecedenceTests : BaseTestClass
{
    private static readonly (int Red, int Green, int Blue) Teal = (6, 159, 177);
    private static readonly (int Red, int Green, int Blue) DarkGray = (70, 70, 80);

    public StateVersusReferenceRowPrecedenceTests()
    {
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.RegisterGueInstantiation("ColoredRectangle", () => new ColoredRectangleRuntime());
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static void UseExpressions(bool useExpressions)
    {
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }
    }

    #region Plain variable (Width)

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyState_PlainWidthOverDefaultReferenceRow_ShowsTheStatesWidth(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement panel = CreatePanel();
        Background(panel).Width.ShouldBe(50f);

        panel.SetProperty("SizeCategoryState", "Large");

        Background(panel).Width.ShouldBe(200f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyState_PlainWidthThenReferencedSourceChangesThroughTheChild_RowReDrivesWidthOverTheState(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement panel = CreatePanel();
        panel.SetProperty("SizeCategoryState", "Large");

        panel.GetGraphicalUiElementByName("Reference")!.Width = 80f;

        Background(panel).Width.ShouldBe(80f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyState_PlainWidthThenReferencedSourceChangesThroughTheContainer_RowReDrivesWidthOverTheState(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement panel = CreatePanel();
        panel.SetProperty("SizeCategoryState", "Large");

        panel.SetProperty("Reference.Width", 80f);

        Background(panel).Width.ShouldBe(80f);
    }

    #endregion

    #region Composite (Color)

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyState_PlainChannelsOverDefaultColorReferenceRow_ShowsTheStatesColor(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement swatch = CreateSwatch();
        ColorOf(swatch, "Box").ShouldBe(Teal);

        swatch.SetProperty("ShadeCategoryState", "Dark");

        ColorOf(swatch, "Box").ShouldBe(DarkGray);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyState_PlainChannelsThenReferencedSourceChanges_RowReDrivesColorOverTheState(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement swatch = CreateSwatch();
        swatch.SetProperty("ShadeCategoryState", "Dark");
        ColoredRectangleRuntime source = (ColoredRectangleRuntime)swatch.GetGraphicalUiElementByName("Source")!;

        source.Red = 200;
        source.Green = 100;
        source.Blue = 50;

        ColorOf(swatch, "Box").ShouldBe((200, 100, 50));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyState_PlainChannelsThenOnlyOneSourceChannelChanges_RowReDrivesOnlyThatChannel(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement swatch = CreateSwatch();
        swatch.SetProperty("ShadeCategoryState", "Dark");
        ColoredRectangleRuntime source = (ColoredRectangleRuntime)swatch.GetGraphicalUiElementByName("Source")!;

        source.Red = 200;

        ColorOf(swatch, "Box").ShouldBe((200, DarkGray.Green, DarkGray.Blue));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyState_PlainWidthThenReferencedSourceChangesBackToItsOriginalValue_RowReDrivesTheOriginalValue(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement panel = CreatePanel();
        panel.SetProperty("SizeCategoryState", "Large");
        panel.GetGraphicalUiElementByName("Reference")!.Width = 80f;

        panel.GetGraphicalUiElementByName("Reference")!.Width = 50f;

        Background(panel).Width.ShouldBe(50f);
    }

    #endregion

    #region Forms Button, cross-element row

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IsEnabled_FalseOverDefaultCrossElementColorRow_ShowsTheDisabledColor(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement visual = CreateButtonVisual();
        ColorOf(visual, "Background").ShouldBe(Teal);

        ((Button)((InteractiveGue)visual).FormsControlAsObject).IsEnabled = false;

        ColorOf(visual, "Background").ShouldBe(DarkGray);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RefreshStyles_StylesChangedWhileDisabled_KeepsTheDisabledColor(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement visual = CreateButtonVisual();
        ((Button)((InteractiveGue)visual).FormsControlAsObject).IsEnabled = false;
        StateSave stylesDefault = ObjectFinder.Self.GetComponent("Styles")!.DefaultState!;
        stylesDefault.GetVariableSave("Primary.Red")!.Value = 200;
        stylesDefault.GetVariableSave("Primary.Green")!.Value = 100;
        stylesDefault.GetVariableSave("Primary.Blue")!.Value = 50;

        ObjectFinder.Self.GumProjectSave!.ApplyAllVariableReferences();
        // The default state's materialized color followed the row, so the row did re-evaluate.
        ObjectFinder.Self.GetComponent("Controls/ButtonStandard")!.DefaultState!.GetVariableSave("Background.Red")!.Value.ShouldBe(200);

        visual.RefreshStyles();

        ColorOf(visual, "Background").ShouldBe(DarkGray);
    }

    #endregion

    private static ColoredRectangleRuntime Background(GraphicalUiElement owner) =>
        (ColoredRectangleRuntime)owner.GetGraphicalUiElementByName("Background")!;

    private static (int, int, int) ColorOf(GraphicalUiElement owner, string instanceName)
    {
        ColoredRectangleRuntime rectangle = (ColoredRectangleRuntime)owner.GetGraphicalUiElementByName(instanceName)!;
        return (rectangle.Red, rectangle.Green, rectangle.Blue);
    }

    private static void SetColor(StateSave state, string owner, (int Red, int Green, int Blue) color)
    {
        state.Variables.Add(new VariableSave { Name = owner + ".Red", Value = color.Red, Type = "int", SetsValue = true });
        state.Variables.Add(new VariableSave { Name = owner + ".Green", Value = color.Green, Type = "int", SetsValue = true });
        state.Variables.Add(new VariableSave { Name = owner + ".Blue", Value = color.Blue, Type = "int", SetsValue = true });
    }

    private static void AddRow(StateSave state, string owner, string line)
    {
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = owner + ".VariableReferences" };
        list.Value.Add(line);
        state.VariableLists.Add(list);
    }

    private static GumProjectSave BuildProject(params ComponentSave[] components)
    {
        GumProjectSave project = new GumProjectSave();
        foreach (string standardName in new[] { "Container", "ColoredRectangle" })
        {
            StandardElementSave standard = new StandardElementSave { Name = standardName };
            StateSave standardDefault = StandardElementsManager.Self.GetDefaultStateFor(standardName)!;
            standardDefault.ParentContainer = standard;
            standard.States.Add(standardDefault);
            project.StandardElements.Add(standard);
        }
        foreach (ComponentSave component in components)
        {
            project.Components.Add(component);
        }
        ObjectFinder.Self.GumProjectSave = project;
        return project;
    }

    // Default: Background.Width = 50 with the row "Width = Reference.Width"; Large sets Background.Width = 200 and has no row.
    private static GraphicalUiElement CreatePanel()
    {
        ComponentSave panel = new ComponentSave { Name = "Panel", BaseType = "Container" };
        panel.Instances.Add(new InstanceSave { Name = "Background", BaseType = "ColoredRectangle", ParentContainer = panel });
        panel.Instances.Add(new InstanceSave { Name = "Reference", BaseType = "ColoredRectangle", ParentContainer = panel });

        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = panel };
        panel.States.Add(defaultState);
        defaultState.Variables.Add(new VariableSave { Name = "Reference.Width", Value = 50f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "Background.Width", Value = 50f, Type = "float", SetsValue = true });
        AddRow(defaultState, "Background", "Width = Reference.Width");

        StateSaveCategory category = new StateSaveCategory { Name = "SizeCategory" };
        StateSave large = new StateSave { Name = "Large", ParentContainer = panel };
        large.Variables.Add(new VariableSave { Name = "Background.Width", Value = 200f, Type = "float", SetsValue = true });
        category.States.Add(large);
        panel.Categories.Add(category);

        BuildProject(panel);
        return panel.ToGraphicalUiElement();
    }

    // Default: Box is teal with the row "Color = Source.Color" (Source is teal); Dark sets Box to dark gray and has no row.
    private static GraphicalUiElement CreateSwatch()
    {
        ComponentSave swatch = new ComponentSave { Name = "Swatch", BaseType = "Container" };
        swatch.Instances.Add(new InstanceSave { Name = "Box", BaseType = "ColoredRectangle", ParentContainer = swatch });
        swatch.Instances.Add(new InstanceSave { Name = "Source", BaseType = "ColoredRectangle", ParentContainer = swatch });

        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = swatch };
        swatch.States.Add(defaultState);
        SetColor(defaultState, "Source", Teal);
        SetColor(defaultState, "Box", Teal);
        AddRow(defaultState, "Box", "Color = Source.Color");

        StateSaveCategory category = new StateSaveCategory { Name = "ShadeCategory" };
        StateSave dark = new StateSave { Name = "Dark", ParentContainer = swatch };
        SetColor(dark, "Box", DarkGray);
        category.States.Add(dark);
        swatch.Categories.Add(category);

        BuildProject(swatch);
        return swatch.ToGraphicalUiElement();
    }

    // ButtonStandard as the Forms template ships it, with Styles.Primary teal. The default state's
    // Background color follows Styles.Primary through a cross-element row; Disabled sets dark gray and has no row.
    private static GraphicalUiElement CreateButtonVisual()
    {
        ComponentSave styles = new ComponentSave { Name = "Styles", BaseType = "Container" };
        styles.Instances.Add(new InstanceSave { Name = "Primary", BaseType = "ColoredRectangle", ParentContainer = styles });
        StateSave stylesDefault = new StateSave { Name = "Default", ParentContainer = styles };
        styles.States.Add(stylesDefault);
        SetColor(stylesDefault, "Primary", Teal);

        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });

        ComponentSave button = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });
        button.Instances.Add(new InstanceSave { Name = "Background", BaseType = "ColoredRectangle", ParentContainer = button });

        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = button };
        button.States.Add(defaultState);
        SetColor(defaultState, "Background", Teal);
        AddRow(defaultState, "Background", "Color = Components/Styles.Primary.Color");

        StateSaveCategory category = new StateSaveCategory { Name = "ButtonCategory" };
        StateSave enabled = new StateSave { Name = "Enabled", ParentContainer = button };
        SetColor(enabled, "Background", Teal);
        StateSave disabled = new StateSave { Name = "Disabled", ParentContainer = button };
        SetColor(disabled, "Background", DarkGray);
        category.States.Add(enabled);
        category.States.Add(disabled);
        button.Categories.Add(category);

        GumProjectSave project = BuildProject(styles, button);
        project.Behaviors.Add(behavior);
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        return button.ToGraphicalUiElement();
    }
}
