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

using static Gum.Presentation.Tests.StateSelectionCanvasHarness;

namespace Gum.Presentation.Tests;

/// <summary>
/// Issue #6851: the state the user clicks in the States tab must be the state the canvas shows.
/// Drives the real <see cref="WireframeObjectManager.RefreshAll(bool, bool)"/> over elements built
/// with the real <c>ToGraphicalUiElement</c>; only the renderable (a recorder of Red/Green/Blue
/// and Width) and the selection are stand-ins.
/// </summary>
public class StateSelectionCanvasTests : BaseTestClass
{
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
}
