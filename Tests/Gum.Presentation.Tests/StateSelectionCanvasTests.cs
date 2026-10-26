using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Issue #6851: the state the user clicks in the States tab must be the state the canvas shows.
/// Drives the real <see cref="WireframeObjectManager.RefreshAll(bool, bool)"/> over elements built
/// with the real <c>ToGraphicalUiElement</c>; only the renderable (a recorder of Red/Green/Blue
/// and Width) and the selection are stand-ins.
/// </summary>
public class StateSelectionCanvasTests : BaseTestClass
{
    private sealed class RecordingRenderable : InvisibleRenderable
    {
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
    }

    private static readonly (int Red, int Green, int Blue) Teal = (6, 159, 177);
    private static readonly (int Red, int Green, int Blue) DarkGray = (70, 70, 80);

    private readonly Func<string, ISystemManagers, IRenderable?>? _previousRenderableFunc;

    public StateSelectionCanvasTests()
    {
        GumExpressionService.Initialize();
        _previousRenderableFunc = ElementSaveExtensions.CustomCreateGraphicalComponentFunc;
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = (_, _) => new RecordingRenderable();
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = _previousRenderableFunc;
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.VariableChangedThroughReference = null;
        base.Dispose();
    }

    #region Issue repro and its discriminators

    /// <summary>
    /// The reported case: ButtonStandard with the Button behavior, a reference on the default state
    /// (<c>Color = Styles.Primary.FillColor</c>) and a reference on Disabled
    /// (<c>Color = Styles.DarkGray.FillColor</c>). Clicking Disabled must show dark gray.
    /// </summary>
    [Fact]
    public void RefreshAll_DisabledSelectedOnButtonWithDefaultStateReference_ShowsDisabledColor()
    {
        ComponentSave button = BuildButton(defaultHasColorReference: true);

        (int, int, int) shown = ShowSelectedAndReadBackground(button, button.Categories[0].States.Single(s => s.Name == "Disabled"));

        shown.ShouldBe(DarkGray);
    }

    /// <summary>
    /// Same as the repro after the tool has re-run the behavior's tool-only references on the default
    /// state, as it does after every edit. Separates "the applier pins Enabled" from the repro above.
    /// </summary>
    [Fact]
    public void RefreshAll_DisabledSelectedAfterBehaviorApplierRan_ShowsDisabledColor()
    {
        ComponentSave button = BuildButton(defaultHasColorReference: true);
        BehaviorToolOnlyReferencesApplier.Apply(button, button.DefaultState!);

        (int, int, int) shown = ShowSelectedAndReadBackground(button, button.Categories[0].States.Single(s => s.Name == "Disabled"));

        shown.ShouldBe(DarkGray);
    }

    /// <summary>
    /// Discriminator: the default state holds no reference for the Background color, so nothing can
    /// re-derive it after the selected state is applied. This is green when the cause is the default
    /// state's references, and the behavior applier is not involved at all.
    /// </summary>
    [Fact]
    public void RefreshAll_DisabledSelectedAndDefaultStateHasNoColorReference_ShowsDisabledColor()
    {
        ComponentSave button = BuildButton(defaultHasColorReference: false);

        (int, int, int) shown = ShowSelectedAndReadBackground(button, button.Categories[0].States.Single(s => s.Name == "Disabled"));

        shown.ShouldBe(DarkGray);
    }

    /// <summary>
    /// Discriminator: applying the selected state to a freshly created root (the step before the
    /// references pass in RefreshAll) already shows Disabled, so the later references pass is what
    /// brings the default state's color back.
    /// </summary>
    [Fact]
    public void ApplySelectedStateAfterCreation_DisabledSelectedOnButton_ShowsDisabledColor()
    {
        ComponentSave button = BuildButton(defaultHasColorReference: true);
        StateSave disabled = button.Categories[0].States.Single(s => s.Name == "Disabled");
        GraphicalUiElement root = button.ToGraphicalUiElement(systemManagers: null!, addToManagers: false);
        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.SetupGet(x => x.SelectedStateSave).Returns(disabled);
        WireframeObjectManager wireframeObjectManager = new WireframeObjectManager(
            Mock.Of<IFontManager>(), selectedState.Object, Mock.Of<IDialogService>(), Mock.Of<IGuiCommands>(),
            new LocalizationService(), Mock.Of<IPluginManager>(), Mock.Of<IProjectState>());

        wireframeObjectManager.ApplySelectedStateAfterCreation(root, button);

        RecordingRenderable background = BackgroundOf(root);
        (background.Red, background.Green, background.Blue).ShouldBe(DarkGray);
    }

    /// <summary>
    /// The behavior's tool-only reference writes nothing into the component's default state while
    /// IsEnabled is at its declared default, so it cannot be what keeps the canvas on Enabled.
    /// </summary>
    [Fact]
    public void BehaviorApplier_IsEnabledAtDeclaredDefault_DoesNotWriteACategoryStateOnTheComponent()
    {
        ComponentSave button = BuildButton(defaultHasColorReference: true);

        BehaviorToolOnlyReferencesApplier.Apply(button, button.DefaultState!);

        button.DefaultState!.GetValue("ButtonCategoryState").ShouldBeNull();
    }

    #endregion

    /// <summary>
    /// Setup guard: the composite <c>Color = Components/Styles.Primary.FillColor</c> row on the default
    /// state is evaluated by RefreshAll in this harness, so a green repro is not a reference that never ran.
    /// </summary>
    [Fact]
    public void RefreshAll_DefaultSelectedWithColorReference_AppliesTheReferencedColor()
    {
        ComponentSave button = BuildButton(defaultHasColorReference: true);
        foreach (VariableSave variable in button.DefaultState!.Variables.Where(v => v.Name is "Background.Red" or "Background.Green" or "Background.Blue"))
        {
            variable.Value = 1;
        }

        (int, int, int) shown = ShowSelectedAndReadBackground(button, button.DefaultState);

        shown.ShouldBe(Teal);
    }

    #region Siblings sharing the root condition

    /// <summary>
    /// A categorized state in a category with no behavior: Size/Large sets the Background width and
    /// the default state has a reference on the same variable.
    /// </summary>
    [Fact]
    public void RefreshAll_CategoryWithoutBehaviorSelected_ShowsSelectedStatesValue()
    {
        ComponentSave panel = BuildPanelWithSizeCategory();

        float shown = ShowSelectedAndReadBackgroundWidth(panel, panel.Categories[0].States.Single(s => s.Name == "Large"));

        shown.ShouldBe(200f);
    }

    /// <summary>
    /// Authored IsEnabled=false: creation applies Disabled through the default state's derived
    /// ButtonCategoryState, then the user clicks Enabled. The clicked state must win. The default state
    /// has no color reference, so this isolates the behavior-derived state from the references pass.
    /// </summary>
    [Fact]
    public void RefreshAll_EnabledSelectedOnButtonWithAuthoredIsEnabledFalse_ShowsEnabledColor()
    {
        ComponentSave button = BuildButton(defaultHasColorReference: false);
        button.DefaultState!.SetValue("IsEnabled", false, "bool");
        BehaviorToolOnlyReferencesApplier.Apply(button, button.DefaultState);
        button.DefaultState.GetValue("ButtonCategoryState").ShouldBe("Disabled");

        (int, int, int) shown = ShowSelectedAndReadBackground(button, button.Categories[0].States.Single(s => s.Name == "Enabled"));

        shown.ShouldBe(Teal);
    }

    /// <summary>
    /// Authored IsEnabled=false and the user clicks Disabled: nothing may change what it shows.
    /// </summary>
    [Fact]
    public void RefreshAll_DisabledSelectedOnButtonWithAuthoredIsEnabledFalse_ShowsDisabledColor()
    {
        ComponentSave button = BuildButton(defaultHasColorReference: true);
        button.DefaultState!.SetValue("IsEnabled", false, "bool");
        BehaviorToolOnlyReferencesApplier.Apply(button, button.DefaultState);

        (int, int, int) shown = ShowSelectedAndReadBackground(button, button.Categories[0].States.Single(s => s.Name == "Disabled"));

        shown.ShouldBe(DarkGray);
    }

    /// <summary>
    /// A state that holds only a reference row (nothing materialized yet) while the default state
    /// stores plain values: the selected state's own reference must still be applied.
    /// </summary>
    [Fact]
    public void RefreshAll_SelectedStateHasOnlyAReferenceRow_AppliesThatReference()
    {
        ComponentSave button = BuildButton(defaultHasColorReference: false);
        StateSave disabled = button.Categories[0].States.Single(s => s.Name == "Disabled");
        disabled.Variables.Clear();

        (int, int, int) shown = ShowSelectedAndReadBackground(button, disabled);

        shown.ShouldBe(DarkGray);
    }

    #endregion

    #region Setup

    private (int, int, int) ShowSelectedAndReadBackground(ComponentSave component, StateSave selected)
    {
        GraphicalUiElement root = Show(component, selected);
        RecordingRenderable background = BackgroundOf(root);
        return (background.Red, background.Green, background.Blue);
    }

    private float ShowSelectedAndReadBackgroundWidth(ComponentSave component, StateSave selected)
    {
        GraphicalUiElement root = Show(component, selected);
        return root.GetGraphicalUiElementByName("Background")!.Width;
    }

    private static RecordingRenderable BackgroundOf(GraphicalUiElement root) =>
        (RecordingRenderable)root.GetGraphicalUiElementByName("Background")!.RenderableComponent!;

    private static GraphicalUiElement Show(ComponentSave component, StateSave selected)
    {
        GraphicalUiElement? root = null;

        Mock<IPluginManager> pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(x => x.CreateGraphicalUiElement(component)).Returns(() =>
        {
            root = component.ToGraphicalUiElement(systemManagers: null!, addToManagers: false);
            return root;
        });

        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.SetupGet(x => x.SelectedElements).Returns(new[] { component });
        selectedState.SetupGet(x => x.SelectedElement).Returns(component);
        selectedState.SetupGet(x => x.SelectedStateSave).Returns(selected);

        WireframeObjectManager wireframeObjectManager = new WireframeObjectManager(
            Mock.Of<IFontManager>(),
            selectedState.Object,
            Mock.Of<IDialogService>(),
            Mock.Of<IGuiCommands>(),
            new LocalizationService(),
            pluginManager.Object,
            Mock.Of<IProjectState>());

        wireframeObjectManager.RefreshAll(forceLayout: true);

        root.ShouldNotBeNull();
        return root!;
    }

    private static StandardElementSave BuildStandard(string name)
    {
        StandardElementSave standard = new StandardElementSave { Name = name };
        StateSave defaults = StandardElementsManager.Self.GetDefaultStateFor(name).Clone();
        defaults.ParentContainer = standard;
        standard.States.Add(defaults);
        return standard;
    }

    private static void AddReference(StateSave state, string owner, string line)
    {
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = owner + ".VariableReferences" };
        list.Value.Add(line);
        state.VariableLists.Add(list);
    }

    // A Rectangle's color channels are FillRed/Green/Blue; a NineSlice's (the button Background) are Red/Green/Blue.
    private static void SetFill(StateSave state, string owner, (int Red, int Green, int Blue) color, string prefix = "Fill")
    {
        state.Variables.Add(new VariableSave { Name = owner + "." + prefix + "Red", Value = color.Red, Type = "int", SetsValue = true });
        state.Variables.Add(new VariableSave { Name = owner + "." + prefix + "Green", Value = color.Green, Type = "int", SetsValue = true });
        state.Variables.Add(new VariableSave { Name = owner + "." + prefix + "Blue", Value = color.Blue, Type = "int", SetsValue = true });
    }

    /// <summary>The project's Styles component: Primary is teal, DarkGray is dark gray.</summary>
    private static ComponentSave BuildStyles()
    {
        ComponentSave styles = new ComponentSave { Name = "Styles", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = styles };
        styles.States.Add(defaultState);
        styles.Instances.Add(new InstanceSave { Name = "Primary", BaseType = "Rectangle", ParentContainer = styles });
        styles.Instances.Add(new InstanceSave { Name = "DarkGray", BaseType = "Rectangle", ParentContainer = styles });
        SetFill(defaultState, "Primary", Teal);
        SetFill(defaultState, "DarkGray", DarkGray);
        return styles;
    }

    private static GumProjectSave BuildProject(params ComponentSave[] components)
    {
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(BuildStandard("Container"));
        project.StandardElements.Add(BuildStandard("Rectangle"));
        project.StandardElements.Add(BuildStandard("NineSlice"));
        foreach (ComponentSave component in components)
        {
            project.Components.Add(component);
        }
        ObjectFinder.Self.GumProjectSave = project;
        return project;
    }

    /// <summary>
    /// ButtonStandard as the Forms template ships it: Background (NineSlice), the Button behavior,
    /// a default state with a hard teal color and, when asked, the reference row
    /// <c>Color = Components/Styles.Primary.FillColor</c>, and ButtonCategory with Enabled (teal,
    /// Primary) and Disabled (dark gray stored, <c>Color = Components/Styles.DarkGray.FillColor</c>).
    /// </summary>
    private static ComponentSave BuildButton(bool defaultHasColorReference)
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        behavior.ToolOnlyVariableReferences.Add("ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");

        ComponentSave button = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });
        button.Instances.Add(new InstanceSave { Name = "Background", BaseType = "NineSlice", ParentContainer = button });

        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = button };
        button.States.Add(defaultState);
        SetFill(defaultState, "Background", Teal, prefix: "");
        defaultState.Variables.Add(new VariableSave { Name = "ButtonCategoryState", Type = "ButtonCategory", SetsValue = false });
        if (defaultHasColorReference)
        {
            AddReference(defaultState, "Background", "Color = Components/Styles.Primary.FillColor");
        }

        StateSaveCategory category = new StateSaveCategory { Name = "ButtonCategory" };
        StateSave enabled = new StateSave { Name = "Enabled", ParentContainer = button };
        SetFill(enabled, "Background", Teal, prefix: "");
        AddReference(enabled, "Background", "Color = Components/Styles.Primary.FillColor");
        StateSave disabled = new StateSave { Name = "Disabled", ParentContainer = button };
        SetFill(disabled, "Background", DarkGray, prefix: "");
        AddReference(disabled, "Background", "Color = Components/Styles.DarkGray.FillColor");
        category.States.Add(enabled);
        category.States.Add(disabled);
        button.Categories.Add(category);

        GumProjectSave project = BuildProject(BuildStyles(), button);
        project.Behaviors.Add(behavior);
        return button;
    }

    /// <summary>A Panel whose default Background width follows a reference and whose Large state sets 200.</summary>
    private static ComponentSave BuildPanelWithSizeCategory()
    {
        ComponentSave panel = new ComponentSave { Name = "Panel", BaseType = "Container" };
        panel.Instances.Add(new InstanceSave { Name = "Background", BaseType = "Rectangle", ParentContainer = panel });
        panel.Instances.Add(new InstanceSave { Name = "Reference", BaseType = "Rectangle", ParentContainer = panel });

        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = panel };
        panel.States.Add(defaultState);
        defaultState.Variables.Add(new VariableSave { Name = "Reference.Width", Value = 50f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "Background.Width", Value = 50f, Type = "float", SetsValue = true });
        AddReference(defaultState, "Background", "Width = Reference.Width");

        StateSaveCategory category = new StateSaveCategory { Name = "SizeCategory" };
        StateSave large = new StateSave { Name = "Large", ParentContainer = panel };
        large.Variables.Add(new VariableSave { Name = "Background.Width", Value = 200f, Type = "float", SetsValue = true });
        category.States.Add(large);
        panel.Categories.Add(category);

        BuildProject(panel);
        return panel;
    }

    #endregion
}
